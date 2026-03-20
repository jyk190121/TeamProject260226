using UnityEngine;

public class ZoomOutTrigger : MonoBehaviour
{
    [Header("확대 해제 패널 설정(자기 자신)")]
    public GameObject myPanel;

    [Header("복귀 위치")]
    public string returnLocationID = "Sub";

    // 판넬 클릭 시 실행되는 함수
    public void Execute(ToolBarController controller)
    {
        if (myPanel != null)
        {
            // 확대 패널을 종료
            myPanel.SetActive(false);

            // 툴바 아이콘 및 커서 상태 원래대로 복구
            if (controller != null)
            {
                controller.SetZoomState(false);
            }
            // 원래 방 위치(Main/Sub)에 맞게 아이템 가시성 복구
            if (ItemManager.Instance != null)
                ItemManager.Instance.UpdateStageVisibility(returnLocationID);
        }
    }
}