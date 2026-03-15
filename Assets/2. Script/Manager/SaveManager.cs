using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public bool isGameStarted = false;    // 진행 중인 게임이 있는지 저장

    public int lastUnlockedChapter = 1; // 마지막으로 도달한 챕터 (메인)
    public int UnlockedStage = 1;       // 플레이중인 스테이지 (서브)
    public List<ItemSaveInfo> itemPositions = new List<ItemSaveInfo>();

    // 볼륨 데이터 (기본값 1)
    public float volMaster = 1.0f;
    public float volBgm = 1.0f;
    public float volSfx = 1.0f;
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

    // 단순 파일 존재 여부 (설정 로드 등을 위해 필요할 수 있음)
    public bool HasSaveData()
    {
        return File.Exists(savePath);
    }
    public bool CanContinue()
    {
        if (!File.Exists(savePath)) return false;

        // 파일을 로드해서 진행 플래그 확인
        SaveData data = Load();
        return data.isGameStarted;
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            print("기존 저장 데이터 삭제 완료");
        }
    }

    // 새 게임 시작 시 (사운드 유지)
    public void ResetGame()
    {
        if (File.Exists(savePath))
        {
            SaveData currentData = Load();

            SaveData newData = new SaveData();
            newData.volMaster = currentData.volMaster;
            newData.volBgm = currentData.volBgm;
            newData.volSfx = currentData.volSfx;

            Save(newData); // 덮어쓰기
            print("게임 진행도만 초기화되었습니다. (볼륨 유지)");
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
