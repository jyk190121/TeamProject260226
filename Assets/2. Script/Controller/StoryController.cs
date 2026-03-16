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
    //int originalTopPadding;

    // 서브 스토리가 생성 대기 중인지 확인하는 플래그 (true : 생성)
    [SerializeField] bool _isPendingSubStory = false;

    void OnEnable()
    {
        // 이제 StageManager를 직접 참조하지 않고 이벤트만 수신합니다.
        StageManager.OnChapterCleared += HandleStageCleared;
    }

    void OnDisable()
    {
        StageManager.OnChapterCleared -= HandleStageCleared;
    }

    private void HandleStageCleared()
    {
        Debug.Log("<color=yellow>스테이지 클리어 감지: 서브 스토리 예약</color>");
        SetPendingSubStory(true);
    }

    void Awake()
    {
        layoutGroup = contentParent.GetComponent<LayoutGroup>();
        //if (layoutGroup != null) originalTopPadding = layoutGroup.padding.left;
    }

    // 스테이지 클리어 시 호출하여 생성 예약
    public void SetPendingSubStory(bool state) => _isPendingSubStory = state;

    // 실제로 생성을 시작하는 함수
    public void TriggerSubStoryGeneration()
    {
        // 예약된 게 없으면 나감
        if (!_isPendingSubStory) return;

        //StartCoroutine(StartSequence());
        //_isPendingSubStory = false;

        // 현재 스테이지 번호를 가져와서 이름에 활용할 수 있습니다.
        int currentStage = SaveManager.Instance.Load().lastUnlockedChapter;
        //string storyName = $"{currentStage}장의 기록";

        StartCoroutine(StartSequence());
        _isPendingSubStory = false;
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
        //RectTransform rect = sub.GetComponent<RectTransform>();

        //// 유니티 스타일의 Null 체크 및 컴포넌트 추가
        //CanvasGroup group = sub.GetComponent<CanvasGroup>();
        //if (group == null)
        //{
        //    group = sub.AddComponent<CanvasGroup>();
        //}

        //// 1. 빈 게임오브젝트 생성 (Content 맨 앞에)
        //GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
        //spacer.transform.SetParent(contentParent);
        //spacer.transform.SetAsFirstSibling();
        //RectTransform spacerRect = spacer.GetComponent<RectTransform>();

        //float targetWidth = sub.GetComponent<RectTransform>().rect.width;

        //// 2. 애니메이션 루프
        //float elapsedTime = 0f;
        //while (elapsedTime < animationDuration)
        //{
        //    elapsedTime += Time.deltaTime;
        //    float t = Mathf.SmoothStep(0, 1, elapsedTime / animationDuration);

        //    // [핵심] 빈 칸의 넓이를 줄여나감 -> 기존 스토리들이 왼쪽으로 딸려옴
        //    spacerRect.sizeDelta = new Vector2(Mathf.Lerp(targetWidth, 0, t), 0);

        //    // 새 스토리 이동
        //    rect.anchoredPosition = Vector2.Lerp(startPosition, finalTargetPos, t);
        //    group.alpha = t;

        //    // 실시간 레이아웃 갱신
        //    LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);

        //    yield return null;
        //}

        //Destroy(spacer);
        //sub.transform.SetParent(contentParent);
        //sub.transform.SetAsFirstSibling();

        //// 최종 위치 및 알파값 강제 고정
        //rect.anchoredPosition = Vector2.zero;
        //group.alpha = 1f;

        //LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);


        ///수정
        RectTransform rect = sub.GetComponent<RectTransform>();

        // CanvasGroup 체크 및 추가
        CanvasGroup group = sub.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = sub.AddComponent<CanvasGroup>();
            // 중요: AddComponent 직후에 즉시 접근하면 에러가 날 수 있으므로 한 프레임 대기
            yield return null;
        }

        // 코루틴 내부 수정 버전
        float targetWidth = 300f; // 정확히 300으로 고정

        // Spacer 설정 부분
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(contentParent);
        spacer.transform.SetAsFirstSibling();

        // 레이아웃 엔진에 크기를 전달할 컴포넌트 추가
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.preferredWidth = 0; // 시작은 0
        le.flexibleWidth = 0;  // 300 이상으로 늘어나지 않게 방지

        RectTransform spacerRect = spacer.GetComponent<RectTransform>();

        // LayoutElement 추가 (LayoutGroup이 Spacer의 크기를 무시하지 않도록)
        LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();

        // 초기 상태 설정
        group.alpha = 0;
        rect.anchoredPosition = startPosition;

        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / animationDuration);

            float currentWidth = Mathf.Lerp(0, targetWidth, t);

            spacerRect.sizeDelta = new Vector2(currentWidth, 0);
            le.preferredWidth = currentWidth;

            rect.anchoredPosition = Vector2.Lerp(startPosition, finalTargetPos, t);
            group.alpha = t;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
            yield return null;
        }

        // 최종 정착 부분 수정
        Destroy(spacer);

        // 부모를 옮기되, 현재 월드 위치를 유지하도록 true 설정
        sub.transform.SetParent(contentParent, true);
        sub.transform.SetAsFirstSibling();

        // 레이아웃이 즉시 계산되도록 호출
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);

        group.alpha = 1f;
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame == true)
        {
            StartCoroutine(StartSequence());
        }
    }
}