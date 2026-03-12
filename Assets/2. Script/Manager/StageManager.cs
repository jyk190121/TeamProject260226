using NUnit.Framework.Interfaces;
using UnityEngine;


public class StageManager : MonoBehaviour
{
    // 총 스테이지
    public int totalStage = 6;
    private int currentStageIndex = 1;

    void Start()
    {
        // 스테이지 값 받아오기 (SaveManager)

        // 저장된 데이터 로드
        SaveData data = SaveManager.Instance.Load();
        currentStageIndex = data.lastUnlockedStage;

        // 로드된 스테이지 시작
        StartStage(currentStageIndex);
    }

    public void ClearStage()
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
        data.lastUnlockedStage = currentStageIndex;
        data.itemPositions.Clear(); // 다음 스테이지는 초기값으로 시작하도록 비움

        // JSON 저장
        SaveManager.Instance.Save(data);

        // 다음 스테이지 진행
        currentStageIndex++;

        if (currentStageIndex < totalStage)
        {
            StartStage(currentStageIndex);
        }
        else
        {
            print("모든 스테이지 클리어!");
        }
    }

    // 스테이지 시작 시 
    public void StartStage(int index)
    {
        currentStageIndex = index;

        // 기존 존재하는 아이템 파괴
        ItemManager.Instance.ClearAllItems();

        ItemManager.Instance.SpawnItem(currentStageIndex);

        print($"{currentStageIndex} 시작됨");
    }

    // 스테이지 리셋
    public void ResetGame()
    {
        // 저장 파일 삭제 혹은 초기화 데이터 덮어쓰기
        SaveData emptyData = new SaveData();
        SaveManager.Instance.Save(emptyData);

        // SO들의 changePos도 초기화
        foreach (Item item in ItemManager.Instance.itemData) item.changePos = item.oriPos;

        // 1스테이지부터 다시 시작
        StartStage(1);
    }
}