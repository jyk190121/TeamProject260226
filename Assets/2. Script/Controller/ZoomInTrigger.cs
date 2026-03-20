using UnityEngine;

public class ZoomInTrigger : MonoBehaviour
{
    [Header("확대 패널 설정")]
    public GameObject targetZoomPanel;

    [Header("줌 연결 위치")]
    public string zoomLocationID = "Zoom_Desk";

    [Header("등장 조건 (챕터/스테이지(Page)확인)")]
    public int requiredChapter = 1;
    public int requiredPage = 1;


    public void Execute(ToolBarController controller)
    {
        // 인스펙터에 설정된 챕터, 스테이지인가?
        if (StageManager.Instance != null)
        {
            if (StageManager.Instance.CurrentChapter() != requiredChapter ||
                StageManager.Instance.CurrentStage() != requiredPage)
            {
                return;
            }
        }

        if (targetZoomPanel != null)
        {
            // 확대 패널을 활성화하고 최상단으로 배치
            targetZoomPanel.SetActive(true);
            targetZoomPanel.transform.SetAsLastSibling();

            // 툴바 아이콘 및 커서 상태를 돋보기(확대)로 변경
            if (controller != null)
            {
                controller.SetZoomState(true);
            }

            // 줌 위치(예: Zoom_Desk)에 맞게 아이템 가시성 갱신
            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.UpdateStageVisibility(zoomLocationID);
            }
        }
    }
}