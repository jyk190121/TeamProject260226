using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChapterController : MonoBehaviour
{
    [Header("UI 연결")]
    public Image targetMainImage;        // 실제로 화면에 보여지는 Image 컴포넌트
    public List<Sprite> chapterSprites;  // 1장~ ?장까지의 이미지 리스트
    public Button prevBtn;
    public Button nextBtn;

    // 현재 UI에서 보여주고 있는 챕터 번호 (1부터 시작)
    int currentViewChapter;
    // 세이브 데이터 기준 해금된 최대 챕터 번호
    int maxUnlockedChapter;

    private readonly Color activeColor = new Color(207f / 255f, 255f / 255f, 162f / 255f);
    private readonly Color disabledColor = new Color(188f / 255f, 188f / 255f, 188f / 255f);

    void OnEnable()
    {
        // StageManager의 챕터 시작 이벤트를 구독
        //StageManager.OnChapterStarted += UpdateChapterUI;

        StageManager.OnChapterStarted += InitChapterUI;

        // 버튼 리스너 등록
        if (prevBtn != null) prevBtn.onClick.AddListener(() => ChangeChapter(-1));
        if (nextBtn != null) nextBtn.onClick.AddListener(() => ChangeChapter(1));
    }

    void OnDisable()
    {
        // 이벤트 구독 해제
        //StageManager.OnChapterStarted -= UpdateChapterUI;

        StageManager.OnChapterStarted -= InitChapterUI;

        if (prevBtn != null) prevBtn.onClick.RemoveAllListeners();
        if (nextBtn != null) nextBtn.onClick.RemoveAllListeners();
    }

    void Start()
    {
        if (StageManager.Instance != null)
        {
            // 이벤트 호출을 놓쳤더라도 현재 값을 안전하게 가져옴
            maxUnlockedChapter = StageManager.Instance.CurrentChapter();
            currentViewChapter = maxUnlockedChapter;
        }
        RefreshUI();
    }

    //// 챕터 번호(1~6)를 받아 UI 업데이트
    //void UpdateChapterUI(int chapterIndex)
    //{
    //    if (targetMainImage == null || chapterSprites == null) return;

    //    // 리스트는 0부터 시작하므로 chapterIndex - 1 사용
    //    int spriteIndex = chapterIndex - 1;

    //    if (spriteIndex >= 0 && spriteIndex < chapterSprites.Count)
    //    {
    //        targetMainImage.sprite = chapterSprites[spriteIndex];
    //    }
    //}

    // 씬 시작 또는 새로운 챕터 진입 시 초기화
    private void InitChapterUI(int chapterIndex)
    {
        // 현재 해금된 최대치 정보를 가져옴
        maxUnlockedChapter = StageManager.Instance.CurrentChapter();

        // 처음 보여줄 화면은 현재 진행 중인 챕터로 설정
        currentViewChapter = chapterIndex;

        RefreshUI();
    }

    // 버튼 클릭 시 호출 (delta: -1 이면 이전, 1 이면 다음)
    public void ChangeChapter(int delta)
    {
        int nextView = currentViewChapter + delta;

        // 범위 체크 (1장 미만으로 못 가고, 해금된 챕터 초과로 못 감)
        if (nextView >= 1 && nextView <= maxUnlockedChapter)
        {
            currentViewChapter = nextView;
            RefreshUI();
        }
    }

    // 인덱스에 따라 이미지와 버튼 상태를 새로고침
    private void RefreshUI()
    {
        if (targetMainImage == null || chapterSprites == null) return;

        // 1. 이미지 교체
        int spriteIndex = currentViewChapter - 1;
        if (spriteIndex >= 0 && spriteIndex < chapterSprites.Count)
        {
            targetMainImage.sprite = chapterSprites[spriteIndex];
        }

        // 2. 버튼 활성화/비활성화 로직
        // 이전 버튼: 1장일 때는 무조건 비활성

        UpdateButtonAppearance(prevBtn, currentViewChapter > 1);
        UpdateButtonAppearance(nextBtn, currentViewChapter < maxUnlockedChapter);

        //if (prevBtn != null)
        //{
        //    prevBtn.interactable = (currentViewChapter > 1);
            
        //}

        //// 다음 버튼: 현재 보는 페이지가 해금된 최대 장수보다 작을 때만 활성
        //if (nextBtn != null)
        //{
        //    nextBtn.interactable = (currentViewChapter < maxUnlockedChapter);
        //}

        print($"<color=cyan>현재 UI 표시 챕터: {currentViewChapter} / 해금 최대치: {maxUnlockedChapter}</color>");
    }

    void UpdateButtonAppearance(Button btn, bool isInteractable)
    {
        if (btn == null) return;

        // 버튼 상호작용 설정
        btn.interactable = isInteractable;

        // 버튼의 Image 컴포넌트 색상 변경
        Image btnImg = btn.GetComponent<Image>();
        if (btnImg != null)
        {
            btnImg.color = isInteractable ? activeColor : disabledColor;
        }
    }
}