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
    public GameObject warningPopup;                     // 데이터 초기화 경고 팝업창
    public Button popupYesBtn;                          // 팝업 내 확인 버튼
    public Button popupNoBtn;                           // 팝업 내 취소 버튼

    public GameObject settingPopup;                     // 설정 팝업창
    public Button settingExitBtn;                       // 설정 팝업창 닫기 버튼

    [Header("색상 설정")]
    //Color hoverColor = Color.navyBlue;                // 변경 색상
    public Color hoverColor = new Color(0, 0, 0.5f);    // navyBlue 대용 (Color에 navyBlue는 기본 정의되어 있지 않음)
    Color normalColor = Color.yellow;                   // 기본 색상

    void Start()
    {
        // 팝업 버튼 리스너 등록 (한 번만 등록하면 됨)
        if (popupYesBtn != null) popupYesBtn.onClick.AddListener(OnClickPopupYes);
        if (popupNoBtn != null) popupNoBtn.onClick.AddListener(OnClickPopupNo);
        if (settingExitBtn != null) settingExitBtn.onClick.AddListener(OnClickSettingExit);

        // 팝업 초기 비활성화
        if (warningPopup != null) warningPopup.SetActive(false);
        if (settingPopup != null) settingPopup.SetActive(false);

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

        bool canContinue = SaveManager.Instance.CanContinue();

        // 이어하기 버튼: 데이터가 있고, 사운드 설정만이 아닐경우
        continueBtn.interactable = canContinue;

        CanvasGroup cg = continueBtn.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = continueBtn.gameObject.AddComponent<CanvasGroup>();
        }

        // 이제 안전하게 alpha 값을 조절할 수 있습니다.
        cg.alpha = canContinue ? 1.0f : 0.5f;

        print($"사운드값을 제외한 세이브데이터 존재여부 : {canContinue}");
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

        if (SaveManager.Instance.CanContinue())
        {
            warningPopup.SetActive(true);
        }
        else
        {
            StartGame();
        }
    }
    private IEnumerator GameLoadSequence(System.Action dataProcessAction)
    {
        MouseClickManager.Instance.SetClickEnable(false);
        CursorManager.Instance.ChangeCursor(CursorState.Loading);

        dataProcessAction?.Invoke();

        yield return new WaitForSeconds(2f);

        GameSceneManager.Instance.LoadScene("GameScene_KJY");
        CursorManager.Instance.ChangeCursor(CursorState.Normal);
        MouseClickManager.Instance.SetClickEnable(true);
    }


    void StartGame()
    {
        StartCoroutine(GameLoadSequence(() => {
            SaveData data = SaveManager.Instance.Load();
            data.isGameStarted = true;
            SaveManager.Instance.Save(data);
            print($"최초 게임 시작 {data.isGameStarted }");
        }));
    }

    void StartNewGame()
    {
        StartCoroutine(GameLoadSequence(() => {
            SaveManager.Instance.DeleteSaveFile();

            // 게임 시작 초기화
            SaveData newData = new SaveData();
            newData.isGameStarted = false;
            SaveManager.Instance.Save(newData);
            print($"새 게임 시작 (기존데이터 삭제) {newData.isGameStarted}");
        }));
    }

    private void OnClickContinue()
    {
        //print("게임 이어하기 로직 실행");
        SaveData data = SaveManager.Instance.Load();
        print($"이어하기 로직 실행: 메인 {data.lastUnlockedChapter}장");

        // 로드된 데이터를 GameScene에 전달하는 로직 필요
        GameSceneManager.Instance.LoadScene("GameScene_KJY");
    }

    private void OnClickSetting()
    {
        settingPopup.gameObject.SetActive(true);
    }
    private void OnClickSettingExit()
    {
        settingPopup.gameObject.SetActive(false);
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
        StartNewGame();
    }

    // 팝업 No: 그냥 팝업 닫기
    private void OnClickPopupNo()
    {
        warningPopup.SetActive(false);
    }
}
