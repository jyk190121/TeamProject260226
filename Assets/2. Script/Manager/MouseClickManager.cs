using UnityEngine;
using UnityEngine.UI;

public class MouseClickManager : MonoBehaviour
{
    public static MouseClickManager Instance;

    CanvasGroup _canvasGroup;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Init()
    {
        // 런타임에 클릭 차단용 Canvas 생성
        GameObject blocker = new GameObject("MouseBlocker");
        blocker.transform.SetParent(this.transform);

        // Canvas 설정
        Canvas canvas = blocker.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // 최상단 배치

        blocker.AddComponent<GraphicRaycaster>();

        // 투명 이미지 추가 (Raycast 타겟용)
        Image img = blocker.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0); // 투명

        // CanvasGroup으로 제어
        _canvasGroup = blocker.AddComponent<CanvasGroup>();

        // 초기 상태: 클릭 허용
        SetClickEnable(true);
    }

    /// <summary>
    /// 클릭 가능 여부를 설정
    /// </summary>
    /// <param name="enable">true면 클릭 가능, false면 모든 클릭 차단</param>
    public void SetClickEnable(bool enable)
    {
        if (_canvasGroup == null) return;

        _canvasGroup.blocksRaycasts = !enable;
    }

    // 현재 클릭이 가능하지 않는 상태냐
    public bool IsClickBlocked => _canvasGroup != null && _canvasGroup.blocksRaycasts;
}