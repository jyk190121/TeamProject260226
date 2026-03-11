using UnityEngine;
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
    }

    public void SubStorySelect()
    {
        stagePanel.SetActive(true);
        mainStoryPanel.SetActive(false);
        subStoryPanel.SetActive(true);
        blockImg.SetActive(false);
    }

    // 메인 or 서브 스토리에서 나가기 버튼 선택 시 (서재가 Default)
    public void ExitStory()
    {
        stagePanel.SetActive(false);
        blockImg.SetActive(true);
    }
}
