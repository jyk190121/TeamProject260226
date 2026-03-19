using UnityEngine;

public class ZoomInTrigger : MonoBehaviour
{
    [Header("켤 줌 패널 (예: 서랍 확대 화면)")]
    public GameObject targetZoomPanel;

    [Header("데이터 연동")]
    public string zoomLocationID = "Zoom_Desk";

    [Header("등장 조건")]
    public int requiredChapter = 1;
    public int requiredPage = 1;

    public void Execute(ToolBarController controller)
    {
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
            targetZoomPanel.SetActive(true);
            targetZoomPanel.transform.SetAsLastSibling();
            controller.UpdateMagnifierCursor(true);

            controller.SetMagnifierIcon(true);

            if (ItemManager.Instance != null)
                ItemManager.Instance.UpdateStageVisibility(zoomLocationID);
        }
    }
}