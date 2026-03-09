using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StageData
{
    public string stageName;
    public List<Item> itemsToSpawn; // 해당 스테이지에서 생성할 SO 리스트
}

public class StageManager : MonoBehaviour
{
    public List<StageData> stages; // 인스펙터에서 스테이지별 아이템 설정
    private int currentStageIndex = -1;

    void Start()
    {
        // 첫 번째 스테이지 시작
        GoToNextStage();
    }

    public void GoToNextStage()
    {
        currentStageIndex++;

        if (currentStageIndex < stages.Count)
        {
            StartStage(currentStageIndex);
        }
        else
        {
            print("모든 스테이지 클리어!");
        }
    }

    private void StartStage(int index)
    {
        // 1. 기존 아이템 모두 파괴
        ItemManager.Instance.ClearAllItems();

        // 2. 새로운 스테이지 아이템 생성
        StageData currentStage = stages[index];
        foreach (Item itemSO in currentStage.itemsToSpawn)
        {
            ItemManager.Instance.SpawnItem(itemSO);
        }

        print($"{currentStage.stageName} 시작됨.");
    }
}