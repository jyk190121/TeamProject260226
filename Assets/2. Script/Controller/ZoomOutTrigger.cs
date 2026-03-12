using UnityEngine;

public class ZoomOutTrigger : MonoBehaviour
{
    [Header("닫을 판넬 (자기 자신 연결)")]
    public GameObject myPanel;

    // controller를 인자로 받아 줌 상태를 업데이트합니다.
    public void Execute(ToolBarController controller)
    {
        if (myPanel != null)
        {
            myPanel.SetActive(false);
            controller.UpdateMagnifierCursor(false); // 줌 상태를 false로 변경
            Debug.Log("<color=orange>[ZoomOut]</color> 확대 화면 꺼짐");
        }
    }
}