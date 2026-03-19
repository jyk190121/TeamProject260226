using System.Collections;
using UnityEngine;

public class FadeOut : MonoBehaviour
{
    CanvasGroup canvasGroup;

    [Header("설정")]
    public float fadeDuration; // 사라지는 시간 (초)

    void Awake()
    {
        // CanvasGroup이 없으면 자동으로 추가합니다.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        MouseClickManager.Instance.SetClickEnable(false);
    }

    // 로딩이 시작될 때 이 함수를 호출하세요.
    public void StartFadeOut()
    {
        if(GameSceneManager.Instance.GetContinue())
        {
            Destroy(gameObject);
            MouseClickManager.Instance.SetClickEnable(true);
            return;
        }

        StartCoroutine(FadeOutAndDestroy());
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            // 시간에 따라 알파값을 1에서 0으로 보간
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        // 알파값이 0이 되면 오브젝트 파괴
        Destroy(gameObject);

        print($"{gameObject.name} 캔버스가 페이드아웃 후 파괴되었습니다.");
    }
}