using UnityEngine;

public class ZoomInTrigger : MonoBehaviour
{
    public GameObject targetZoomPanel;

    public void Execute(ToolBarController controller)
    {
        if (targetZoomPanel != null)
        {
            targetZoomPanel.SetActive(true);
            targetZoomPanel.transform.SetAsLastSibling();
            controller.UpdateMagnifierCursor(true); // 줌 상태 업데이트
        }
    }
}