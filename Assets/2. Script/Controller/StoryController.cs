using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StoryController : MonoBehaviour
{
    [Header("UI 연결")]
    public List<GameObject> chapterPrefabs;
    public GameObject storyReadPrefab;
    public Transform contentParent;
    public TextMeshProUGUI progressTxt;                                // 진행도 텍스트 (%)
    public Image progressImg;                                           // 진행도 이미지 (Bar)

    [Header("좌표 보정")]
    public Vector2 startPosition;                                       // 시작 절대 좌표
    public Vector2 finalTargetPos;                                      // 최종 정착 좌표

    [Header("모션 설정")]
    public float animationDuration = 2f;
    //public Vector2 startOffset = new Vector2(-Screen.width, -Screen.height); // 화면 왼쪽 밖 오프셋

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

    void Start()
    {
        // 게임 시작 시 저장된 데이터로부터 이미 생성된 서브 스토리들 복구
        LoadExistingSubStories();
    }
    void HandleStageCleared()
    {
        int currentChapter = StageManager.Instance.CurrentChapter();
        SaveData data = SaveManager.Instance.Load();

        // 이미 생성된 기록이 있다면 예약하지 않음
        if (data.generatedSubStories.Contains(currentChapter))
        {
            print($"<color=white>{currentChapter}장은 이미 생성된 스토리입니다.</color>");
            return;
        }

        print("<color=yellow>새로운 스테이지 클리어 감지: 서브 스토리 예약</color>");
        SetPendingSubStory(true);
    }

    public void ForceGenerateSubStory(int chapterIndex)
    {
        if (IsAlreadyGenerated(chapterIndex)) return;

        ExecuteGeneration(chapterIndex);
    }

    // 공통 실행 로직
    private void ExecuteGeneration(int chapter)
    {
        _isPendingSubStory = false;

        // 1. 데이터 저장
        SaveData data = SaveManager.Instance.Load();
        if (!data.generatedSubStories.Contains(chapter))
        {
            data.generatedSubStories.Add(chapter);
            SaveManager.Instance.Save(data);
        }

        // 2. 연출 실행
        StopAllCoroutines();
        StartCoroutine(SubStorySequence(chapter));
    }

    private bool IsAlreadyGenerated(int chapter)
    {
        SaveData data = SaveManager.Instance.Load();
        return data.generatedSubStories != null && data.generatedSubStories.Contains(chapter);
    }


    void LoadExistingSubStories()
    {
        //SaveData data = SaveManager.Instance.Load();
        //if (data.generatedSubStories == null) return;

        //foreach (int chapterIndex in data.generatedSubStories)
        //{
        //    // 애니메이션 없이 즉시 생성
        //    CreateSubStoryUI($"{chapterIndex}장의 기록", false);
        //}

        SaveData data = SaveManager.Instance.Load();
        if (data.generatedSubStories == null) return;

        //foreach (int chapterIndex in data.generatedSubStories)
        //{
        //    // 다 읽은 목록에 포함되어 있다면 읽은 프리팹 사용
        //    bool isRead = data.readSubStories.Contains(chapterIndex);

        //    // 저장된 데이터를 불러올 때는 애니메이션 없이 즉시 생성
        //    CreateSubStoryUI($"{chapterIndex}장의 기록", false, isRead);
        //}

        for (int i = 0; i < data.generatedSubStories.Count; i++)
        {
            int chapterIndex = data.generatedSubStories[i];

            // [로직 변경] 리스트의 마지막 요소가 아니라면 모두 읽음(isRead = true) 처리
            bool isRead = (i != data.generatedSubStories.Count - 1);

            CreateSubStoryUI($"{chapterIndex}장의 기록", false, isRead);
        }

        UpdateProgressUI();
    }

    // 스테이지 클리어 시 호출하여 생성 예약
    public void SetPendingSubStory(bool state) => _isPendingSubStory = state;

    // 외부에서 생성을 시작하는 함수
    public void TriggerSubStoryGeneration()
    {
        // 예약된 게 없으면 나감
        if (!_isPendingSubStory) return;

        int currentChapter = StageManager.Instance.CurrentChapter();
        if (IsAlreadyGenerated(currentChapter))
        {
            _isPendingSubStory = false;
            return;
        }

        ExecuteGeneration(currentChapter);

        //SaveData data = SaveManager.Instance.Load();

        //if (data.generatedSubStories.Contains(currentChapter))
        //{
        //    print($"{currentChapter}장은 이미 생성되어 있어 생성을 취소합니다.");
        //    _isPendingSubStory = false;
        //    return;
        //}

        //// [추가] 새 스토리를 만들기 전에, 기존에 New 상태였던 UI들을 모두 Read로 교체
        //StartCoroutine(SubStorySequence(currentChapter));

        ////// UI 생성 (애니메이션 포함)
        ////StartCoroutine(StartSequence());

        //// 데이터 기록 및 저장
        //data.generatedSubStories.Add(currentChapter);
        //SaveManager.Instance.Save(data);

        //_isPendingSubStory = false;
    }
    //void RefreshAllToReadState()
    //{
    //    // New 프리팹은 "New"라는 태그를 붙여두거나, 특정 컴포넌트로 구별하면 좋습니다.
    //    // 여기서는 간단하게 contentParent의 자식들 중 New 프리팹을 찾아 교체합니다.
    //    foreach (Transform child in contentParent)
    //    {
    //        // 만약 이름이나 태그로 구분이 가능하다면 (예: storyPrefab의 이름이 "SubStory_New")
    //        if (child.name.Contains(storyPrefab.name))
    //        {
    //            int chapterIdx = 0; // 실제 데이터와 연동하려면 정보를 들고 있어야 함
    //            // 기존 MarkAsRead 로직을 활용해 교체
    //            ReplaceToReadPrefab(child.gameObject);
    //        }
    //    }
    //}

    private IEnumerator StartSequence(int chapter)
    {
        yield return new WaitForEndOfFrame();
        int index = Mathf.Clamp(chapter - 1, 0, chapterPrefabs.Count - 1);
        GameObject targetPrefab = chapterPrefabs[index];

        yield return StartCoroutine(AddStorysubAtTopWithWorldMotion($"{chapter}장의 기록", targetPrefab));
        //yield return StartCoroutine(AddStorysubAtTopWithWorldMotion("새로운 스토리"), targetPrefab);
    }

    IEnumerator SubStorySequence(int chapter)
    {
        // 1. 새로운 스토리 올라오는 애니메이션 실행 및 끝날 때까지 대기
        // yield return을 사용해 AnimatesubFromScreenToContent가 끝날 때까지 기다립니다.
        yield return StartCoroutine(StartSequence(chapter));

        // 2. 새 스토리 배치가 완전히 끝난 후, 기존 스토리들 교체 시작
        RefreshAllToReadState();
    }
    //void CreateSubStoryUI(string message, bool useAnimation)
    //{
    //    if (storyPrefab == null || contentParent == null) return;

    //    if (useAnimation)
    //    {
    //        AddStorysubAtTopWithWorldMotion(message);
    //    }
    //    else
    //    {
    //        // 로드 시: 애니메이션 없이 즉시 배치
    //        GameObject sub = Instantiate(storyPrefab, contentParent);
    //        sub.transform.SetAsFirstSibling();
    //        Text txt = sub.GetComponentInChildren<Text>();
    //        if (txt != null) txt.text = message;


    //        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
    //    }
    //}

    void CreateSubStoryUI(string message, bool useAnimation, bool isRead = false)
    {
        if (contentParent == null) return;

        int chapter = StageManager.Instance.CurrentChapter();

        GameObject targetPrefab = null;
        GameObject sub = null;

        if (isRead)
        {
            targetPrefab = storyReadPrefab;
        }
        else
        {
            int index = Mathf.Clamp(chapter - 1, 0, chapterPrefabs.Count - 1);
            targetPrefab = chapterPrefabs[index];
        }

        if (targetPrefab == null) return;

        if (useAnimation)
        {
            //AddStorysubAtTopWithWorldMotion(message);
            StartCoroutine(AddStorysubAtTopWithWorldMotion(message, targetPrefab));
        }
        else
        {
            //애니메이션 없이 바로 생성
            sub = Instantiate(targetPrefab, contentParent);
            sub.name = targetPrefab.name;
            sub.transform.SetAsFirstSibling();

            Text txt = sub.GetComponentInChildren<Text>();
            if (txt != null) txt.text = message;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
        }
    }


    //IEnumerator AddStorysubAtTopWithWorldMotion(string message)
    //{
    //    if (storyPrefab == null || contentParent == null) yield break;

    //    // 임시 생성 (Content가 아닌 Canvas 바로 아래 생성하여 레이아웃 방해 금지)
    //    Canvas parentCanvas = contentParent.GetComponentInParent<Canvas>();
    //    GameObject tempsub = Instantiate(storyPrefab, parentCanvas.transform);

    //    // 텍스트 변경
    //    Text txt = tempsub.GetComponentInChildren<Text>();
    //    if (txt != null) txt.text = message;

    //    yield return StartCoroutine(AnimatesubFromScreenToContent(tempsub));
    //}

    IEnumerator AddStorysubAtTopWithWorldMotion(string message, GameObject targetPrefab)
    {
        if (targetPrefab == null || contentParent == null) yield break;

        Canvas parentCanvas = contentParent.GetComponentInParent<Canvas>();
        GameObject tempsub = Instantiate(targetPrefab, parentCanvas.transform);
        tempsub.name = targetPrefab.name;

        Text txt = tempsub.GetComponentInChildren<Text>();
        if (txt != null) txt.text = message;

        yield return StartCoroutine(AnimatesubFromScreenToContent(tempsub));
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

        MouseClickManager.Instance.SetClickEnable(false);

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

        MouseClickManager.Instance.SetClickEnable(true);

        UpdateProgressUI();
    }


    void RefreshAllToReadState()
    {
        List<GameObject> targets = new List<GameObject>();

        // 하이어라키를 순회하며 '읽음' 프리팹이 아닌 '일반' 프리팹들을 찾습니다.
        foreach (Transform child in contentParent)
        {
            if (child.GetSiblingIndex() == 0) continue;
            if (child.name.Contains(storyReadPrefab.name)) continue;
            // 리스트에 등록된 챕터 프리팹 중 하나인지 확인
            bool isNewTypePrefab = false;
            foreach (var prefab in chapterPrefabs)
            {
                if (child.name.Contains(prefab.name))
                {
                    isNewTypePrefab = true;
                    break;
                }
            }

            if (isNewTypePrefab) targets.Add(child.gameObject);

            //// 이름에 storyPrefab 이름이 포함되어 있고, storyReadPrefab 이름은 포함되지 않은 것
            //if (child.name.Contains(storyPrefab.name) && !child.name.Contains(storyReadPrefab.name))
            //{
            //    // [추가 조건] 방금 막 생성된 첫 번째 자식(Index 0)은 제외합니다.
            //    // 그래야 방금 올라온 책은 New 상태를 유지합니다.
            //    if (child.GetSiblingIndex() != 0)
            //    {
            //        targets.Add(child.gameObject);
            //    }
            //}
        }

        foreach (GameObject target in targets)
        {
            StartCoroutine(ReplaceWithFadeRoutine(target));
        }
    }

    IEnumerator ReplaceWithFadeRoutine(GameObject currentObj)
    {
        if (currentObj == null) yield break;

        int targetIndex = currentObj.transform.GetSiblingIndex();
        Text txtComp = currentObj.GetComponentInChildren<Text>();
        string msg = (txtComp != null) ? txtComp.text : "";

        // 1. 새 '읽음' 프리팹 생성
        GameObject newReadSub = Instantiate(storyReadPrefab, contentParent);
        newReadSub.name = storyReadPrefab.name;
        newReadSub.transform.SetSiblingIndex(targetIndex);

        Text newTxt = newReadSub.GetComponentInChildren<Text>();
        if (newTxt != null) newTxt.text = msg;

        // 2. CanvasGroup 확보 (에러 방지를 위해 먼저 가져옴)
        CanvasGroup oldGroup = currentObj.GetComponent<CanvasGroup>() ?? currentObj.AddComponent<CanvasGroup>();
        CanvasGroup newGroup = newReadSub.GetComponent<CanvasGroup>() ?? newReadSub.AddComponent<CanvasGroup>();

        yield return new WaitForEndOfFrame();

        // 3. 이제 안전하게 접근 가능
        if (newGroup != null) newGroup.alpha = 0f;
        if (oldGroup != null) oldGroup.alpha = 1f;

        float fadeDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            // 도중에 파괴되었을 경우 대비
            if (oldGroup == null || newGroup == null) break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            oldGroup.alpha = 1f - t;
            newGroup.alpha = t;

            yield return null;
        }

        if (newGroup != null) newGroup.alpha = 1f;
        if (currentObj != null) Destroy(currentObj);

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
    }

    // 진행도 업데이트
    private void UpdateProgressUI()
    {
        if (contentParent == null || progressTxt == null || progressImg == null) return;

        int validCount = 0;

        foreach (Transform child in contentParent)
        {
            // 1. 이름이 "Spacer"인 임시 객체 제외
            // 2. 이미 파괴 예약이 걸린 객체(Active 상태가 아님) 제외 (선택적)
            if (child.name != "Spacer")
            {
                validCount++;
            }
        }

        // 개당 10% 계산
        float progress = Mathf.Clamp01(validCount * 0.1f);

        progressImg.fillAmount = progress;
        progressTxt.text = $"{(int)(progress * 100)}%";

        Debug.Log($"[정확한 카운트] 실제 책 개수: {validCount} -> {progress * 100}%");
    }

    public void SyncToChapter(int targetChapter)
    {
        SaveData data = SaveManager.Instance.Load();

        // 1. 데이터 업데이트
        data.UnlockedStage = targetChapter;

        // 만약 1장 상태로 되돌리는 치트라면, 데이터 리스트에서 2장(인덱스 2) 이후 기록 삭제
        if (targetChapter == 1)
        {
            // 1장까지만 남기고 이후 기록 제거 (Ex: [1, 2] -> [1])
            data.generatedSubStories.RemoveAll(ch => ch > 1);
        }

        SaveManager.Instance.Save(data);

        // 2. 물리적 객체 정리: 2장 이상의 프리팹만 골라서 삭제
        RemoveBooksAboveChapter(targetChapter);

        // 3. UI 갱신 (남아있는 1권 등의 상태 업데이트)
        UpdateProgressUI();
    }

    private void RemoveBooksAboveChapter(int limitChapter)
    {
        List<GameObject> toDestroy = new List<GameObject>();

        foreach (Transform child in contentParent)
        {
            if (child.name == "Spacer") continue;

            // [핵심 체크] 
            // 1. 기존 프리팹(0권)은 보통 chapterPrefabs에 없거나 이름이 다를 것이므로 통과.
            // 2. 챕터 프리팹들 중 limitChapter보다 높은 숫자의 프리팹을 찾습니다.

            for (int i = 0; i < chapterPrefabs.Count; i++)
            {
                int chapterNum = i + 1; // 인덱스 0 = 1장, 인덱스 1 = 2장...

                // 만약 현재 자식이 '제한 챕터'보다 높은 장의 프리팹이라면
                if (chapterNum > limitChapter && child.name.Contains(chapterPrefabs[i].name))
                {
                    toDestroy.Add(child.gameObject);
                    break;
                }
            }

            // 추가로 '읽음(Read)' 상태가 된 2장 이상의 객체도 지워야 한다면:
            // (이름 규칙이 "2장의 기록" 처럼 숫자를 포함한다면 아래와 같이 체크 가능)
            Text txt = child.GetComponentInChildren<Text>();
            if (txt != null && txt.text.Contains("2장의 기록") && limitChapter < 2)
            {
                if (!toDestroy.Contains(child.gameObject)) toDestroy.Add(child.gameObject);
            }
        }

        // 대상 삭제
        foreach (GameObject obj in toDestroy)
        {
            obj.transform.SetParent(null); // 즉시 부모 관계 해제 (UI 레이아웃 갱신용)
            Destroy(obj);
        }

        // 레이아웃 즉시 재계산
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
    }
   
}