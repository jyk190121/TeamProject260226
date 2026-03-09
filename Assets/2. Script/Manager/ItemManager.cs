using UnityEngine;
using System.Collections.Generic;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    public List<Item> itemList;

    // 현재 씬에 생성된 아이템들을 추적하기 위한 딕셔너리 (ID 기반)
    private Dictionary<string, GameObject> spawnedItems = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    /// <summary>
    /// 아이템 생성 및 타입별 기능 부여
    /// </summary>
    public void SpawnItem(Item itemData)
    {
        if (itemData == null || itemData.prefab == null) return;
        if (spawnedItems.ContainsKey(itemData.id)) return;

        // 1. 프리팹 생성
        GameObject newItem = Instantiate(itemData.prefab, itemData.oriPos, Quaternion.identity);
        newItem.name = itemData.id;

        // 2. 타입별 로직 처리
        ApplyTypeLogic(newItem, itemData);

        spawnedItems.Add(itemData.id, newItem);
    }

    private void ApplyTypeLogic(GameObject obj, Item data)
    {
        // 공통 기능: 들기 가능 (예: Pickup 스크립트가 있다고 가정)
        // obj.AddComponent<PickupAbility>(); 

        if (data.type == ItemType.A)
        {
            // A 타입 전용: 색상 가져오기 및 적용
            var renderer = obj.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.material.color = GetColorByIndex(data.color);
            }
            print($"{data.id} 생성: [A타입] 들기 및 색상 적용 완료");
        }
        else if (data.type == ItemType.B)
        {
            // B 타입 전용: 들기만 가능 (색상 로직 건너뜀)
            print($"{data.id} 생성: [B타입] 들기 기능만 활성화");
        }
    }

    /// <summary>
    /// 아이템 파괴
    /// </summary>
    public void RemoveItem(string itemId)
    {
        if (spawnedItems.TryGetValue(itemId, out GameObject obj))
        {
            Destroy(obj);
            spawnedItems.Remove(itemId);
        }
    }

    private Color GetColorByIndex(int index)
    {
        return index switch
        {
            0 => Color.red,
            1 => Color.green,
            2 => Color.blue,
            _ => Color.white
        };
    }

    public void ClearAllItems()
    {
        foreach (var item in spawnedItems.Values)
        {
            if (item != null) Destroy(item);
        }
        spawnedItems.Clear();
        print("모든 스테이지 아이템이 제거되었습니다.");
    }

    public void ChangeItemPos(Item data, Vector2 vec)
    {
        data.changePos = vec;
    }
}