using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    public StoryController storyController;

    [SerializeField] private NarrationManager narrationManager;

    // 메인스토리를 진입한지 체크
    bool enterStory = false;

    void Start()
    {
        ExitStory();

        toolBar = FindAnyObjectByType<ToolBarController>();
        storyController = FindAnyObjectByType<StoryController>();
    }

    void Update()
    {
        // stagePanel이 꺼져있을 때 (= 서재 상태일 때)만 레이캐스트 작동
        if (stagePanel != null && !stagePanel.activeSelf)
        {
            MouseHover("study");
        }
        else
        {
            //스토리 체크용
            MouseHover("story");
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
        enterStory = true;
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

        if ((StageManager.Instance.CurrentStage() & StageManager.Instance.CurrentChapter()) == 1)
        {
            print("서브스토리 1-1 나레이션 재생");
            narrationManager.StartNarration("1", 1, "Sub");
        }
           

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
        {
            ItemManager.Instance.CloseAllZoomPanels();
            ItemManager.Instance.UpdateStageVisibility("Library");
        }

        if (storyController != null && enterStory)
        {
            storyController.TriggerSubStoryGeneration();
            enterStory = false;
        }
    }

    void MouseHover(string space )
    {
        if (CursorManager.Instance == null) return;
        
        if (MouseClickManager.Instance != null && MouseClickManager.Instance.IsClickBlocked)
        {
            if (CursorManager.Instance.GetCurrentState() != CursorState.Loading)
            {
                CursorManager.Instance.ChangeCursor(CursorState.Loading);
            }
            return;
        }

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        CursorState targetState = CursorState.Normal;

        if (results.Count > 0)
        {
            GameObject hoveredObj = results[0].gameObject;
            int studyLayerIndex = LayerMask.NameToLayer("Study");
            int itemStorgeLayerIndex = LayerMask.NameToLayer("ItemStorge");
            int itemLayerIndex = LayerMask.NameToLayer("Item");

            // 상호작용 가능한 구역(Study 레이어가 아닌 곳)에 마우스가 올라갔을 때
            if (hoveredObj.layer != studyLayerIndex)
            {
                
                //targetState = (toolBar != null) ? toolBar.GetCurrentToolCursorState() : CursorState.HandOpen;
                //targetState = CursorState.HandOpen;
                if(space.Equals("study"))
                {
                    if(hoveredObj.layer == itemStorgeLayerIndex)
                    {
                        targetState= CursorState.HandOpen;
                    }
                    else if(hoveredObj.layer == itemLayerIndex)
                    {
                        targetState = CursorState.HandHalf;
                    }
                    if (Mouse.current.leftButton.isPressed) targetState = CursorState.HandClosed;
                }

                else if (space.Equals("story"))
                {
                    // [스토리] 툴바 상태에 따른 커서, 툴바가 없으면 기본 손모양
                    targetState = (toolBar != null) ? toolBar.GetCurrentToolCursorState() : CursorState.HandOpen;
                }
            }

        }

        // 커서 상태 변경
        if (CursorManager.Instance.GetCurrentState() != targetState)
        {
            CursorManager.Instance.ChangeCursor(targetState);
        }

    }
}
