using System.Collections.Generic;
using Unity.VisualScripting;
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
        bool isContinue = (GameSceneManager.Instance != null) && GameSceneManager.Instance.GetContinue();

        foreach (Item item in itemData)
        {
            if (spawnedItems.ContainsKey(item.id)) continue;
            if (isContinue && item.id.Equals("IntroMerry")) continue;

            var savedInfo = savedData.itemPositions?.Find(x => x.itemId == item.id);

            //  1. 이 아이템이 내 가방(Storage)에 있거나 이미 사용(Used)했는가?
            bool isOwned = (savedInfo != null && (savedInfo.savedState == ItemState.Storage || savedInfo.savedState == ItemState.Used));

            //  2. 이 아이템이 현재 스테이지 바닥에 원래 떨어져 있어야 하는가?
            bool isCurrentStageItem = (item.chapterIndex == currentChapter && item.stageIndex == currentStage);

            // 가방에 있거나, 현재 스테이지 아이템일 때만 스폰 진행
            if (isOwned || isCurrentStageItem)
            {
                if (item.isHiddenInitially && savedInfo == null) continue;

                Vector2 spawnPos = (savedInfo != null) ? savedInfo.savedPos : item.oriPos;
                int spawnColor = (savedInfo != null) ? savedInfo.savedColor : item.originColor;

                //  3. 세이브 파일에 상태가 있으면 그대로 복구, 없으면 Field(바닥)로 설정
                item.currentState = (savedInfo != null) ? savedInfo.savedState : ItemState.Field;
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

            // 1. [기믹] 다른 아이템 획득 시 숨겨지는 처리
            if (!string.IsNullOrEmpty(data.targetItemToHideMe))
            {
                Item linkedItem = GetItemDataById(data.targetItemToHideMe);
                if (linkedItem != null && (linkedItem.currentState == ItemState.Storage || linkedItem.currentState == ItemState.Used))
                {
                    obj.SetActive(false);
                    continue;
                }
            }

            // 2. [가시성 핵심 로직] 상태에 따른 출력
            if (data.currentState == ItemState.Storage)
            {
                // 보관함(Storage)에 있는 아이템은 맵 이동과 무관하게 항상 활성화
                obj.SetActive(true);
            }
            else
            {
                // 바닥(Field)에 있거나 사용 완료(Used)된 아이템은 '원래 위치'일 때만 활성화
                bool isMyStage = (data.locationID == _currentLocation) &&
                                 (data.chapterIndex == currentChap) &&
                                 (data.stageIndex == currentStage);

                obj.SetActive(isMyStage);
            }
        }

        // 3. 문, 줌 패널 등 인터랙션 구역 업데이트
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

        GameObject frontObj = GameObject.Find("Front");

        if (data.id.Equals("105"))
        {
            if (frontObj != null)
            {
                newItem.transform.SetParent(frontObj.transform, false);
            }
            else
            {
                print("Front 오브젝트를 찾을 수 없습니다.");
            }
        }
        if (GameSceneManager.Instance.GetContinue() && data.id.Equals("IntroMerry"))
        {
            Destroy(data.prefab);
        }

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
        if (data != null)
        {
            // 💡 [핵심 보완] 상태를 덮어쓰기 전에 과거 상태(oldState)를 기억합니다.
            ItemState oldState = data.currentState;

            data.currentState = newState;
            data.changePos = newPos;

            // 1. 세이브 데이터 갱신 및 위치 저장
            ChangeItemPos(itemId, newPos);

            // 2. 아이템 가시성 갱신
            UpdateStageVisibility(_currentLocation);

            // 3. 💡 [기믹 호출] 과거 상태와 현재 상태를 비교할 수 있도록 같이 넘겨줍니다.
            HandleItemGimmicks(itemId, newState, oldState);
        }
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

        if (StageManager.Instance != null)
        {
            data.lastUnlockedChapter = StageManager.Instance.CurrentChapter();
            data.UnlockedStage = StageManager.Instance.CurrentStage();
        }
        data.isGameStarted = true;

        if (data.itemPositions == null) data.itemPositions = new List<ItemSaveInfo>();

        foreach (var entry in spawnedItems)
        {
            string id = entry.Key;
            GameObject obj = entry.Value;
            Item so = itemData.Find(x => x.id == id);

            if (so != null && obj != null)
            {
                RectTransform rt = obj.GetComponent<RectTransform>();
                Vector2 savePos = (rt != null) ? rt.anchoredPosition : (Vector2)obj.transform.position;

                var existingInfo = data.itemPositions.Find(x => x.itemId == id);
                if (existingInfo != null)
                {
                    existingInfo.savedPos = savePos;
                    existingInfo.savedColor = so.color;
                    existingInfo.savedState = so.currentState;
                }
                else
                {
                    data.itemPositions.Add(new ItemSaveInfo
                    {
                        itemId = id,
                        savedPos = savePos,
                        savedColor = so.color,
                        savedState = so.currentState
                    });
                }
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
        Item pumpkin = GetItemDataById("105");
        bool isPumpkinUsed = (pumpkin != null && pumpkin.currentState == ItemState.Used);

        Item clock = GetItemDataById("107");
        bool hasClock = (clock != null && (clock.currentState == ItemState.Storage || clock.currentState == ItemState.Used));

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
            if ((isPumpkinUsed || hasClock) && obj.gameObject.name == "Stage1_Door")
            {
                obj.isSolved = true;
                obj.gameObject.SetActive(false);
                continue;
            }

            if (hasClock && obj.gameObject.name == "YesClock")
            {
                obj.isSolved = true;
                obj.gameObject.SetActive(false);
                continue;
            }

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

    public void CloseAllZoomPanels()
    {
        ZoomOutTrigger[] allZoomOuts = Object.FindObjectsByType<ZoomOutTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var trigger in allZoomOuts)
        {
            if (trigger.myPanel != null && trigger.myPanel.activeSelf)
            {
                trigger.myPanel.SetActive(false);
            }
        }

        ToolBarController tool = Object.FindFirstObjectByType<ToolBarController>();
        if (tool != null)
        {
            // 💡 [수정 완료] 통합된 줌 함수 사용
            tool.SetZoomState(false);
        }

        Debug.Log("<color=cyan>[UI 클린업]</color> 모든 확대 패널을 닫았습니다.");
    }

    private void HandleItemGimmicks(string itemId, ItemState newState, ItemState oldState)
    {
        // 💡 [핵심 보완] 107번 시계가 '바닥(Field)'에서 '가방(Storage)'으로 처음 들어올 때만 작동!
        if (itemId == "107" && newState == ItemState.Storage && oldState == ItemState.Field)
        {
            Debug.Log("<color=lime>[Gimmick]</color> 107번 시계 초회 획득 기믹 발동!");

            // 1. 배경 및 문 상태 영구 고정 (ObjectVisibilityController 제어)
            ObjectVisibilityController[] allControllers = Object.FindObjectsByType<ObjectVisibilityController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ctrl in allControllers)
            {
                if (ctrl.gameObject.name == "YesClock" || ctrl.gameObject.name == "Stage1_Door")
                {
                    ctrl.isSolved = true;
                    ctrl.gameObject.SetActive(false);
                }
            }

            // 2. 현재 열려있는 모든 확대 패널 닫기
            CloseAllZoomPanels();

            // 3. 다른 팀원의 영역: 서재로 강제 복귀 (StoryConversionController)
            StoryConversionController conversionCtrl = Object.FindFirstObjectByType<StoryConversionController>();
            if (conversionCtrl != null)
            {
                conversionCtrl.ExitStory();
            }

            // 4. 아이템 가시성을 서재(Study) 기준으로 즉시 갱신
            UpdateStageVisibility("Study");
        }

        // 💡 나중에 새로운 아이템 기믹이 생기면 여기에 else if로 추가하면 됩니다!
    }
}