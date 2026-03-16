using UnityEngine;

public class ZoomInTrigger : MonoBehaviour
{
    [Header("켤 줌 패널 (예: 서랍 확대 화면)")]
    public GameObject targetZoomPanel;

    [Header("데이터 연동")]
    [Tooltip("이 확대 화면에 진입할 때 켤 아이템의 소속 (예: Zoom_Desk)")]
    public string zoomLocationID = "Zoom_Desk";

    [Header("등장 조건 (예: 1챕터 2페이지)")]
    public int requiredChapter = 1; // Main
    public int requiredPage = 1;    // Sub

    public void Execute(ToolBarController controller)
    {
        // 1. 현재 진행도와 돋보기 기믹의 요구 진행도가 맞는지 검사
        if (StageManager.Instance != null)
        {
            if (StageManager.Instance.CurrentChapter() != requiredChapter ||
                StageManager.Instance.CurrentStage() != requiredPage)
            {
                Debug.Log($"<color=orange>[ZoomIn]</color> 아직 이 곳을 살펴볼 타이밍(페이지)이 아닙니다.");
                return; // 조건 안 맞으면 확대 패널 안 열림
            }
        }

        // 2. 조건이 맞으면 정상 작동
        if (targetZoomPanel != null)
        {
            targetZoomPanel.SetActive(true);
            targetZoomPanel.transform.SetAsLastSibling();
            controller.UpdateMagnifierCursor(true);

            if (ItemManager.Instance != null)
                ItemManager.Instance.UpdateStageVisibility(zoomLocationID);
        }
    }
}