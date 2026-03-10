using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public int lastUnlockedStage = 1; // 마지막으로 도달한 스테이지
    public List<ItemSaveInfo> itemPositions = new List<ItemSaveInfo>();
}

[System.Serializable]
public class ItemSaveInfo
{
    public string itemId;
    public Vector2 savedPos;
    public int savedColor;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string savePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            savePath = Path.Combine(Application.persistentDataPath, "savefile.json");
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"저장 완료: {savePath}");
    }

    public SaveData Load()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        return new SaveData(); // 파일이 없으면 새 데이터 반환
    }


}
