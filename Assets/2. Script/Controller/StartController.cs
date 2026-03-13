using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StartController : MonoBehaviour
{
    public Button startBtn;
    public Button continueBtn;
    public Button settingBtn;
    public Button exitBtn;

    [Header("팝업 UI")]
    public GameObject warningPopup;             // 데이터 초기화 경고 팝업창
    public Button popupYesBtn;                  // 팝업 내 확인 버튼
    public Button popupNoBtn;                   // 팝업 내 취소 버튼

    [Header("색상 설정")]
    //Color hoverColor = Color.navyBlue;                // 변경 색상
    public Color hoverColor = new Color(0, 0, 0.5f);    // navyBlue 대용 (Color에 navyBlue는 기본 정의되어 있지 않음)
    Color normalColor = Color.yellow;                   // 기본 색상

    void Start()
    {
        // 팝업 버튼 리스너 등록 (한 번만 등록하면 됨)
        if (popupYesBtn != null) popupYesBtn.onClick.AddListener(OnClickPopupYes);
        if (popupNoBtn != null) popupNoBtn.onClick.AddListener(OnClickPopupNo);

        // 팝업 초기 비활성화
        if (warningPopup != null) warningPopup.SetActive(false);

        UpdateButtonState();
    }


    void OnEnable()
    {
        startBtn.onClick.AddListener(() => OnClickStart());
        continueBtn.onClick.AddListener(() => OnClickContinue());
        settingBtn.onClick.AddListener(() => OnClickSetting());
        exitBtn.onClick.AddListener(() => OnClickExit());

        // 호버 이벤트 등록 (모든 버튼에 대해 반복)
        AddHoverEvents(startBtn);
        AddHoverEvents(continueBtn);
        AddHoverEvents(settingBtn);
        AddHoverEvents(exitBtn);
    }

    // 버튼 활성화/비활성화 상태 업데이트
    void UpdateButtonState()
    {
        if (SaveManager.Instance == null) return;

        if (continueBtn == null) return;

        bool hasData = SaveManager.Instance.HasSaveData();

        // 이어하기 버튼: 데이터가 있을 때만 클릭 가능
        continueBtn.interactable = hasData;

        CanvasGroup cg = continueBtn.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = continueBtn.gameObject.AddComponent<CanvasGroup>();
        }

        // 이제 안전하게 alpha 값을 조절할 수 있습니다.
        cg.alpha = hasData ? 1.0f : 0.5f;

        print($"세이브데이터 존재여부 : {hasData}");
    }

    void OnDisable()
    {
        startBtn.onClick.RemoveListener(OnClickStart);
        continueBtn.onClick.RemoveListener(OnClickContinue);
        settingBtn.onClick.RemoveListener(OnClickSetting);
        exitBtn.onClick.RemoveListener(OnClickExit);
    }
    // --- 공통 로직 ---
    void AddHoverEvents(Button btn)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();

        // 중복 방지를 위해 기존 트리거 리스트를 비웁니다.
        trigger.triggers.Clear();

        // 1. PointerEnter
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((data) => { MouseOver(btn); });
        trigger.triggers.Add(enterEntry);

        // 2. PointerExit
        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) => { MouseExit(btn); });
        trigger.triggers.Add(exitEntry);

    }

    void MouseOver(Button overBtn)
    {
        // 버튼이 클릭 불가능한 상태(interactable = false)일 때는 호버 무시
        if (!overBtn.interactable) return;

        Image btnImg = overBtn.targetGraphic as Image;
        if (btnImg != null) btnImg.color = hoverColor;
        
        //Image btnImg = overBtn.GetComponent<Image>();
        //if (btnImg != null)
        //{
        //    btnImg.color = hoverColor;
        //}
    }

    void MouseExit(Button outBtn)
    {
        Image btnImg = outBtn.targetGraphic as Image;
        if (btnImg != null) btnImg.color = normalColor;

        //Image btnImg = outBtn.GetComponent<Image>();
        //if (btnImg != null)
        //{
        //    btnImg.color = normalColor;
        //}
    }

    // --- 개별 버튼 기능 함수 ---
    private void OnClickStart()
    {
        print("게임 시작 로직 실행");

        if (SaveManager.Instance.HasSaveData())
        {
            // 데이터가 있으면 팝업창 띄우기
            warningPopup.SetActive(true);
        }
        else
        {
            //GameSceneManager.Instance.LoadScene("GameScene_KJY");
            StartCoroutine( StartNewGame() );
        }
    }

    //void StartNewGame()
    //{
    //    SaveManager.Instance.DeleteSaveFile(); // 기존 데이터 삭제
    //    print("새 게임 시작");
    //    GameSceneManager.Instance.LoadScene("GameScene_KJY");
    //}

    IEnumerator StartNewGame()
    {
        CursorManager.Instance.ChangeCursor(CursorState.Loading);

        SaveManager.Instance.DeleteSaveFile(); // 기존 데이터 삭제
        print("새 게임 시작");
        
        
        yield return new WaitForSeconds(2f);
        GameSceneManager.Instance.LoadScene("GameScene_KJY");
        CursorManager.Instance.ChangeCursor(CursorState.Normal);
    }

    private void OnClickContinue()
    {
        //print("게임 이어하기 로직 실행");
        SaveData data = SaveManager.Instance.Load();
        print($"이어하기 로직 실행: 스테이지 {data.lastUnlockedStage}");

        // 로드된 데이터를 GameScene에 전달하는 로직 필요
        GameSceneManager.Instance.LoadScene("GameScene_KJY");
    }

    private void OnClickSetting()
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

    private void OnClickPopupYes()
    {
        warningPopup.SetActive(false);
        StartCoroutine(StartNewGame());
    }

    // 팝업 No: 그냥 팝업 닫기
    private void OnClickPopupNo()
    {
        warningPopup.SetActive(false);
    }
}
