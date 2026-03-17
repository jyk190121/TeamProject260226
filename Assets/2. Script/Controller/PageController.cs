using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PageController : MonoBehaviour
{
    [Header("서브 페이지 배경 설정")]
    [Tooltip("실제로 화면에 보여지는 배경 Image 컴포넌트를 넣으세요.")]
    public Image targetPageImage;

    [Tooltip("1페이지, 2페이지, 3페이지... 순서대로 배경 이미지를 넣으세요.")]
    public List<Sprite> pageSprites;

    private void Start()
    {
        RefreshPageImage();
    }

    public void RefreshPageImage()
    {
        if (targetPageImage == null || pageSprites == null || pageSprites.Count == 0) return;

        if (StageManager.Instance != null)
        {
            int currentStage = StageManager.Instance.CurrentStage();
            int spriteIndex = currentStage - 1;

            if (spriteIndex >= 0 && spriteIndex < pageSprites.Count)
            {
                targetPageImage.sprite = pageSprites[spriteIndex];
            }
        }
    }
}