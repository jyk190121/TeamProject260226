using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StoryConversionController : MonoBehaviour
{
    // StagePanel(부모Panel) : MainStoryPanel or SubStoryPanel ON -> ON, OFF -> OFF
    // MainStoryPanel 과 SubStroyPanel은 동시에 켜지 않음

    public GameObject stagePanel;
    public GameObject mainStoryPanel;
    public GameObject subStoryPanel;

    public GameObject blockImg;        // 서재로 돌아갔을 때 켜주고 이외왼 꺼줘야함 (도구, 아이템 클릭방지)

    public Button mainBtn;
    //public Button subBtn;

    public Button[] exitBtns = new Button[2];

    public ToolBarController toolBar;
    void Start()
    {
        ExitStory();

        toolBar = FindAnyObjectByType<ToolBarController>();
    }

    void Update()
    {
        // stagePanel이 꺼져있을 때 (= 서재 상태일 때)만 레이캐스트 작동
        if (stagePanel != null && !stagePanel.activeSelf)
        {
            CheckWorldObjectHover();
        }
        else
        {
            NormalCursor();
        }
    }

    void CheckWorldObjectHover()
    {
        if (CursorManager.Instance == null) return;

        // 1. 마우스 위치에 있는 UI 요소를 탐색하기 위한 설정
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        // 2. 마우스 아래에 있는 모든 UI를 담을 리스트
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        CursorState targetState = CursorState.Normal;

        if (results.Count > 0)
        {
            // 첫 번째로 잡힌 UI의 레이어 확인
            GameObject hoveredObj = results[0].gameObject;
            int studyLayerIndex = LayerMask.NameToLayer("Study");

            if (hoveredObj.layer != studyLayerIndex)
            {
                targetState = CursorState.HandOpen;
            }
        }

        // 마우스 커서 적용
        if (CursorManager.Instance.GetCurrentState() != targetState)
        {
            CursorManager.Instance.ChangeCursor(targetState);
        }
    }

    void OnEnable()
    {
        mainBtn.onClick.AddListener(MainStorySelect);
        SubBook.OnSubBookClicked += SubStorySelect;
        //subBtn.onClick.AddListener(SubStorySelect);

        foreach(Button btn in exitBtns)
        {
            btn.onClick.AddListener(ExitStory);
        }
    }

    void OnDisable()
    {
        mainBtn.onClick.RemoveListener(MainStorySelect);
        //subBtn.onClick.RemoveListener(SubStorySelect);
        SubBook.OnSubBookClicked -= SubStorySelect;
        foreach (Button btn in exitBtns)
        {
            btn.onClick.RemoveListener(ExitStory);
        }
    }

    public void MainStorySelect()
    {
        stagePanel.SetActive(true);
        mainStoryPanel.SetActive(true);
        subStoryPanel.SetActive(false);
        blockImg.SetActive(false);
        EnterStory();

        //현재 위치 인식(Main)
        if (ItemManager.Instance != null)
            ItemManager.Instance.UpdateStageVisibility("Main");
    }

    public void SubStorySelect()
    {
        stagePanel.SetActive(true);
        mainStoryPanel.SetActive(false);
        subStoryPanel.SetActive(true);
        blockImg.SetActive(false);
        EnterStory();

        //현재 위치 인식(Sub)
        if (ItemManager.Instance != null)
            ItemManager.Instance.UpdateStageVisibility("Sub");
    }

    void EnterStory()
    {
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.ChangeCursor(CursorState.HandOpen);
        }
    }

    // 메인 or 서브 스토리에서 나가기 버튼 선택 시 (서재가 Default)
    public void ExitStory()
    {
        stagePanel.SetActive(false);
        blockImg.SetActive(true);

        //현재 위치 인식(Library)
        if (ItemManager.Instance != null)
            ItemManager.Instance.UpdateStageVisibility("Library");
    }


    void NormalCursor()
    {
        if (CursorManager.Instance == null) return;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 기본값은 Normal
        CursorState targetState = CursorState.Normal;

        if (results.Count > 0)
        {
            GameObject hoveredObj = results[0].gameObject;
            int studyLayerIndex = LayerMask.NameToLayer("Study");

            // [수정 핵심] 'Study' 레이어가 아닌 곳(상호작용 가능 구역)에 마우스가 올라가면
            if (hoveredObj.layer != studyLayerIndex)
            {
                // ToolBarController가 있다면 현재 도구 상태를 가져오고, 없으면 기본 HandOpen
                if (toolBar != null)
                {
                    targetState = toolBar.GetCurrentToolCursorState();
                }
                else
                {
                    targetState = CursorState.HandOpen;
                }
            }
        }

        // 마우스 커서 적용 (중복 호출 방지 로직은 CursorManager 내부에 있으므로 안전)
        if (CursorManager.Instance.GetCurrentState() != targetState)
        {
            CursorManager.Instance.ChangeCursor(targetState);
        }
    }
}
