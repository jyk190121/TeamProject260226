using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("모든 스테이지 아이템 데이터")]
    public List<Item> itemData = new List<Item>();

    [Header("UI 부모 설정")]
    public Transform itemParent; // [필수] Canvas 내부의 패널 연결

    private Dictionary<string, GameObject> spawnedItems = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void SpawnItem(int stageIndex)
    {
        if (stageIndex <= 0) return;
        SaveData savedData = SaveManager.Instance.Load();

        foreach (Item item in itemData)
        {
            if (item.stageIndex == stageIndex)
            {
                var savedInfo = savedData.itemPositions?.Find(x => x.itemId == item.id);

                // 1. 위치: 세이브가 있으면 세이브값, 없으면 SO의 oriPos
                Vector2 spawnPos = (savedInfo != null) ? savedInfo.savedPos : item.oriPos;

                // 2. 색상: 세이브가 있으면 세이브값, 없으면 SO의 originColor
                int spawnColor = (savedInfo != null) ? savedInfo.savedColor : item.originColor;

                item.color = spawnColor; // 인게임 데이터 동기화

                CreateItemObject(item, spawnPos, spawnColor);
            }
        }
    }

    private void CreateItemObject(Item data, Vector2 pos, int colorIndex)
    {
        if (data.prefab == null) return;

        // 1. [핵심] 부모를 지정해서 일단 생성만 합니다. (Instantiate에서 위치 강제 배정 금지)
        GameObject newItem = Instantiate(data.prefab, itemParent);
        newItem.name = data.id;

        // 2. [최초 좌표 세팅 핵심]
        // Overlay 캔버스에서는 transform.position이 화면의 픽셀 좌표와 일치합니다.
        // ToolBarController의 마우스 드롭 로직과 동일하게 transform.position에 직접 대입합니다.
        newItem.transform.position = pos;

        // 3. 색상 적용
        ApplyTypeLogic(newItem, data, colorIndex);

        if (!spawnedItems.ContainsKey(data.id))
            spawnedItems.Add(data.id, newItem);
    }

    private void ApplyTypeLogic(GameObject obj, Item data, int colorIndex)
    {
        ColorManager colorMgr = Object.FindFirstObjectByType<ColorManager>();
        if (colorMgr == null) return;

        Color targetColor = colorMgr.GetColor(colorIndex);

        // 부모/자식 어디에 있든 Image 컴포넌트를 찾아 색상 적용
        Image uiImage = obj.GetComponentInChildren<Image>(true);
        if (uiImage != null)
        {
            uiImage.color = targetColor;
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

        // 화면에 있는 모든 아이템의 '현재 픽셀 좌표(transform.position)'를 저장합니다.
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
    public void ClearAllItems() { foreach (var item in spawnedItems.Values) if (item != null) Destroy(item); spawnedItems.Clear(); }
}