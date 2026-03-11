using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class StartController : MonoBehaviour
{
    public Button startBtn;
    public Button continueBtn;
    public Button optionBtn;
    public Button exitBtn;

    [Header("색상 설정")]
    Color hoverColor = Color.navyBlue;          // 변경 색상
    Color normalColor = Color.yellow;           // 기본 색상

    void OnEnable()
    {
        startBtn.onClick.AddListener(() => OnClickStart());
        continueBtn.onClick.AddListener(() => OnClickContinue());
        optionBtn.onClick.AddListener(() => OnClickOption());
        exitBtn.onClick.AddListener(() => OnClickExit());

        // 호버 이벤트 등록 (모든 버튼에 대해 반복)
        AddHoverEvents(startBtn);
        AddHoverEvents(continueBtn);
        AddHoverEvents(optionBtn);
        AddHoverEvents(exitBtn);
    }

    void OnDisable()
    {
        startBtn.onClick.RemoveListener(OnClickStart);
        continueBtn.onClick.RemoveListener(OnClickContinue);
        optionBtn.onClick.RemoveListener(OnClickOption);
        exitBtn.onClick.RemoveListener(OnClickExit);
    }
    // --- 공통 로직 ---
    void AddHoverEvents(Button btn)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        // 1. 마우스 진입 (PointerEnter)
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { MouseOver(btn); });
        trigger.triggers.Add(enterEntry);

        // 2. 마우스 퇴장 (PointerExit)
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { MouseExit(btn); });
        trigger.triggers.Add(exitEntry);
    }

    void MouseOver(Button overBtn)
    {
        Image btnImg = overBtn.GetComponent<Image>();
        if (btnImg != null)
        {
            btnImg.color = hoverColor;
        }
    }

    void MouseExit(Button outBtn)
    {
        Image btnImg = outBtn.GetComponent<Image>();
        if (btnImg != null)
        {
            btnImg.color = normalColor;
        }
    }

    // --- 개별 버튼 기능 함수 ---
    private void OnClickStart()
    {
        print("게임 시작 로직 실행");
        GameSceneManager.Instance.LoadScene("GameScene_KJY");
    }

    private void OnClickContinue()
    {
        print("게임 이어하기 로직 실행");
    }

    private void OnClickOption()
    {
        print("옵션 창 열기 로직 실행");
    }

    private void OnClickExit()
    {
        print("게임 종료 로직 실행");
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                    Application.Quit();
        #endif
    }
}
