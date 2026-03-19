using UnityEngine;
using UnityEngine.UI;
using Key = UnityEngine.InputSystem.Key;

public class GameController : MonoBehaviour
{
    public GameObject settingPopup;                     // 설정 팝업창
    public GameObject exitPopup;                        // 종료 팝업창
    public Button settingEnterBtn;                      // 설정 팝업창 열기 버튼
    public Button settingExitBtn;                       // 설정 팝업창 닫기 버튼
    public Button OpenExitBtn;                          // 종료 팝업창 닫기 버튼
    public Button exitYesBtn;                          // 팝업 내 확인 버튼
    public Button exitNoBtn;                           // 팝업 내 취소 버튼
    public FadeOut loadingCanvas;

    NarrationManager narrationManager;

    void Start()
    {
        narrationManager = FindAnyObjectByType<NarrationManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(Key.F1))
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.RestartAtStage(1, 1);
                print("스테이지 1-1 데이터 초기화 및 서재에서 재시작");
            }
        }
        if (Input.GetKeyDown(Key.F2))
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.RestartAtStage(1, 2);
                print("스테이지 1-2 데이터 초기화 및 서재에서 재시작");
            }
        }
    }
    void FixedUpdate()
    {
        if (loadingCanvas != null && narrationManager.isLoaded)
        {
            loadingCanvas.StartFadeOut();
        }
    }


    void OnEnable()
    {
        SettingClose();
        ExitClose();

        settingEnterBtn.onClick.AddListener(SettingOpen);
        settingExitBtn.onClick.AddListener(SettingClose);
        OpenExitBtn.onClick.AddListener(ExitOpen);
        exitYesBtn.onClick.AddListener(ExitGame);
        exitNoBtn.onClick.AddListener(ExitClose);
    }

    void OnDisable()
    {
        settingEnterBtn.onClick.RemoveListener(SettingOpen);
        settingExitBtn.onClick.RemoveListener(SettingClose);
        OpenExitBtn.onClick.RemoveListener(ExitOpen);
        exitYesBtn.onClick.RemoveListener(ExitGame);
        exitNoBtn.onClick.RemoveListener(ExitClose);
    }

    void SettingOpen()
    {
        settingPopup.gameObject.SetActive(true);
    }
    void SettingClose()
    {
        settingPopup.gameObject.SetActive(false);
    }

    void ExitOpen()
    {
        exitPopup.gameObject.SetActive(true);
    }
    void ExitClose()
    {
        exitPopup.gameObject.SetActive(false);
    }
    void ExitGame()
    {
        print("게임 종료 로직 실행");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
    }
}
