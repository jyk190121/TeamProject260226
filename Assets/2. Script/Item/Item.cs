using UnityEngine;

public enum ItemState { Field, Storage, Used }
public enum ItemType { A, B, C }

[CreateAssetMenu(fileName = "New Item", menuName = "Item")]
public class Item : ScriptableObject
{
    [Header("--- [불변] 초기 스펙 ---")]
    public string id;
    public string locationID = "Sub";

    [Header("스토리 등장 조건")]
    public int chapterIndex;
    public int stageIndex;

    // ==========================================
    // [핵심] 이 체크박스를 켜면 방에 처음 들어올 땐 투명인간 취급!
    // ==========================================
    [Tooltip("체크 시 방에 들어올 때 자동 스폰되지 않고 이벤트(SpawnSpecificItem)로만 등장합니다.")]
    public bool isHiddenInitially = false;

    public Vector2 oriPos;
    public int originColor;

    [Header("타입 - A: 들기&색상 / B: 색상만 / C: 확대")]
    public ItemType type;
    public GameObject prefab;

    [Header("--- [스토리 기믹] ---")]
    public string targetItemToHideMe;

    [Header("--- [변동] 인게임 상태 ---")]
    [System.NonSerialized] public ItemState currentState;
    [System.NonSerialized] public Vector2 changePos;
    [System.NonSerialized] public int color;

    public void ResetToOriginal()
    {
        changePos = oriPos;
        color = originColor;
        currentState = ItemState.Field;
    }
}