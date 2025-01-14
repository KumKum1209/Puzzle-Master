
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
public class ClearPlayerData : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Clear Data")]
#endif
    static void Clear()
    {
        PlayerPrefs.DeleteAll();
        string[] filePaths = Directory.GetFiles(Application.persistentDataPath);
        foreach (string filePath in filePaths)
            if (filePath.Contains(".dat"))
                File.Delete(filePath);
    }
}

