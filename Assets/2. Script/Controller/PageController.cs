using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class ChapterPageGroup
{
    public string chapterName;      // 챕터 구분을 위한 변수
    public List<Sprite> pageSprites; // 해당 챕터의 페이지별 배경들
}

public class PageController : MonoBehaviour
{
    [Header("제어할 Sub 페이지 배경 설정")]
    public Image targetPageImage;

    [Header("챕터별 페이지 배경 리스트")]
    public List<ChapterPageGroup> allChapterPages;

    private void Start()
    {
        RefreshPageImage();
    }

    // 스테이지와 챕터를 체크하여 갱신
    public void RefreshPageImage()
    {
        if (targetPageImage == null || allChapterPages == null || allChapterPages.Count == 0) return;

        if (StageManager.Instance != null)
        {
            //현재 챕터와 스테이지(Page) 번호 가져오기 
            int currentChapter = StageManager.Instance.CurrentChapter();
            int currentStage = StageManager.Instance.CurrentStage();

            // List<Sprite>를 위한 인덱스 가공
            int chapIdx = currentChapter - 1;
            int stageIdx = currentStage - 1;

            // 챕터 체크
            if (chapIdx >= 0 && chapIdx < allChapterPages.Count)
            {
                List<Sprite> sprites = allChapterPages[chapIdx].pageSprites;

                // 챕터 내 스테이지(Page) 배경이 존재하는지 체크
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