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

        SaveData savedData = SaveManager.Instance.Load();

        foreach (Item item in itemData)
        {
            if (item.chapterIndex == currentChapter && item.stageIndex == currentStage)
            {
                if (spawnedItems.ContainsKey(item.id)) continue;

                var savedInfo = savedData.itemPositions?.Find(x => x.itemId == item.id);

                // [핵심 방어막] 숨겨진 아이템이고 획득한 기록도 없다면 스폰 무시
                if (item.isHiddenInitially && savedInfo == null) continue;

                Vector2 spawnPos = (savedInfo != null) ? savedInfo.savedPos : item.oriPos;
                int spawnColor = (savedInfo != null) ? savedInfo.savedColor : item.originColor;

                item.currentState = ItemState.Field;
                item.color = spawnColor;

                CreateItemObject(item, spawnPos, spawnColor);
            }
        }

        UpdateStageVisibility(_currentLocation);

        PageController pageCtrl = Object.FindFirstObjectByType<PageController>();
        if (pageCtrl != null) pageCtrl.RefreshPageImage();
    }

    public void UpdateStageVisibility(string newLocation)
    {
        _currentLocation = newLocation;
        int currentChap = StageManager.Instance != null ? StageManager.Instance.CurrentChapter() : 1;
        int currentStage = StageManager.Instance != null ? StageManager.Instance.CurrentStage() : 1;

        foreach (var entry in spawnedItems)
        {
            string id = entry.Key;
            GameObject obj = entry.Value;
            Item data = GetItemDataById(id);

            if (data == null || obj == null) continue;

            if (!string.IsNullOrEmpty(data.targetItemToHideMe))
            {
                Item linkedItem = GetItemDataById(data.targetItemToHideMe);
                if (linkedItem != null && (linkedItem.currentState == ItemState.Storage || linkedItem.currentState == ItemState.Used))
                {
                    obj.SetActive(false);
                    continue;
                }
            }

            if (data.currentState == ItemState.Storage || data.currentState == ItemState.Used)
            {
                obj.SetActive(true);
            }
            else
            {
                bool isMyStage = (data.locationID == _currentLocation) &&
                                 (data.chapterIndex == currentChap) &&
                                 (data.stageIndex == currentStage);
                obj.SetActive(isMyStage);
            }
        }

        if (StageManager.Instance != null)
        {
            UpdateInteractZones(currentChap, currentStage);
        }
    }

    // [이벤트 소환용 함수]
    public void SpawnSpecificItem(string itemID)
    {
        Item item = GetItemDataById(itemID);
        if (item != null && !spawnedItems.ContainsKey(itemID))
        {
            item.currentState = ItemState.Field;
            CreateItemObject(item, item.oriPos, item.originColor);
            UpdateStageVisibility(_currentLocation);

            // 저장 로직 (방을 나갔다 들어와도 계속 스폰되도록 기록해둠)
            ChangeItemPos(itemID, item.oriPos);
            Debug.Log($"<color=lime>[이벤트 소환]</color> {itemID} 아이템이 나타났습니다!");
        }
    }

    private void CreateItemObject(Item data, Vector2 pos, int colorIndex)
    {
        if (data.prefab == null) return;

        GameObject newItem = Instantiate(data.prefab, itemParent);
        newItem.name = data.id;

        RectTransform rt = newItem.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = pos;
        else newItem.transform.position = pos;

        ApplyTypeLogic(newItem, data, colorIndex);

        if (!spawnedItems.ContainsKey(data.id))
            spawnedItems.Add(data.id, newItem);
    }

    private void ApplyTypeLogic(GameObject obj, Item data, int colorIndex)
    {
        if (data.type != ItemType.A && data.type != ItemType.B) return;
        Color targetColor = ColorManager.GetColor(colorIndex);
        Image uiImage = obj.GetComponentInChildren<Image>(true);
        if (uiImage != null) uiImage.color = targetColor;
    }

    public void UpdateItemStateAndPosition(string itemId, ItemState newState, Vector2 newPos)
    {
        Item data = GetItemDataById(itemId);
        if (data != null) { data.currentState = newState; data.changePos = newPos; ChangeItemPos(itemId, newPos); UpdateStageVisibility(_currentLocation); }
    }

    public void UpdateItemColor(string itemId, int newColorIndex)
    {
        Item data = GetItemDataById(itemId);
        if (data != null) { data.color = newColorIndex; if (spawnedItems.TryGetValue(itemId, out GameObject obj)) ApplyTypeLogic(obj, data, newColorIndex); ChangeItemPos(itemId, data.changePos); }
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
                RectTransform rt = obj.GetComponent<RectTransform>();
                Vector2 savePos = (rt != null) ? rt.anchoredPosition : (Vector2)obj.transform.position;
                data.itemPositions.Add(new ItemSaveInfo { itemId = id, savedPos = savePos, savedColor = so.color });
            }
        }
        SaveManager.Instance.Save(data);
    }

    public Item GetItemDataById(string searchId) => itemData.Find(item => item.id == searchId);

    public void ClearAllItems()
    {
        foreach (var item in spawnedItems.Values) { if (item != null) Destroy(item); }
        spawnedItems.Clear();
    }

    private void UpdateInteractZones(int chapter, int stage)
    {
        AnswerZone[] answerZones = Object.FindObjectsByType<AnswerZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var zone in answerZones)
        {
            if (zone.isMainZone) zone.gameObject.SetActive(zone.requiredChapter == chapter);
            else zone.gameObject.SetActive(zone.requiredChapter == chapter && zone.requiredPage == stage);
        }

        ZoomInTrigger[] zoomZones = Object.FindObjectsByType<ZoomInTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var zone in zoomZones)
        {
            zone.gameObject.SetActive(zone.requiredChapter == chapter && zone.requiredPage == stage);
        }

        ObjectVisibilityController[] storyObjects = Object.FindObjectsByType<ObjectVisibilityController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in storyObjects)
        {


            if (obj.isSolved) continue;

            bool shouldBeActive = false;

            if (obj.locationID == _currentLocation)
            {
                if (obj.isMainObject)
                {
                    if (obj.visibilityType == ObjectVisibilityController.VisibilityType.OnlyThisPage) shouldBeActive = (obj.requiredChapter == chapter);
                    else shouldBeActive = (chapter >= obj.requiredChapter);
                }
                else
                {
                    if (obj.visibilityType == ObjectVisibilityController.VisibilityType.OnlyThisPage) shouldBeActive = (obj.requiredChapter == chapter && obj.requiredPage == stage);
                    else shouldBeActive = (chapter > obj.requiredChapter) || (chapter == obj.requiredChapter && stage >= obj.requiredPage);
                }
            }
            obj.gameObject.SetActive(shouldBeActive);
        }



    }
}