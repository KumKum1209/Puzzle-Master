using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using NaughtyAttributes;
using DG.Tweening;

public class ShapeInfo : MonoBehaviour 
{
	public int ShapeID = 0;
	[HideInInspector] public ShapeBlock firstBlock;
	[HideInInspector] public Sprite blockImage = null;
	[HideInInspector] public int startOffsetX = 0;
	[HideInInspector] public int startOffsetY = 0;

	public List<ShapeBlock> ShapeBlocks;

	void Start()
	{
		CreateBlockList ();

		firstBlock = ShapeBlocks [0];
		blockImage = firstBlock.block.GetComponent<Image>().sprite;
		startOffsetX = firstBlock.rowID;
		startOffsetY = firstBlock.columnID;
	}

	void CreateBlockList()
	{
		ShapeBlocks = new List<ShapeBlock> ();
		List<Transform> shapeAllBlocks = transform.GetComponentsInChildren<Transform> ().ToList ();

		if (shapeAllBlocks.Contains (transform)) {
			shapeAllBlocks.Remove (transform);
		}

		foreach (Transform block in shapeAllBlocks) {
			string[] blockNameSplit =  block.name.Split ('-');

			if (blockNameSplit.Length == 3) {
				int rowID = blockNameSplit [1].TryParseInt ();
				int columnID = blockNameSplit [2].TryParseInt ();
			
				ShapeBlock thisBlock = new ShapeBlock (block, rowID, columnID);
				if (!ShapeBlocks.Contains (thisBlock)) {
					ShapeBlocks.Add (thisBlock);
				}
			}
		}
	}
	[Button]
    public void RotateShape()
    {
		gameObject.transform.DORotate(new Vector3(0, 0, transform.rotation.z + 90), 0.2f);
        int originalStartOffsetX = startOffsetX;
        int originalStartOffsetY = startOffsetY;

        for (int i = 0; i < ShapeBlocks.Count; i++)
        {
            int originalRowID = ShapeBlocks[i].rowID;
            int originalColumnID = ShapeBlocks[i].columnID;

            int newRowID = originalStartOffsetX - originalColumnID;
            int newColumnID = originalStartOffsetY + originalRowID - originalStartOffsetX;

            ShapeBlocks[i].rowID = newRowID;
            ShapeBlocks[i].columnID = newColumnID;
        }

        startOffsetX = ShapeBlocks[0].rowID;
        startOffsetY = ShapeBlocks[0].columnID;
        firstBlock = ShapeBlocks[0];
        blockImage = firstBlock.block.GetComponent<Image>().sprite;

    }
}

public class ShapeBlock
{
	public Transform block;
	public int rowID;
	public int columnID;

	public ShapeBlock(Transform _block, int _rowID, int _columnID)
	{
		this.block = _block;
		this.rowID = _rowID;
		this.columnID = _columnID;
	}
}


