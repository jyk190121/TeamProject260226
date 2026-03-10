using System.Collections.Generic;
using System.Linq; // 리스트 필터링을 위해 추가
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("모든 스테이지 아이템 데이터")]
    public List<Item> itemData = new List<Item>();

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
    public void SpawnItem(int stageIndex)
    {
        if (stageIndex <= 0)
        {
            print("스테이지 번호가 유효하지 않습니다.");
            return;
        }

        SaveData savedData = SaveManager.Instance.Load();

        // itemData 리스트에서 stageIndex가 일치하는 아이템들만 추출
        // (Linq를 사용하거나 foreach문으로 필터링 가능)
        foreach (Item item in itemData)
        {
            if (item.stageIndex == stageIndex)
            {
                // 저장된 위치가 있는지 검색
                var savedInfo = savedData.itemPositions.Find(x => x.itemId == item.id);

                // 1. 위치 결정
                Vector2 spawnPos = (savedInfo != null) ? savedInfo.savedPos : item.oriPos;

                // 2. 색상 결정 (저장된 값이 있으면 사용, 없으면 SO의 기본 color 사용)
                int spawnColor = (savedInfo != null) ? savedInfo.savedColor : item.color;

                CreateItemObject(item, spawnPos, spawnColor);
            }
        }
        //if (itemData == null || itemData.prefab == null) return;

        //// 같은 아이템이 여러 개일 수 있으므로 ID 생성 방식을 보완 (예: ID_순번)
        //string uniqueKey = itemData.id + "_" + spawnedItems.Count;

        //GameObject newItem = Instantiate(itemData.prefab, itemData.oriPos, Quaternion.identity);
        //newItem.name = uniqueKey;

        //ApplyTypeLogic(newItem, itemData);
        //spawnedItems.Add(uniqueKey, newItem);

        //// 1. 프리팹 생성
        //GameObject newItem = Instantiate(itemData.prefab, itemData.oriPos, Quaternion.identity);
        //newItem.name = itemData.id;

        //// 2. 타입별 로직 처리
        //ApplyTypeLogic(newItem, itemData);

        //spawnedItems.Add(itemData.id, newItem);
    }

    // 실제 생성 로직을 별도 메서드로 분리 (가독성)
    private void CreateItemObject(Item data, Vector2 pos, int colorIndex)
    {
        if (data.prefab == null) return;

        // 생성할 때 고유 ID 부여
        GameObject newItem = Instantiate(data.prefab, pos, Quaternion.identity);
        newItem.name = data.id; // 나중에 위치를 업데이트할 때 찾기 위해 원본 id 사용

        ApplyTypeLogic(newItem, data, colorIndex);

        // 딕셔너리에 추가 (key: 아이템ID, value: 게임오브젝트)
        if (!spawnedItems.ContainsKey(data.id))
        { 
            spawnedItems.Add(data.id, newItem);
        }
    }

    private void ApplyTypeLogic(GameObject obj, Item data, int colorIndex)
    {
        // 공통 기능: 들기 가능 (예: Pickup 스크립트가 있다고 가정)
        // obj.AddComponent<PickupAbility>(); 

        if (data.type == ItemType.A)
        {
            // A 타입 전용: 색상 가져오기 및 적용
            var renderer = obj.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                //renderer.material.color = GetColorByIndex(data.color);
                ColorManager colorMgr = Object.FindFirstObjectByType<ColorManager>();
                if (colorMgr != null)
                {
                    renderer.color = colorMgr.GetColor(colorIndex);
                }
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

    public void ClearAllItems()
    {
        foreach (var item in spawnedItems.Values)
        {
            if (item != null) Destroy(item);
        }
        spawnedItems.Clear();
        print("아이템이 파괴되었습니다");
    }

    /// <summary>
    /// 특정 시점에 모든 아이템의 현재 위치를 SO의 changePos에 동기화하고 저장 준비
    /// </summary>
    public void UpdateAllItemPositions()
    {
        foreach (var entry in spawnedItems)
        {
            string id = entry.Key;
            GameObject obj = entry.Value;

            // 리스트에서 해당 ID를 가진 SO 찾기
            Item data = itemData.Find(x => x.id == id);
            if (data != null && obj != null)
            {
                // 현재 월드 위치를 SO의 changePos에 저장
                data.changePos = obj.transform.position;
            }
        }
    }
    /// <summary>
    /// 외부(예: 드래그 종료 시)에서 특정 아이템의 위치를 업데이트할 때 호출
    /// </summary>
    public void ChangeItemPos(string itemId, Vector2 newPos)
    {
        //Item data = itemData.Find(x => x.id == itemId);
        //if (data != null)
        //{
        //    data.changePos = newPos;
        //    print($"{itemId}의 위치가 {newPos}로 변경되었습니다.");
        //}
        SaveData data = new SaveData();

        foreach (var entry in spawnedItems) // ItemManager의 딕셔너리 순회
        {
            string id = entry.Key;
            GameObject obj = entry.Value;
            Item so = itemData.Find(x => x.id == id);

            if (so != null && obj != null)
            {
                data.itemPositions.Add(new ItemSaveInfo
                {
                    itemId = id,
                    savedPos = obj.transform.position,
                    savedColor = so.color // 실시간으로 갱신된 SO의 색상값
                });
            }
        }
        SaveManager.Instance.Save(data);
    }

}