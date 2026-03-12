using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AnswerZone : MonoBehaviour
{
    [Header("정답 조건 (숫자 ID 111 방식)")]
    public int targetID;       // 예: 111 (서브스토리 1장 1번 아이템)
    public int targetColor;    // 정답 색상 인덱스

    [Header("성공 시 실행할 이벤트")]
    public UnityEvent onCorrect;

    // ToolBarController에서 아이템을 놓았을 때 호출됨
    public bool CheckMatch(Item data, GameObject itemObj)
    {
        if (data == null) return false;

        // [수정] string ID를 숫자로 변환하여 3자리 판정
        if (int.TryParse(data.id, out int itemNumber))
        {
            if (itemNumber == targetID && data.color == targetColor)
            {
                Success(itemObj);
                return true;
            }
        }

        Debug.Log($"<color=red>[오답]</color> ID:{data.id} / Color:{data.color} 는 정답이 아닙니다.");
        return false;
    }

    private void Success(GameObject itemObj)
    {
        // 1. 위치 고정 (스냅)
        itemObj.transform.position = this.transform.position;

        // 2. 더 이상 집을 수 없게 레이캐스트 차단 (Lock)
        Graphic[] graphics = itemObj.GetComponentsInChildren<Graphic>();
        foreach (var g in graphics) g.raycastTarget = false;

        // 3. 아이템의 부모를 정답 구역으로 변경 (관리 편의성)
        itemObj.transform.SetParent(this.transform);

        Debug.Log($"<color=cyan>[정답]</color> ID {targetID} 매칭 성공! 기믹 해결.");
        onCorrect?.Invoke();
    }
}