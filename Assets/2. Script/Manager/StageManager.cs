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

    public static System.Action<int> OnChapterStarted;
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
        SaveData data = SaveManager.Instance.Load();

        int clearedChapter = currentChapterIndex;

        // 다음 챕터 진행
        currentChapterIndex++;

        data.lastUnlockedChapter = currentChapterIndex;
        data.UnlockedStage = 1;
        data.itemPositions.Clear(); // 다음 스테이지는 초기값으로 시작하도록 비움

        // JSON 저장
        SaveManager.Instance.Save(data);

        // "나 챕터 깼어!"라고 방송함
        OnChapterCleared?.Invoke();

        if (currentChapterIndex <= totalChapter)
        {
            StartStage(currentChapterIndex, 1);
            //currentChapterIndex의 스토리로 진행되어져야 함
            OnChapterStarted?.Invoke(clearedChapter);
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

    void StartChapter(int main)
    {
        OnChapterStarted?.Invoke(main);
        print($"{currentChapterIndex}장의 {currentStage}스테이지 시작됨");
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