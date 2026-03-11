using UnityEngine;

public enum ItemType { A, B }

[CreateAssetMenu(fileName = "New Item", menuName = "Item")]
public class Item : ScriptableObject
{
    [Header("--- [불변] 초기 스펙 ---")]
    public string id;          // Apple_01_A 구분자
    public int stageIndex;     // 스테이지 번호
    public Vector2 oriPos;     // 초기 위치
    public int originColor;    // 초기 색상

    [Header("타입 - A : 들기&색상 / B : 색상만")]
    public ItemType type;

    [Header("아이템 프리팹")]
    public GameObject prefab;

    [Header("--- [변동] 인게임 상태 ---")]
    public Vector2 changePos;  // 변경 위치
    public int color;          // 현재 색상 인덱스 (변경 가능)

    [Header("대지인가?")]
    public bool isGround;

    // ---------------------------------------------------
    // '처음부터'시 데이터를 리셋하는 함수
    // ---------------------------------------------------
    public void ResetToOriginal()
    {
        // 새 위치를 고향 위치로
        changePos = oriPos;
        color = originColor;

        Debug.Log($"<color=white><b>[{id}]</b> 위치와 색상이 초기 상태로 리셋되었습니다.</color>");
    }
}