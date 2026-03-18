using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// [사진처럼 챕터별 리스트를 만들기 위한 그룹 클래스]
[System.Serializable]
public class ChapterPageGroup
{
    public string chapterName;      // 인스펙터 구분용 (예: Chapter 1)
    public List<Sprite> pageSprites; // 해당 챕터의 페이지별 배경들
}

public class PageController : MonoBehaviour
{
    [Header("서브 페이지 배경 설정")]
    public Image targetPageImage;

    [Header("챕터별 페이지 배경 리스트")]
    [Tooltip("챕터 순서대로(1장, 2장...) 그룹을 추가하세요.")]
    public List<ChapterPageGroup> allChapterPages;

    private void Start()
    {
        RefreshPageImage();
    }

    // 내부에서 스테이지와 챕터를 자동으로 체크하여 갱신
    public void RefreshPageImage()
    {
        if (targetPageImage == null || allChapterPages == null || allChapterPages.Count == 0) return;

        if (StageManager.Instance != null)
        {
            // 1. 현재 챕터와 스테이지 번호 가져오기 (1부터 시작한다고 가정)
            int currentChapter = StageManager.Instance.CurrentChapter();
            int currentStage = StageManager.Instance.CurrentStage();

            // 2. 인덱스 계산 (0부터 시작)
            int chapIdx = currentChapter - 1;
            int stageIdx = currentStage - 1;

            // 3. 해당 챕터 그룹이 존재하는지 체크
            if (chapIdx >= 0 && chapIdx < allChapterPages.Count)
            {
                List<Sprite> sprites = allChapterPages[chapIdx].pageSprites;

                // 4. 해당 챕터 내에 스테이지 배경이 존재하는지 체크
                if (stageIdx >= 0 && stageIdx < sprites.Count)
                {
                    targetPageImage.sprite = sprites[stageIdx];
                    Debug.Log($"<color=lime>[PageUI]</color> {currentChapter}챕터 {currentStage}페이지 배경 적용 완료");
                }
                else
                {
                    Debug.LogWarning($"{currentChapter}챕터에 {currentStage}번 페이지 스프라이트가 비어있습니다.");
                }
            }
            else
            {
                Debug.LogWarning($"{currentChapter}챕터에 해당하는 그룹이 리스트에 없습니다.");
            }
        }
    }
}