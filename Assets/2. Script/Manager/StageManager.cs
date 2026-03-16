using NUnit.Framework;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    // 메인챕터 장수
    public int totalChapter = 6;
    // 현재 플레이중인 챕터
    int currentChapterIndex = 1;

    // 현재 플레이중인 스테이지
    int currentStage = 1;

    // 스테이지 클리어 시 발생하는 이벤트
    public static System.Action OnChapterCleared;
    public static StageManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);                      // 이 객체를 씬이 바뀌어도 보존
        }
        else
        {
            Destroy(gameObject);                                // 중복 생성된 객체는 제거
        }
    }

    void Start()
    {
        // 스테이지 값 받아오기 (SaveManager)

        // 저장된 데이터 로드
        SaveData data = SaveManager.Instance.Load();
        currentChapterIndex = data.lastUnlockedChapter;
        currentStage = data.UnlockedStage;

        // 로드된 스테이지 시작
        StartStage(currentChapterIndex, currentStage);
    }

    public void ClearChapter()
    {
        //// 1. 현재 맵에 있는 아이템들의 위치를 SO 데이터(changePos)로 동기화
        //ItemManager.Instance.UpdateAllItemPositions();

        //// 2. 저장 데이터 생성
        //SaveData data = new SaveData();
        //data.lastUnlockedStage = currentStageIndex + 1; // 다음 스테이지 번호

        //// 3. 현재 스테이지의 아이템 위치 정보들 리스트에 담기
        //foreach (Item item in ItemManager.Instance.itemData)
        //{
        //    // 위치가 변한(이동된) 아이템만 저장하거나 전체 저장
        //    if (item.stageIndex == currentStageIndex)
        //    {
        //        data.itemPositions.Add(new ItemSaveInfo
        //        {
        //            itemId = item.id,
        //            savedPos = item.changePos, // 업데이트된 changePos 저장
        //            savedColor = item.color    // 업데이트된 Color 저장
        //        });
        //    }
        //}

        SaveData data = new SaveData();
        data.lastUnlockedChapter = currentChapterIndex;

        data.itemPositions.Clear(); // 다음 스테이지는 초기값으로 시작하도록 비움

        // "나 챕터 깼어!"라고 방송함
        OnChapterCleared?.Invoke();

        // JSON 저장
        SaveManager.Instance.Save(data);

        // 다음 챕터 진행
        currentChapterIndex++;

        // 스테이지 초기화
        currentStage = 1;

        if (currentChapterIndex < totalChapter)
        {
            StartStage(currentChapterIndex, currentStage);
        }
        else
        {
            print("모든 챕터 클리어!");
        }
    }

    public void ClearStage()
    {
        currentStage++;
    }

    // 스테이지 시작 시 
    public void StartStage(int main, int sub)
    {
        currentChapterIndex = main;
        currentStage = sub;

        // 기존 존재하는 아이템 파괴
        ItemManager.Instance.ClearAllItems();

        ItemManager.Instance.SpawnItem(currentChapterIndex, currentStage);

        print($"{currentChapterIndex} 시작됨");
    }

    // 스테이지 리셋
    public void ResetGame()
    {
        // 저장 파일 삭제 혹은 초기화 데이터 덮어쓰기
        SaveData emptyData = new SaveData();
        SaveManager.Instance.Save(emptyData);

        // SO들의 changePos도 초기화
        foreach (Item item in ItemManager.Instance.itemData) item.changePos = item.oriPos;

        // 1챕터 1스테이지부터 다시 시작
        StartStage(1, 1);
    }

    public int CurrentStage()
    {
        return currentStage;
    }

    public int CurrentChapter()
    {
        return currentChapterIndex;
    }
}