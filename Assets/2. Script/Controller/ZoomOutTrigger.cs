using UnityEngine;

public class ZoomOutTrigger : MonoBehaviour
{
    [Header("끌 줌 패널 (자기 자신)")]
    public GameObject myPanel;

    [Header("데이터 연동")]
    [Tooltip("줌을 닫을 때 돌아갈 원래 화면의 소속 (예: Main 또는 Sub)")]
    public string returnLocationID = "Sub";

    // 클릭 시 실행되는 함수
    public void Execute(ToolBarController controller)
    {
        if (myPanel != null)
        {
            // 1. 확대 패널을 끕니다.
            myPanel.SetActive(false);
            if (controller != null)
            {
                controller.SetMagnifierIcon(false);
                controller.UpdateMagnifierCursor(false);
            }
            // 2. [핵심] 매니저에게 "원래 방 조명 다시 켜!" 라고 지시합니다.
            if (ItemManager.Instance != null)
                ItemManager.Instance.UpdateStageVisibility(returnLocationID);
        }
    }
}