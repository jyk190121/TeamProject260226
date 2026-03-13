using UnityEngine;

public class ZoomInTrigger : MonoBehaviour
{
    [Header("켤 줌 패널 (예: 서랍 확대 화면)")]
    public GameObject targetZoomPanel;

    [Header("데이터 연동")]
    [Tooltip("이 확대 화면에 진입할 때 켤 아이템의 소속 (예: Zoom_Desk)")]
    public string zoomLocationID = "Zoom_Desk";

    // 클릭 시 실행되는 함수 (이전 코드에 맞춤)
    public void Execute(ToolBarController controller)
    {
        if (targetZoomPanel != null)
        {
            // 1. 확대 패널을 화면에 켭니다.
            targetZoomPanel.SetActive(true);
            targetZoomPanel.transform.SetAsLastSibling(); // 맨 앞으로 가져오기
            controller.UpdateMagnifierCursor(true); // 마우스 커서 변경

            // 2. [핵심] 매니저에게 "서랍 안쪽 조명 켜!" 라고 지시합니다.
            if (ItemManager.Instance != null)
                ItemManager.Instance.UpdateStageVisibility(zoomLocationID);
        }
    }
}