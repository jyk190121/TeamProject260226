using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum ClearType { Page, Chapter }

public class AnswerZone : MonoBehaviour
{
    [Header("정답 조건")]
    public int targetID;
    public int targetColor;

    // [추가됨] 메인 씬 전용인지 체크
    [Header("위치 및 등장 조건")]
    [Tooltip("체크 시 메인(Main) 화면용 존이 되며, Sub 페이지 번호를 무시합니다.")]
    public bool isMainZone = false;

    public int requiredChapter = 1; // Main
    public int requiredPage = 1;    // Sub (isMainZone이 true면 무시됨)

    [Header("클리어 설정")]
    public ClearType clearType = ClearType.Page;

    [Header("성공 시 실행할 이벤트")]
    public UnityEvent onCorrect;

    public bool CheckMatch(Item data, GameObject itemObj)
    {
        if (data == null) return false;

        // 1. [변경됨] 메인 존 체크박스에 따른 유연한 조건 검사
        if (StageManager.Instance != null)
        {
            // 챕터는 무조건 일치해야 함
            if (StageManager.Instance.CurrentChapter() != requiredChapter)
            {
                Debug.Log($"<color=orange>[AnswerZone]</color> 다른 챕터의 정답 구역입니다.");
                return false;
            }

            // 메인 존이 아닐 경우에만 페이지 번호까지 깐깐하게 검사
            if (!isMainZone && StageManager.Instance.CurrentStage() != requiredPage)
            {
                Debug.Log($"<color=orange>[AnswerZone]</color> 아직 이 기믹을 풀 타이밍(페이지)이 아닙니다.");
                return false;
            }
        }

        // 2. 정답 판정
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
        itemObj.transform.position = this.transform.position;
        Graphic[] graphics = itemObj.GetComponentsInChildren<Graphic>();
        foreach (var g in graphics) g.raycastTarget = false;
        itemObj.transform.SetParent(this.transform);

        Debug.Log($"<color=cyan>[정답]</color> ID {targetID} 매칭 성공! 기믹 해결.");
        onCorrect?.Invoke();

        if (clearType == ClearType.Page)
        {
            Debug.Log("<color=yellow>[스테이지 진행]</color> 페이지(Sub) 클리어! 다음 페이지로 넘어갑니다.");
            StageManager.Instance.ClearStage();

            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.SpawnItem(StageManager.Instance.CurrentChapter(), StageManager.Instance.CurrentStage());
            }
        }
        else if (clearType == ClearType.Chapter)
        {
            Debug.Log("<color=yellow>[스테이지 진행]</color> 챕터(Main) 클리어! 다음 챕터로 넘어갑니다.");
            StageManager.Instance.ClearChapter();
        }
    }
}