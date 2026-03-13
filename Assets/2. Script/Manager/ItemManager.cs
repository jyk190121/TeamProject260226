using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("모든 스테이지 아이템 데이터")]
    public List<Item> itemData = new List<Item>();

    [Header("UI 부모 설정 (단일 대기실)")]
    public Transform itemParent; // Canvas 내부의 Pnl_Item 연결

    // 생성된 아이템들을 추적하는 딕셔너리
    private Dictionary<string, GameObject> spawnedItems = new Dictionary<string, GameObject>();

    // 현재 카메라가 비추고 있는 무대 이름 (기본값: Main)
    private string _currentLocation = "Main";

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // ---------------------------------------------------
    // [기능 1] 스테이지 시작 (아이템 일괄 생성)
    // ---------------------------------------------------
    public void SpawnItem(int currentChapter, int currentStage)
    {
        if (currentChapter <= 0 || currentStage <= 0) return;

        ClearAllItems();

        SaveData savedData = SaveManager.Instance.Load();

        foreach (Item item in itemData)
        {
            // [핵심 변경] Main과 Sub가 현재 진행도와 완벽히 일치하는 아이템만 골라서 생성합니다.
            if (item.chapterIndex == currentChapter && item.stageIndex == currentStage)
            {
                var savedInfo = savedData.itemPositions?.Find(x => x.itemId == item.id);

                Vector2 spawnPos = (savedInfo != null) ? savedInfo.savedPos : item.oriPos;
                int spawnColor = (savedInfo != null) ? savedInfo.savedColor : item.originColor;

                item.currentState = ItemState.Field;
                item.color = spawnColor;

                CreateItemObject(item, spawnPos, spawnColor);
            }
        }

        UpdateStageVisibility(_currentLocation);
    }

    private void CreateItemObject(Item data, Vector2 pos, int colorIndex)
    {
        if (data.prefab == null) return;

        GameObject newItem = Instantiate(data.prefab, itemParent);
        newItem.name = data.id;
        newItem.transform.position = pos;

        ApplyTypeLogic(newItem, data, colorIndex);

        if (!spawnedItems.ContainsKey(data.id))
            spawnedItems.Add(data.id, newItem);
    }

    private void ApplyTypeLogic(GameObject obj, Item data, int colorIndex)
    {
        ColorManager colorMgr = Object.FindFirstObjectByType<ColorManager>();
        if (colorMgr == null) return;

        Color targetColor = colorMgr.GetColor(colorIndex);
        Image uiImage = obj.GetComponentInChildren<Image>(true);
        if (uiImage != null) uiImage.color = targetColor;
    }

    // ---------------------------------------------------
    // [기능 2] 무대 조명 켜기/끄기 (핵심 가시성 제어)
    // ---------------------------------------------------
    public void UpdateStageVisibility(string newLocation)
    {
        _currentLocation = newLocation; // 현재 무대 이름 갱신

        foreach (var entry in spawnedItems)
        {
            string id = entry.Key;
            GameObject obj = entry.Value;
            Item data = GetItemDataById(id);

            if (data == null || obj == null) continue;

            // [우선순위 필터링]
            if (data.currentState == ItemState.Storage || data.currentState == ItemState.Used)
            {
                // 1순위: 보관함(Storage)에 있거나 정답으로 사용(Used)되었다면 무조건 켜둠
                obj.SetActive(true);
            }
            else // 2순위: 바닥(Field)에 있다면 출신지(locationID) 검사
            {
                bool isMyStage = (data.locationID == _currentLocation);
                obj.SetActive(isMyStage);
            }
        }
        Debug.Log($"<color=green>[Visibility]</color> '{_currentLocation}' 화면 기준으로 아이템 필터링 완료");
    }

    // ---------------------------------------------------
    // [기능 3] 데이터 갱신 (상태, 위치, 색상)
    // ---------------------------------------------------
    public void UpdateItemStateAndPosition(string itemId, ItemState newState, Vector2 newPos)
    {
        Item data = GetItemDataById(itemId);
        if (data != null)
        {
            data.currentState = newState;
            data.changePos = newPos;
            ChangeItemPos(itemId, newPos);

            // 상태가 변했으니 화면에 즉시 반영 (예: 바닥->보관함 이동 시)
            UpdateStageVisibility(_currentLocation);
        }
    }

    public void UpdateItemColor(string itemId, int newColorIndex)
    {
        Item data = GetItemDataById(itemId);
        if (data != null)
        {
            data.color = newColorIndex;
            if (spawnedItems.TryGetValue(itemId, out GameObject obj))
                ApplyTypeLogic(obj, data, newColorIndex);

            ChangeItemPos(itemId, data.changePos);
        }
    }

    public void UpdateItemPosition(string itemId, Vector2 newPos)
    {
        Item data = GetItemDataById(itemId);
        if (data != null) { data.changePos = newPos; ChangeItemPos(itemId, newPos); }
    }

    // ---------------------------------------------------
    // [기능 4] 세이브 및 스테이지 완전 정리
    // ---------------------------------------------------
    public void ChangeItemPos(string itemId, Vector2 newPos)
    {
        SaveData data = SaveManager.Instance.Load();
        if (data.itemPositions == null) data.itemPositions = new List<ItemSaveInfo>();
        data.itemPositions.Clear();

        foreach (var entry in spawnedItems)
        {
            string id = entry.Key;
            GameObject obj = entry.Value;
            Item so = itemData.Find(x => x.id == id);
            if (so != null && obj != null)
            {
                data.itemPositions.Add(new ItemSaveInfo { itemId = id, savedPos = obj.transform.position, savedColor = so.color });
            }
        }
        SaveManager.Instance.Save(data);
    }

    public Item GetItemDataById(string searchId) => itemData.Find(item => item.id == searchId);

    // [핵심] 스테이지가 완전히 끝날 때 호출하여 찌꺼기를 날려버리는 함수
    public void ClearAllItems()
    {
        foreach (var item in spawnedItems.Values)
        {
            if (item != null) Destroy(item);
        }
        spawnedItems.Clear();
        Debug.Log("<color=red>[System]</color> 이전 스테이지 아이템 데이터 완전 파괴 완료.");
    }
}