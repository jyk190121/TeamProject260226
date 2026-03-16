using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("모든 스테이지 아이템 데이터")]
    public List<Item> itemData = new List<Item>();

    [Header("UI 부모 설정 (단일 대기실)")]
    public Transform itemParent;

    private Dictionary<string, GameObject> spawnedItems = new Dictionary<string, GameObject>();
    private string _currentLocation = "Main";

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void SpawnItem(int currentChapter, int currentStage)
    {
        if (currentChapter <= 0 || currentStage <= 0) return;

        ClearAllItems();
        SaveData savedData = SaveManager.Instance.Load();

        foreach (Item item in itemData)
        {
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

        // [추가됨] 아이템 깔기 끝난 후, 씬에 있는 정답/확대 존들도 현재 페이지에 맞게 켜고 끄기
        UpdateInteractZones(currentChapter, currentStage);
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
        Color targetColor = ColorManager.GetColor(colorIndex);
        Image uiImage = obj.GetComponentInChildren<Image>(true);
        if (uiImage != null) uiImage.color = targetColor;
    }

    public void UpdateStageVisibility(string newLocation)
    {
        _currentLocation = newLocation;

        foreach (var entry in spawnedItems)
        {
            string id = entry.Key;
            GameObject obj = entry.Value;
            Item data = GetItemDataById(id);

            if (data == null || obj == null) continue;

            if (data.currentState == ItemState.Storage || data.currentState == ItemState.Used)
            {
                obj.SetActive(true);
            }
            else
            {
                bool isMyStage = (data.locationID == _currentLocation);
                obj.SetActive(isMyStage);
            }
        }
        Debug.Log($"<color=green>[Visibility]</color> '{_currentLocation}' 화면 기준으로 아이템 필터링 완료");
    }

    public void UpdateItemStateAndPosition(string itemId, ItemState newState, Vector2 newPos)
    {
        Item data = GetItemDataById(itemId);
        if (data != null)
        {
            data.currentState = newState;
            data.changePos = newPos;
            ChangeItemPos(itemId, newPos);
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

    public void ClearAllItems()
    {
        foreach (var item in spawnedItems.Values)
        {
            if (item != null) Destroy(item);
        }
        spawnedItems.Clear();
        Debug.Log("<color=red>[System]</color> 이전 스테이지 아이템 데이터 완전 파괴 완료.");
    }

    // [추가됨] 씬에 있는 Zone들을 페이지 진행도에 따라 자동으로 On/Off 해주는 함수
    // [변경됨] isMainZone 체크 여부에 따라 끄고 켜는 조건 분리
    private void UpdateInteractZones(int chapter, int stage)
    {
        AnswerZone[] answerZones = Object.FindObjectsByType<AnswerZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var zone in answerZones)
        {
            // 메인 존이면 챕터만 맞으면 무조건 켜둠, 서브 존이면 페이지까지 맞아야 켜짐
            if (zone.isMainZone)
                zone.gameObject.SetActive(zone.requiredChapter == chapter);
            else
                zone.gameObject.SetActive(zone.requiredChapter == chapter && zone.requiredPage == stage);
        }

        ZoomInTrigger[] zoomZones = Object.FindObjectsByType<ZoomInTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var zone in zoomZones)
        {
            // ZoomInTrigger도 추후 메인 존에 쓸 수 있으니 확장성을 위해 로직을 맞춰둠 (필요시 ZoomInTrigger 스크립트에도 isMainZone 변수 추가 가능)
            zone.gameObject.SetActive(zone.requiredChapter == chapter && zone.requiredPage == stage);
        }

        Debug.Log($"<color=cyan>[Zone Update]</color> {chapter}-{stage} 페이지용 상호작용 구역(Zone) 켜기/끄기 동기화 완료.");
    }
}