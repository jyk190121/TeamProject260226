using UnityEngine;

// [추가] 아이템의 3가지 생애 주기 상태
public enum ItemState { Field, Storage, Used }
public enum ItemType { A, B }

[CreateAssetMenu(fileName = "New Item", menuName = "Item")]
public class Item : ScriptableObject
{
    [Header("--- [불변] 초기 스펙 ---")]
    public string id;              // 고유 번호 (예: Apple_01)

    // [핵심] 이 아이템이 필드에 있을 때, 어느 화면에서 보일 것인가?
    [Tooltip("예: Main, Sub, Zoom_Desk 등")]
    public string locationID = "Sub";

    public int stageIndex;         // 스테이지(장) 번호
    public Vector2 oriPos;         // 초기 픽셀 위치
    public int originColor;        // 초기 색상 인덱스

    [Header("타입 - A: 들기&색상 / B: 색상만")]
    public ItemType type;

    [Header("아이템 프리팹")]
    public GameObject prefab;

    [Header("--- [변동] 인게임 상태 ---")]
    [System.NonSerialized]
    public ItemState currentState; // 현재 취급 상태 (Field, Storage, Used)
    public Vector2 changePos;      // 현재 픽셀 위치
    public int color;              // 현재 색상 인덱스

    // ---------------------------------------------------
    // [기능] 처음부터 시작 시 데이터를 리셋하는 함수
    // ---------------------------------------------------
    public void ResetToOriginal()
    {
        changePos = oriPos;
        color = originColor;

        // 리셋 시 무조건 '바닥(Field)' 상태로 되돌림
        currentState = ItemState.Field;

        Debug.Log($"<color=white><b>[{id}]</b> 리셋 완료 (State: {currentState}, Location: {locationID})</color>");
    }
}