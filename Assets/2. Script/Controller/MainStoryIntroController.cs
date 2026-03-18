using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainStoryIntroController : MonoBehaviour
{
    [Header("배경 이미지")]
    [SerializeField] private Image baseBackgroundImage;
    // 처음부터 보이는 배경 A

    [SerializeField] private Image fadeBackgroundImage;
    // 3초 뒤 서서히 나타나는 배경 B
    // 최종적으로 이 배경만 남아서 게임 화면이 된다

    [Header("연출 시간")]
    [SerializeField] private float delayBeforeFade = 3f;
    // Fade 시작 전 대기 시간

    [SerializeField] private float fadeDuration = 2f;
    // 배경 B가 완전히 나타날 때까지 걸리는 시간

    [Header("자동 재생")]
    [SerializeField] private bool playOnStart = true;
    // 씬 시작 시 자동으로 연출을 재생할지 여부

    [Header("완료 후 배경 A 비활성화")]
    [SerializeField] private bool disableBaseAfterComplete = true;
    // Fade가 끝난 뒤 배경 A를 끌지 여부

    private Coroutine playRoutine;
    private bool isPlaying = false;
    private bool isCompleted = false;

    public bool IsPlaying => isPlaying;
    public bool IsCompleted => isCompleted;

    private void Start()
    {
        InitializeState();

        if (playOnStart)
        {
            PlayIntro();
        }
    }

    /// <summary>
    /// 연출 시작 전 상태를 초기화한다.
    /// 
    /// 시작 상태:
    /// - 배경 A는 보임
    /// - 배경 B는 활성화되어 있지만 완전히 투명
    /// </summary>
    private void InitializeState()
    {
        if (baseBackgroundImage != null)
        {
            baseBackgroundImage.gameObject.SetActive(true);
            SetImageAlpha(baseBackgroundImage, 1f);
        }

        if (fadeBackgroundImage != null)
        {
            fadeBackgroundImage.gameObject.SetActive(true);
            SetImageAlpha(fadeBackgroundImage, 0f);
        }

        isPlaying = false;
        isCompleted = false;
    }

    /// <summary>
    /// 인트로 연출을 시작한다.
    /// </summary>
    public void PlayIntro()
    {
        if (isPlaying)
            return;

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        InitializeState();
        playRoutine = StartCoroutine(PlayIntroRoutine());
    }

    /// <summary>
    /// 실제 연출 흐름을 처리하는 코루틴.
    /// 
    /// 1. 지정 시간 대기
    /// 2. 배경 B를 서서히 FadeIn
    /// 3. 완료 후 배경 A를 끄고 배경 B만 남긴다
    /// </summary>
    private IEnumerator PlayIntroRoutine()
    {
        isPlaying = true;

        SoundManager.Instance.PlaySfx(SfxId.Beach);

        if (delayBeforeFade > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFade);
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = (fadeDuration > 0f) ? Mathf.Clamp01(elapsed / fadeDuration) : 1f;
            SetImageAlpha(fadeBackgroundImage, t);

            yield return null;
        }

        SetImageAlpha(fadeBackgroundImage, 1f);

        FinishIntro();
        playRoutine = null;
    }

    /// <summary>
    /// 연출 완료 처리.
    /// 
    /// 배경 B는 그대로 남기고,
    /// 필요 시 배경 A를 비활성화한다.
    /// </summary>
    private void FinishIntro()
    {
        if (disableBaseAfterComplete && baseBackgroundImage != null)
        {
            baseBackgroundImage.gameObject.SetActive(false);
        }

        isPlaying = false;
        isCompleted = true;

        Debug.Log($"{nameof(MainStoryIntroController)}: 메인 스테이지 배경 연출 완료", this);
    }

    /// <summary>
    /// Image의 알파값만 변경한다.
    /// </summary>
    private void SetImageAlpha(Image target, float alpha)
    {
        if (target == null)
            return;

        Color color = target.color;
        color.a = alpha;
        target.color = color;
    }
}