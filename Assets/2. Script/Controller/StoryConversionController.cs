using System.Collections.Generic;
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


    void Start()
    {
        ExitStory();
    }

    void Update()
    {
        // stagePanel이 꺼져있을 때 (= 서재 상태일 때)만 레이캐스트 작동
        if (stagePanel != null && !stagePanel.activeSelf)
        {
            CheckWorldObjectHover();
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

            // [핵심] 만약 잡힌 UI가 'Study' 레이어가 아니라면 (예: 책, 아이템) HandOpen
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
    }

    public void SubStorySelect()
    {
        stagePanel.SetActive(true);
        mainStoryPanel.SetActive(false);
        subStoryPanel.SetActive(true);
        blockImg.SetActive(false);
        EnterStory();
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
    }
}
