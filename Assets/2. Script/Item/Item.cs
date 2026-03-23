using UnityEngine;

public enum ItemState { Field, Storage, Used }
public enum ItemType 
{ 
  A, // 이동 or 색상 변경 가능
  B, // 색상 변경 가능
  C  // 확대 가능.
}

[CreateAssetMenu(fileName = "New Item", menuName = "Item")]
public class Item : ScriptableObject
{
    [Header("--- [불변] 초기 스펙 ---")]
    public string id;
    public string locationID = "Sub"; //등장 맵 위치(Main, Sub)

    [Header("스토리 등장 조건")]
    public int chapterIndex;
    public int stageIndex;
    public Vector2 oriPos;
    public int originColor;

    [Tooltip("숨겨진 기믹용. (Ex : 시계)")]
    public bool isHiddenInitially = false;

    [Header("타입 - A: 들기&색상 / B: 색상만 / C: 확대")]
    public ItemType type;
    public GameObject prefab;

    [Header("--- [변동] 인게임 상태 ---")]
    [System.NonSerialized] public ItemState currentState;
    [System.NonSerialized] public Vector2 changePos;
    [System.NonSerialized] public int color;

    //초기화시 발동 코드
    public void ResetToOriginal()
    {
        changePos = oriPos;
        color = originColor;
        currentState = ItemState.Field;
    }
}