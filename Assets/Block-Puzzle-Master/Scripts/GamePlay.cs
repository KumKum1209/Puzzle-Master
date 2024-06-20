using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Xml.Linq;
using NaughtyAttributes;
using Unity.Burst.Intrinsics;

#if HBDOTween
using DG.Tweening;
#endif


// This script has main logic to run entire gameplay.
public class GamePlay : Singleton<GamePlay>, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler
{
    [HideInInspector]
    public List<Block> blockGrid;

    ShapeInfo currentShape = null;
    Transform hittingBlock = null;

    List<Block> highlightingBlocks;

    public AudioClip blockPlaceSound;
    public AudioClip blockSelectSound;

    // Line break sounds.
    [SerializeField] private AudioClip lineClear1;
    [SerializeField] private AudioClip lineClear2;
    [SerializeField] private AudioClip lineClear3;
    [SerializeField] private AudioClip lineClear4;

    public AudioClip blockNotPlacedSound;

    [Tooltip("Max no. of times rescue can be used in 1 game. -1 is infinite")]
    [SerializeField] private int MaxAllowedRescuePerGame = 0;

    [Tooltip("Max no. of times rescue can be used in 1 game using watch video. -1 is infinite")]
    [SerializeField] private int MaxAllowedVideoWatchRescue = 0;

    [HideInInspector]
    public int TotalFreeRescueDone = 0;

    [HideInInspector]
    public int TotalRescueDone = 0;

    [HideInInspector]
    public int MoveCount = 0;

    public Sprite BombSprite;
    public Timer timeSlider;

    //combo 
    public ComboAnim Combo;
    private int comboCount = 0;
    private float comboTime = 10f;
    private Coroutine comboResetCoroutine;

    public bool isHelpOnScreen = false;

    void Start()
    {
        //Generate board from GameBoardGenerator Script Component.
        GetComponent<GameBoardGenerator>().GenerateBoard();
        highlightingBlocks = new List<Block>();

        #region time mode
        // Timer will start with TIME and CHALLENGE mode.
        if (GameController.gameMode == GameMode.TIMED || GameController.gameMode == GameMode.CHALLENGE)
        {
            timeSlider.gameObject.SetActive(true);
        }
        #endregion

        #region check for help
        Invoke("CheckForHelp", 0.5F);
        #endregion
    }

    #region IBeginDragHandler implementation

    /// <summary>
    /// Raises the begin drag event.
    /// </summary>
    /// <param name="eventData">Event data.</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentShape != null)
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(eventData.position);
            pos.z = currentShape.transform.localPosition.z;
            currentShape.transform.localPosition = pos;
        }
    }

    #endregion

    #region IPointerDownHandler implementation
    /// <summary>
    /// Raises the pointer down event.
    /// </summary>
    /// <param name="eventData">Event data.</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            Transform clickedObject = eventData.pointerCurrentRaycast.gameObject.transform;

            if (clickedObject.GetComponent<ShapeInfo>() != null)
            {
                if (clickedObject.transform.childCount > 0)
                {
                    currentShape = clickedObject.GetComponent<ShapeInfo>();
                    Vector3 pos = Camera.main.ScreenToWorldPoint(eventData.position);
                    currentShape.transform.localScale = Vector3.one * GameBoardGenerator.Instance.BlockSize;
                    currentShape.transform.localPosition = new Vector3(pos.x, pos.y, 0);
                    AudioManager.Instance.PlaySound(blockSelectSound);

                    if (isHelpOnScreen)
                    {
                        GetComponent<InGameHelp>().StopHelp();
                    }
                }
            }
        }
    }
    #endregion

    #region IPointerUpHandler implementation
    /// <summary>
    /// Raises the pointer up event.
    /// </summary>
    /// <param name="eventData">Event data.</param>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (currentShape != null)
        {

            if (highlightingBlocks.Count > 0)
            {
                SetImageToPlacingBlocks();
                Destroy(currentShape.gameObject);
                currentShape = null;
                MoveCount += 1;
                Invoke("CheckBoardStatus", 0.1F);
            }
            else
            {
#if HBDOTween
                currentShape.transform.DOLocalMove(Vector3.zero, 0.5F);
                currentShape.transform.DOScale(Vector3.one * 0.6F, 0.5F);
#endif
                currentShape = null;
                AudioManager.Instance.PlaySound(blockNotPlacedSound);
            }
        }
    }
    #endregion

    #region IDragHandler implementation
    /// <summary>
    /// Raises the drag event.
    /// </summary>
    /// <param name="eventData">Event data.</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (currentShape != null)
        {
            Vector3 pos = Camera.main.ScreenToWorldPoint(eventData.position);
            pos = new Vector3(pos.x, (pos.y + 1F), 0F);

            currentShape.transform.position = pos;

            RaycastHit2D hit = Physics2D.Raycast(currentShape.GetComponent<ShapeInfo>().firstBlock.block.position, Vector2.zero, 1);

            if (hit.collider != null)
            {
                if (hittingBlock == null || hit.collider.transform != hittingBlock)
                {
                    hittingBlock = hit.collider.transform;
                    CanPlaceShape(hit.collider.transform);
                }
            }
            else
            {
                StopHighlighting();
            }
        }
    }
    #endregion

    /// <summary>
    /// Determines whether this instance can place shape the specified currentHittingBlock.
    /// </summary>
    public bool CanPlaceShape(Transform currentHittingBlock)
    {
        Block currentCell = currentHittingBlock.GetComponent<Block>();
        Debug.Log(currentCell);
        int currentRowID = currentCell.rowID;
        int currentColumnID = currentCell.columnID;

        StopHighlighting();

        bool canPlaceShape = true;
        foreach (ShapeBlock c in currentShape.ShapeBlocks)
        {
            Block checkingCell = blockGrid.Find(o => o.rowID == currentRowID + (c.rowID + currentShape.startOffsetX)
            && o.columnID == currentColumnID + (c.columnID - currentShape.startOffsetY));

            if ((checkingCell == null) || (checkingCell != null && checkingCell.isFilled))
            {
                canPlaceShape = false;
                highlightingBlocks.Clear();
                break;
            }
            else
            {
                if (!highlightingBlocks.Contains(checkingCell))
                {
                    highlightingBlocks.Add(checkingCell);
                }
            }
        }

        if (canPlaceShape)
        {
            SetHighLightImage();
        }

        return canPlaceShape;
    }

    /// <summary>
    /// Sets the high light image.
    /// </summary>
    void SetHighLightImage()
    {
        foreach (Block c in highlightingBlocks)
        {
            c.SetHighlightImage(currentShape.blockImage);
        }
    }

    /// <summary>
    /// Stops the highlighting.
    /// </summary>

    void StopHighlighting()
    {
        if (highlightingBlocks != null && highlightingBlocks.Count > 0)
        {
            foreach (Block c in highlightingBlocks)
            {
                c.StopHighlighting();
            }
        }
        hittingBlock = null;
        highlightingBlocks.Clear();
    }

    /// <summary>
    /// Sets the image to placing blocks.
    /// </summary>
    void SetImageToPlacingBlocks()
    {
        if (highlightingBlocks != null && highlightingBlocks.Count > 0)
        {
            foreach (Block c in highlightingBlocks)
            {
                c.SetBlockImage(currentShape.blockImage, currentShape.ShapeID);
            }
        }
        AudioManager.Instance.PlaySound(blockPlaceSound);
    }

    /// <summary>
    /// Checks the board status.
    /// </summary>
    void CheckBoardStatus()
    {
        int placingShapeBlockCount = highlightingBlocks.Count;
        List<int> updatedRows = new List<int>();
        List<int> updatedColumns = new List<int>();

        List<List<Block>> breakingRows = new List<List<Block>>();
        List<List<Block>> breakingColumns = new List<List<Block>>();
        List<List<Block>> allDiagonals = new List<List<Block>>();

        foreach (Block b in highlightingBlocks)
        {
            if (!updatedRows.Contains(b.rowID))
            {
                updatedRows.Add(b.rowID);
            }
            if (!updatedColumns.Contains(b.columnID))
            {
                updatedColumns.Add(b.columnID);
            }
        }
        highlightingBlocks.Clear();

        foreach (int rowID in updatedRows)
        {
            List<Block> currentRow = GetEntireRow(rowID);
            if (currentRow != null)
            {
                breakingRows.Add(currentRow);
            }
        }

        foreach (int columnID in updatedColumns)
        {
            List<Block> currentColumn = GetEntireColumn(columnID);
            if (currentColumn != null)
            {
                breakingColumns.Add(currentColumn);
            }
        }
        //GetAllCrossLine(allDiagonals);
     
        if (breakingRows.Count > 0 || breakingColumns.Count > 0 || allDiagonals.Count > 0)
        {
            StartCoroutine(BreakAllCompletedLines(breakingRows, breakingColumns, allDiagonals, placingShapeBlockCount));
        }
        else
        {
            BlockShapeSpawner.Instance.FillShapeContainer();
            ScoreManager.Instance.AddScore(10 * placingShapeBlockCount);
        }

        if (GameController.gameMode == GameMode.BLAST || GameController.gameMode == GameMode.CHALLENGE)
        {
            Invoke("UpdateBlockCount", 0.5F);
        }
    }

    /// <summary>
    /// Gets the entire row.
    /// </summary>
    /// <returns>The entire row.</returns>
    /// <param name="rowID">Row I.</param>
    List<Block> GetEntireRow(int rowID)
    {
        List<Block> thisRow = new List<Block>();
        for (int columnIndex = 0; columnIndex < GameBoardGenerator.Instance.TotalColumns; columnIndex++)
        {
            Block block = blockGrid.Find(o => o.rowID == rowID && o.columnID == columnIndex);

            if (block.isFilled)
            {
                thisRow.Add(block);
            }
            else
            {
                return null;
            }
        }
        return thisRow;
    }

    /// <summary>
    /// Gets the entire row for rescue.
    /// </summary>
    /// <returns>The entire row for rescue.</returns>
    /// <param name="rowID">Row I.</param>
    List<Block> GetEntireRowForRescue(int rowID)
    {
        List<Block> thisRow = new List<Block>();
        for (int columnIndex = 0; columnIndex < GameBoardGenerator.Instance.TotalColumns; columnIndex++)
        {
            Block block = blockGrid.Find(o => o.rowID == rowID && o.columnID == columnIndex);
            thisRow.Add(block);
        }
        return thisRow;
    }

    /// <summary>
    /// Gets the entire column.
    /// </summary>
    /// <returns>The entire column.</returns>
    /// <param name="columnID">Column I.</param>
    List<Block> GetEntireColumn(int columnID)
    {
        List<Block> thisColumn = new List<Block>();
        for (int rowIndex = 0; rowIndex < GameBoardGenerator.Instance.TotalRows; rowIndex++)
        {
            Block block = blockGrid.Find(o => o.rowID == rowIndex && o.columnID == columnID);
            if (block.isFilled)
            {
                thisColumn.Add(block);
            }
            else
            {
                return null;
            }
        }
        return thisColumn;
    }

    /// <summary>
    /// Gets the entire column for rescue.
    /// </summary>
    /// <returns>The entire column for rescue.</returns>
    /// <param name="columnID">Column I.</param>
    List<Block> GetEntireColumnForRescue(int columnID)
    {
        List<Block> thisColumn = new List<Block>();
        for (int rowIndex = 0; rowIndex < GameBoardGenerator.Instance.TotalRows; rowIndex++)
        {
            Block block = blockGrid.Find(o => o.rowID == rowIndex && o.columnID == columnID);
            thisColumn.Add(block);
        }
        return thisColumn;
    }
    public void GetAllCrossLine(List<List<Block>> allDiagonals)
    {
        for (int row = 0; row < GameBoardGenerator.Instance.TotalRows; row++)
        {
            for (int col = 0; col < GameBoardGenerator.Instance.TotalColumns; col++)
            {
                List<Block> diagonal = GetDiagonalsFromPoint(row, col);
                if (diagonal != null)
                {
                    allDiagonals.Add(diagonal);
                }
            }
        }
    }

    List<Block> GetDiagonalsFromPoint(int startRow, int startCol)
    {
        List<Block> diagonal = new List<Block>();

        int maxRowIndex = GameBoardGenerator.Instance.TotalRows - 1;
        int maxColIndex = GameBoardGenerator.Instance.TotalColumns - 1;

        // Duyệt chéo xuống và phải
        int row = startRow;
        int col = startCol;
        while (row <= maxRowIndex && col <= maxColIndex)
        {
            Block block = blockGrid.Find(o => o.rowID == row && o.columnID == col);
            if (block == null || !block.isFilled)
            {
                return null; 
            }
            diagonal.Add(block);
            row++;
            col++;
        }

        // Duyệt chéo lên và trái
        row = startRow - 1;
        col = startCol - 1;
        while (row >= 0 && col >= 0)
        {
            Block block = blockGrid.Find(o => o.rowID == row && o.columnID == col);
            if (block == null || !block.isFilled)
            {
                return null; 
            }
            diagonal.Add(block);
            row--;
            col--;
        }

        return diagonal;
    }
    /// <summary>
    /// Breaks all completed lines.
    /// </summary>
    /// <returns>The all completed lines.</returns>
    /// <param name="breakingRows">Breaking rows.</param>
    /// <param name="breakingColumns">Breaking columns.</param>
    /// <param name="placingShapeBlockCount">Placing shape block count.</param>
    IEnumerator BreakAllCompletedLines(List<List<Block>> breakingRows, List<List<Block>> breakingColumns,List<List<Block>> breakingCross, int placingShapeBlockCount)
    {
        int TotalBreakingLines = breakingRows.Count + breakingColumns.Count + breakingCross.Count;

        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
        }
        comboResetCoroutine = StartCoroutine(ResetComboAfterDelay(comboTime));

        comboCount += TotalBreakingLines;
        if (comboCount > 10) comboCount = 10;

        bool unbelieveable = false;


        int comboBonus = comboCount * 100;

        if (TotalBreakingLines == 1)
        {
            AudioManager.Instance.PlaySound(lineClear1);
            ScoreManager.Instance.AddScore(100 + (placingShapeBlockCount * 10) + comboBonus);
        }
        else if (TotalBreakingLines == 2)
        {
            AudioManager.Instance.PlaySound(lineClear2);
            ScoreManager.Instance.AddScore(300 + (placingShapeBlockCount * 10) + comboBonus);
        }
        else if (TotalBreakingLines == 3)
        {
            unbelieveable = true;
            AudioManager.Instance.PlaySound(lineClear3);
            ScoreManager.Instance.AddScore(600 + (placingShapeBlockCount * 10) + comboBonus);
        }
        else if (TotalBreakingLines >= 4)
        {
            AudioManager.Instance.PlaySound(lineClear4);
            unbelieveable = true;
            if (TotalBreakingLines == 4)
            {
                ScoreManager.Instance.AddScore(1000 + (placingShapeBlockCount * 10) + comboBonus);
            }
            else
            {
                ScoreManager.Instance.AddScore((300 * TotalBreakingLines) + (placingShapeBlockCount * 10) + comboBonus);
            }
        }
        //  combo
        ShowCombo(comboCount, unbelieveable);
        yield return 0;
        // row 
        if (breakingRows.Count > 0)
        {
            foreach (List<Block> thisLine in breakingRows)
            {
                StartCoroutine(BreakThisLine(thisLine));
            }
        }
        //delay
        if (breakingRows.Count > 0)
        {
            yield return new WaitForSeconds(0.1F);
        }
        // column
        if (breakingColumns.Count > 0)
        {
            foreach (List<Block> thisLine in breakingColumns)
            {
                StartCoroutine(BreakThisLine(thisLine));
            }
        }
        if (breakingColumns.Count > 0)
        {
            yield return new WaitForSeconds(0.1F);
        }
        if (breakingCross.Count > 0)
        {
            foreach (List<Block> thisLine in breakingCross)
            {
                if (thisLine.Count > 1) 
                {
                    StartCoroutine(BreakThisLine(thisLine));
                }
             
            }
        }
        BlockShapeSpawner.Instance.FillShapeContainer();

        #region time mode
        if (GameController.gameMode == GameMode.TIMED || GameController.gameMode == GameMode.CHALLENGE)
        {
            timeSlider.AddSeconds(TotalBreakingLines * 5);
        }
        #endregion
    }
    private void ShowCombo(int comboCount, bool unbelieve)
    {
        Combo.gameObject.SetActive(true);
        Combo.ShowCombo(comboCount, unbelieve);
    }
    private IEnumerator ResetComboAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        comboCount = 0;
        /* HideCombo();*/
    }
    private void HideCombo()
    {
        Combo.gameObject.SetActive(false);
    }
    /// <summary>
    /// Breaks the this line.
    /// </summary>
    /// <returns>The this line.</returns>
    /// <param name="breakingLine">Breaking line.</param>
    IEnumerator BreakThisLine(List<Block> breakingLine)
    {
        foreach (Block b in breakingLine)
        {
            b.ClearBlock();
            yield return new WaitForSeconds(0.02F);
        }
        yield return 0;
    }

    /// <summary>
    /// Determines whether this instance can existing blocks placed the specified OnBoardBlockShapes.
    /// </summary>
    /// <returns><c>true</c> if this instance can existing blocks placed the specified OnBoardBlockShapes; otherwise, <c>false</c>.</returns>
    /// <param name="OnBoardBlockShapes">On board block shapes.</param>
    public bool CanExistingBlocksPlaced(List<ShapeInfo> OnBoardBlockShapes)
    {
        foreach (Block block in blockGrid)
        {
            if (!block.isFilled)
            {
                foreach (ShapeInfo info in OnBoardBlockShapes)
                {
                    bool canPlace = CheckShapeCanPlace(block, info);
                    if (canPlace)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks the shape can place.
    /// </summary>
    /// <returns><c>true</c>, if shape can place was checked, <c>false</c> otherwise.</returns>
    /// <param name="placingBlock">Placing block.</param>
    /// <param name="placingBlockShape">Placing block shape.</param>
    bool CheckShapeCanPlace(Block placingBlock, ShapeInfo placingBlockShape)
    {
        int currentRowID = placingBlock.rowID;
        int currentColumnID = placingBlock.columnID;

        if (placingBlockShape != null && placingBlockShape.ShapeBlocks != null)
        {
            foreach (ShapeBlock c in placingBlockShape.ShapeBlocks)
            {
                Block checkingCell = blockGrid.Find(o => o.rowID == currentRowID + (c.rowID + placingBlockShape.startOffsetX) && o.columnID == currentColumnID + (c.columnID - placingBlockShape.startOffsetY));

                if ((checkingCell == null) || (checkingCell != null && checkingCell.isFilled))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Raises the unable to place shape event.
    /// </summary>
    public void OnUnableToPlaceShape()
    {
        if ((TotalRescueDone < MaxAllowedRescuePerGame) || MaxAllowedRescuePerGame < 0)
        {
            GamePlayUI.Instance.ShowRescue(GameOverReason.OUT_OF_MOVES);
        }
        else
        {
            Debug.Log("GameOver Called..");
            OnGameOver();
        }
    }

    /// <summary>
    /// Raises the bomb counter over event.
    /// </summary>
    public void OnBombCounterOver()
    {
        if ((TotalRescueDone < MaxAllowedRescuePerGame) || MaxAllowedRescuePerGame < 0)
        {
            GamePlayUI.Instance.ShowRescue(GameOverReason.BOMB_COUNTER_ZERO);
        }
        else
        {
            Debug.Log("GameOver Called..");
            OnGameOver();
        }
    }

    [Button("ExecuteRescuse")]
    void ExecuteRescue()
    {
        int totalColumns = GameBoardGenerator.Instance.TotalColumns;
        int totalRows = GameBoardGenerator.Instance.TotalRows;


        int randomColumn = Random.Range(3, totalColumns);
        int randomRow = Random.Range(3, totalRows);

        int startingColumn = Random.Range(0, randomColumn);
        int startingRow = Random.Range(0, randomRow);

        List<List<Block>> breakingColumns = new List<List<Block>>();

        for (int columnIndex = startingColumn; columnIndex < randomColumn; columnIndex++)
        {
            breakingColumns.Add(GetEntireColumnForRescue(columnIndex));
        }

        List<List<Block>> breakingRows = new List<List<Block>>();

        for (int rowIndex = startingRow; rowIndex < randomRow; rowIndex++)
        {
            breakingRows.Add(GetEntireRowForRescue(rowIndex));
        }
        List<List<Block>> breakingCross = new List<List<Block>>();
        StartCoroutine(BreakAllCompletedLines(breakingRows, breakingColumns,breakingCross, 0));

        #region bomb mode
        if ((GameController.gameMode == GameMode.BLAST || GameController.gameMode == GameMode.CHALLENGE) && GamePlayUI.Instance.currentGameOverReason == GameOverReason.BOMB_COUNTER_ZERO)
        {
            List<Block> bombBlocks = blockGrid.FindAll(o => o.isBomb);
            foreach (Block block in bombBlocks)
            {
                if (block.bombCounter <= 1)
                {
                    block.SetCounter(block.bombCounter + 4);
                }
                block.DecreaseCounter();
            }
        }
        #endregion

        #region time mode
        if (GameController.gameMode == GameMode.TIMED || GameController.gameMode == GameMode.CHALLENGE)
        {
            if (GamePlayUI.Instance.currentGameOverReason == GameOverReason.TIME_OVER)
            {
                timeSlider.AddSeconds(30);
            }
            timeSlider.ResumeTimer();
        }
        #endregion
    }


    /// <summary>
    /// Ises the free rescue available.
    /// </summary>
    /// <returns><c>true</c>, if free rescue available was ised, <c>false</c> otherwise.</returns>
    public bool isFreeRescueAvailable()
    {
        if ((TotalFreeRescueDone < MaxAllowedVideoWatchRescue) || (MaxAllowedVideoWatchRescue < 0))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Raises the rescue done event.
    /// </summary>
    /// <param name="isFreeRescue">If set to <c>true</c> is free rescue.</param>
    public void OnRescueDone(bool isFreeRescue)
    {
        if (isFreeRescue)
        {
            TotalFreeRescueDone += 1;
        }
        else
        {
            TotalRescueDone += 1;
        }
        //CloseRescuePopup ();
        Invoke("ExecuteRescue", 0.5F);
    }

    /// <summary>
    /// Raises the rescue discarded event.
    /// </summary>
    public void OnRescueDiscarded()
    {
        //CloseRescuePopup ();
        Debug.Log("GameOver Called..");
        OnGameOver();
    }

    /// <summary>
    /// Closes the rescue popup.
    /// </summary>
    // void CloseRescuePopup()
    // {
    // 	GameObject currentWindow = StackManager.Instance.WindowStack.Peek ();
    // 	if (currentWindow != null) {
    // 		if (currentWindow.OnWindowRemove () == false) {
    // 			Destroy (currentWindow);
    // 		}
    // 		StackManager.Instance.WindowStack.Pop ();
    // 	}
    // }

    /// <summary>
    /// Raises the game over event.
    /// </summary>
    public void OnGameOver()
    {
        GameObject gameOverScreen = StackManager.Instance.gameOverScreen;
        gameOverScreen.Activate();
        int coinreward = 10;
        if ((int)(ScoreManager.Instance.Score / 10) > 10)
        {
            coinreward = (int)(ScoreManager.Instance.Score / 10);
        }
        gameOverScreen.GetComponent<GameOver>().SetLevelScore(ScoreManager.Instance.Score, coinreward);
        GameProgressManager.Instance.ClearProgress();
        StackManager.Instance.DeactivateGamePlay();
    }

    #region Bomb Mode Specific
    /// <summary>
    /// Updates the block count.
    /// </summary>
    void UpdateBlockCount()
    {
        List<Block> bombBlocks = blockGrid.FindAll(o => o.isBomb);
        foreach (Block block in bombBlocks)
        {
            block.DecreaseCounter();
        }

        bool doPlaceBomb = false;
        if (MoveCount <= 10)
        {
            if (MoveCount % 5 == 0)
            {
                doPlaceBomb = true;
            }
        }
        else if (MoveCount <= 30)
        {
            if (((MoveCount - 10) % 4 == 0))
            {
                doPlaceBomb = true;
            }

        }
        else if (MoveCount <= 48)
        {
            if (((MoveCount - 30) % 3 == 0))
            {
                doPlaceBomb = true;
            }
        }
        else
        {
            if (((MoveCount - 48) % 2 == 0))
            {
                doPlaceBomb = true;
            }
        }

        if (doPlaceBomb)
        {
            PlaceAtRandomPlace();
        }
    }

    /// <summary>
    /// Places at random place.
    /// </summary>
    void PlaceAtRandomPlace()
    {
        List<int> BombPlacingRows = new List<int>();

        for (int rowIndex = 0; rowIndex < GameBoardGenerator.Instance.TotalRows; rowIndex++)
        {
            bool canPlace = CheckRowForBombPlace(rowIndex);
            if (canPlace)
            {
                BombPlacingRows.Add(rowIndex);
            }
        }

        int RandomRowForPlacingBomb = 0;
        if (BombPlacingRows.Count > 0)
        {
            RandomRowForPlacingBomb = BombPlacingRows[UnityEngine.Random.Range(0, BombPlacingRows.Count)];
        }
        else
        {
            RandomRowForPlacingBomb = UnityEngine.Random.Range(0, GameBoardGenerator.Instance.TotalRows);
        }

        PlaceBombAtRow(RandomRowForPlacingBomb);
    }

    /// <summary>
    /// Places the bomb at row.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    void PlaceBombAtRow(int rowIndex)
    {
        List<Block> emptyBlockForThisRow = blockGrid.FindAll(o => o.rowID == rowIndex && o.isFilled == false);
        Block randomBlock = emptyBlockForThisRow[UnityEngine.Random.Range(0, emptyBlockForThisRow.Count)];

        if (randomBlock != null)
        {
            randomBlock.ConvertToBomb();
        }
    }

    /// <summary>
    /// Checks the row for bomb place.
    /// </summary>
    /// <returns><c>true</c>, if row for bomb place was checked, <c>false</c> otherwise.</returns>
    /// <param name="rowIndex">Row index.</param>
    bool CheckRowForBombPlace(int rowIndex)
    {
        Block block = blockGrid.Find(o => o.rowID == rowIndex && o.isFilled == true);
        int totalEmptyBlocks = blockGrid.FindAll(o => o.rowID == rowIndex && o.isFilled == false).Count;

        if (block != null && totalEmptyBlocks >= 2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    #endregion


    #region show help if mode opened first time
    /// <summary>
    /// Checks for help.
    /// </summary>
    public void CheckForHelp()
    {
        bool isHelpShown = false;
        if (GameController.gameMode == GameMode.BLAST || GameController.gameMode == GameMode.ADVANCE || GameController.gameMode == GameMode.TIMED)
        {
            isHelpShown = PlayerPrefs.GetInt("isHelpShown_" + GameController.gameMode.ToString(), 0) == 0 ? false : true;

            //isHelpShown = false;
            if (!isHelpShown)
            {
                InGameHelp inGameHelp = gameObject.AddComponent<InGameHelp>();
                inGameHelp.StartHelp();
            }
            else
            {
                ShowBasicHelp();
            }
        }
        else
        {
            ShowBasicHelp();
        }
    }

    /// <summary>
    /// Raises the help popup closed event.
    /// </summary>
    public void OnHelpPopupClosed()
    {
        if (GameController.gameMode == GameMode.TIMED)
        {
            timeSlider.ResumeTimer();
        }
        ShowBasicHelp();
    }

    /// <summary>
    /// Shows the basic help.
    /// </summary>
    void ShowBasicHelp()
    {
        bool isBasicHelpShown = PlayerPrefs.GetInt("isBasicHelpShown", 0) == 0 ? false : true;
        //isBasicHelpShown = false;
        if (!isBasicHelpShown)
        {
            InGameHelp inGameHelp = gameObject.GetComponent<InGameHelp>();
            if (inGameHelp == null)
            {
                inGameHelp = gameObject.AddComponent<InGameHelp>();
            }
            inGameHelp.ShowBasicHelp();
        }
    }
    #endregion
}