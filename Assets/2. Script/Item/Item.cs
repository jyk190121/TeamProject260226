using UnityEngine;
public enum ItemType { A, B }

[CreateAssetMenu(fileName = "New Item", menuName = "Item")]
public class Item : ScriptableObject
{
    // 외부에서 수정 불가능하게 하되, 시리얼라이즈는 되도록 설정
    [SerializeField]
    public string id;          // Apple_01_A 구분자
    public int stageIndex;     // 스테이지 번호
    public Vector2 oriPos;     // 초기 위치
    public Vector2 changePos;  // 변경 위치
    public int color;          // 색상 인덱스

    [Header("타입 - A : 들기가능/색상가져오기가능 , B : 들기만가능")]
    public ItemType type;

    [Header("아이템 프리팹")]
    public GameObject prefab;

    [Header("대지인가")]
    public bool isGround;
}
