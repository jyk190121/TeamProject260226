using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StoryController : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject storyPrefab;
    public Transform contentParent;

    [Header("좌표 보정")]
    public Vector2 startPosition;                                         // 시작 절대 좌표
    public Vector2 finalTargetPos;                                        // 최종 정착 좌표

    [Header("모션 설정")]
    public float animationDuration = 2f;
    //public Vector2 startOffset = new Vector2(-Screen.width, -Screen.height); // 화면 왼쪽 밖 오프셋

    LayoutGroup layoutGroup;
    int originalTopPadding;

    void Awake()
    {
        layoutGroup = contentParent.GetComponent<LayoutGroup>();
        if (layoutGroup != null) originalTopPadding = layoutGroup.padding.left;
    }


    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame == true)
        {
            StartCoroutine(StartSequence());

        }
    }

    private IEnumerator StartSequence()
    {
        yield return new WaitForEndOfFrame();
        AddStorysubAtTopWithWorldMotion("새로운 스토리");
    }

    public void AddStorysubAtTopWithWorldMotion(string message)
    {
        if (storyPrefab == null || contentParent == null) return;

        // 임시 생성 (Content가 아닌 Canvas 바로 아래 생성하여 레이아웃 방해 금지)
        Canvas parentCanvas = contentParent.GetComponentInParent<Canvas>();
        GameObject tempsub = Instantiate(storyPrefab, parentCanvas.transform);

        // 텍스트 변경
        Text txt = tempsub.GetComponentInChildren<Text>();
        if (txt != null) txt.text = message;

        StartCoroutine(AnimatesubFromScreenToContent(tempsub));
    }

    private IEnumerator AnimatesubFromScreenToContent(GameObject sub)
    {
        RectTransform rect = sub.GetComponent<RectTransform>();

        // 유니티 스타일의 Null 체크 및 컴포넌트 추가
        CanvasGroup group = sub.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = sub.AddComponent<CanvasGroup>();
        }

        // 1. 빈 게임오브젝트 생성 (Content 맨 앞에)
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(contentParent);
        spacer.transform.SetAsFirstSibling();
        RectTransform spacerRect = spacer.GetComponent<RectTransform>();

        float targetWidth = sub.GetComponent<RectTransform>().rect.width;

        // 2. 애니메이션 루프
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / animationDuration);

            // [핵심] 빈 칸의 넓이를 줄여나감 -> 기존 스토리들이 왼쪽으로 딸려옴
            spacerRect.sizeDelta = new Vector2(Mathf.Lerp(targetWidth, 0, t), 0);

            // 새 스토리 이동
            rect.anchoredPosition = Vector2.Lerp(startPosition, finalTargetPos, t);
            group.alpha = t;

            // 실시간 레이아웃 갱신
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);

            yield return null;
        }

        Destroy(spacer);
        sub.transform.SetParent(contentParent);
        sub.transform.SetAsFirstSibling();

        // 최종 위치 및 알파값 강제 고정
        rect.anchoredPosition = Vector2.zero;
        group.alpha = 1f;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);


        //// --- [최종 정착] ---
        //layoutGroup.padding.left = originalTopPadding;
        //sub.transform.SetParent(contentParent);
        //sub.transform.SetAsFirstSibling(); // 리스트의 가장 처음에 배치

        //rect.anchoredPosition = Vector2.zero;
        //group.alpha = 1f;

        //LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
    }
}