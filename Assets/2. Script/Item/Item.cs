using UnityEngine;

public enum ItemState { Field, Storage, Used }
public enum ItemType { A, B }

[CreateAssetMenu(fileName = "New Item", menuName = "Item")]
public class Item : ScriptableObject
{
    [Header("--- [불변] 초기 스펙 ---")]
    public string id;
    public string locationID = "Sub";

    [Header("스토리 등장 조건")]
    public int chapterIndex;       // [추가] Main 스토리 번호
    public int stageIndex;         // [유지] Sub 스토리 번호

    public Vector2 oriPos;
    public int originColor;

    [Header("타입 - A: 들기&색상 / B: 색상만")]
    public ItemType type;
    public GameObject prefab;

    [Header("--- [변동] 인게임 상태 ---")]
    [System.NonSerialized]
    public ItemState currentState;
    public Vector2 changePos;
    public int color;

    public void ResetToOriginal()
    {
        changePos = oriPos;
        color = originColor;
        currentState = ItemState.Field;
    }
}