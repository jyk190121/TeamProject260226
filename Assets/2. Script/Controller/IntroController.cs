using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.UI;

public class IntroController : MonoBehaviour
{
    private enum IntroPhase
    {
        None,
        Ready,
        FirstTimelinePlaying,
        FirstDialoguePlaying,
        WaitingForPlayerAction,
        DraggingItem,
        SuccessTransition,
        SecondTimelinePlaying,
        SecondDialoguePlaying,
        Completed
    }

    [System.Serializable]
    public struct NPCStateSprite
    {
        public string stateName;
        public Sprite sprite;
    }

    [System.Serializable]
    public class DialogueEventBinding
    {
        public string eventKey;
        public UnityEvent onEvent;
    }

    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;

    [Header("연출용 메리")]
    [SerializeField] private GameObject cutsceneMaryObject;
    [SerializeField] private GameObject cutsceneMaryContainer;
    [SerializeField] private Image cutsceneMaryImage;

    [Header("진짜 메리")]
    [SerializeField] private GameObject realMaryObject;

    [Header("아이템용 메리 데이터")]
    [SerializeField] private Item introItemData;

    [Header("아이템용 메리 Sprite")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite heldSprite;

    [Header("Timeline")]
    [SerializeField] private PlayableDirector firstTimelineDirector;
    [SerializeField] private PlayableDirector secondTimelineDirector;

    [Header("연결 대상")]
    [SerializeField] private ItemManager itemManager;
    [SerializeField] private ToolBarController toolBarController;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("대사 그룹")]
    [SerializeField] private string firstDialogueGroupId = "DGRP_GAME_START_1";
    [SerializeField] private string secondDialogueGroupId = "DGRP_GAME_START_2";

    [Header("연출 메리 상태 Sprite")]
    [SerializeField] private List<NPCStateSprite> maryStateSprites = new List<NPCStateSprite>();

    [Header("대사 이벤트 매핑")]
    [SerializeField] private List<DialogueEventBinding> dialogueEventBindings = new List<DialogueEventBinding>();

    private readonly Dictionary<string, Sprite> maryStateDictionary = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, UnityEvent> dialogueEventDictionary = new Dictionary<string, UnityEvent>();

    private GameObject introItemObject;
    private Image introItemImage;

    private IntroPhase currentPhase = IntroPhase.None;
    private bool isCurrentlyHeldVisual = false;
    private bool isBootRoutineRunning = false;

    private int itemResolveWaitLogCounter = 0;
    private int dialogueWaitLogCounter = 0;

    private void LogDebug(string message)
    {
        if (!enableDebugLog) return;
        Debug.Log($"[IntroController] {message}", this);
    }

    private void LogPhase(string message)
    {
        if (!enableDebugLog) return;
        Debug.Log($"[IntroController][Phase:{currentPhase}] {message}", this);
    }

    private void Awake()
    {
        LogDebug("Awake 시작");

        BuildMaryStateDictionary();
        BuildDialogueEventDictionary();

        if (cutsceneMaryImage == null && cutsceneMaryObject != null)
        {
            cutsceneMaryImage = cutsceneMaryObject.GetComponentInChildren<Image>(true);
            LogDebug($"cutsceneMaryImage 자동 탐색 결과: {(cutsceneMaryImage != null ? cutsceneMaryImage.name : "null")}");
        }

        LogDebug($"Awake 끝 / maryStateDictionary 개수: {maryStateDictionary.Count}, dialogueEventDictionary 개수: {dialogueEventDictionary.Count}");
    }

    private void Start()
    {
        LogDebug("Start 시작");

        if (toolBarController == null)
            toolBarController = FindAnyObjectByType<ToolBarController>();

        if (itemManager == null)
            itemManager = ItemManager.Instance;

        if (dialogueManager == null)
            dialogueManager = FindAnyObjectByType<DialogueManager>();


        PlayIntro();

        if (realMaryObject != null)
        {
            realMaryObject.SetActive(false);
            LogDebug("realMaryObject SetActive(false)");
        }

        LogDebug("Start 끝");
    }

    private void Update()
    {
        if (currentPhase != IntroPhase.WaitingForPlayerAction &&
            currentPhase != IntroPhase.DraggingItem)
        {
            return;
        }

        CheckStorageSuccess();

        if (currentPhase == IntroPhase.SuccessTransition ||
            currentPhase == IntroPhase.SecondTimelinePlaying ||
            currentPhase == IntroPhase.SecondDialoguePlaying ||
            currentPhase == IntroPhase.Completed)
        {
            return;
        }

        if (toolBarController != null && toolBarController.isHoldingItem)
        {
            HandleHoldingState();
        }
        else
        {
            HandleReleaseState();
        }
    }

    public void PlayIntro()
    {
        LogPhase("PlayIntro 호출");

        if (isBootRoutineRunning)
        {
            LogDebug("PlayIntro 중단 - 이미 부트 루틴 실행 중");
            return;
        }

        if (currentPhase == IntroPhase.FirstTimelinePlaying ||
            currentPhase == IntroPhase.FirstDialoguePlaying ||
            currentPhase == IntroPhase.WaitingForPlayerAction ||
            currentPhase == IntroPhase.DraggingItem ||
            currentPhase == IntroPhase.SuccessTransition ||
            currentPhase == IntroPhase.SecondTimelinePlaying ||
            currentPhase == IntroPhase.SecondDialoguePlaying)
        {
            Debug.LogWarning($"{nameof(IntroController)}: 이미 인트로가 진행 중입니다.", this);
            return;
        }

        StartCoroutine(PlayIntroRoutine());
    }

    private IEnumerator PlayIntroRoutine()
    {
        LogDebug("PlayIntroRoutine 시작");
        isBootRoutineRunning = true;

        if (!ValidateReferences())
        {
            LogDebug("PlayIntroRoutine 중단 - ValidateReferences 실패");
            isBootRoutineRunning = false;
            yield break;
        }

        LogDebug("DialogueManager 로드 대기 시작");
        while (!(dialogueManager != null && dialogueManager.IsLoaded))
        {
            dialogueWaitLogCounter++;
            if (dialogueWaitLogCounter % 60 == 0)
            {
                LogDebug($"DialogueManager 로드 대기중... dialogueManager={(dialogueManager != null ? dialogueManager.name : "null")}, IsLoaded={(dialogueManager != null && dialogueManager.IsLoaded)}");
            }
            yield return null;
        }
        LogDebug("DialogueManager 로드 완료");

        LogDebug("아이템 메리 탐색 대기 시작");
        while (!TryResolveIntroItemRuntimeReferences(false))
        {
            itemResolveWaitLogCounter++;
            if (itemResolveWaitLogCounter % 60 == 0)
            {
                LogDebug($"아이템 메리 탐색 대기중... introItemData.id={(introItemData != null ? introItemData.id : "null")}, itemParent={(itemManager != null && itemManager.itemParent != null ? itemManager.itemParent.name : "null")}");
            }
            yield return null;
        }

        if (!TryResolveIntroItemRuntimeReferences(true))
        {
            LogDebug("PlayIntroRoutine 중단 - 아이템 메리 최종 검증 실패");
            isBootRoutineRunning = false;
            yield break;
        }

        LogDebug($"아이템 메리 탐색 성공: {introItemObject.name}");
        InitializeIntro();
        PlayFirstTimeline();

        isBootRoutineRunning = false;
        LogDebug("PlayIntroRoutine 끝");
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        LogDebug("ValidateReferences 시작");

        if (cutsceneMaryObject == null)
        {
            Debug.LogError($"{nameof(IntroController)}: cutsceneMaryObject가 비어 있습니다.", this);
            isValid = false;
        }

        if (realMaryObject == null)
        {
            Debug.LogError($"{nameof(IntroController)}: realMaryObject가 비어 있습니다.", this);
            isValid = false;
        }

        if (introItemData == null)
        {
            Debug.LogError($"{nameof(IntroController)}: introItemData가 비어 있습니다.", this);
            isValid = false;
        }

        if (firstTimelineDirector == null)
        {
            Debug.LogError($"{nameof(IntroController)}: firstTimelineDirector가 비어 있습니다.", this);
            isValid = false;
        }

        if (secondTimelineDirector == null)
        {
            Debug.LogError($"{nameof(IntroController)}: secondTimelineDirector가 비어 있습니다.", this);
            isValid = false;
        }

        if (itemManager == null)
        {
            Debug.LogError($"{nameof(IntroController)}: itemManager가 비어 있습니다.", this);
            isValid = false;
        }

        if (toolBarController == null)
        {
            Debug.LogError($"{nameof(IntroController)}: toolBarController가 비어 있습니다.", this);
            isValid = false;
        }

        if (dialogueManager == null)
        {
            Debug.LogError($"{nameof(IntroController)}: dialogueManager가 비어 있습니다.", this);
            isValid = false;
        }

        LogDebug($"ValidateReferences 끝 / 결과: {isValid}");
        return isValid;
    }

    private bool TryResolveIntroItemRuntimeReferences(bool logError)
    {
        introItemObject = FindSpawnedIntroItemObject();

        if (introItemObject == null)
        {
            if (logError)
                Debug.LogError($"{nameof(IntroController)}: 런타임에 생성된 [아이템] 메리를 찾지 못했습니다. introItemData.id={(introItemData != null ? introItemData.id : "null")}", this);
            return false;
        }

        introItemImage = introItemObject.GetComponentInChildren<Image>(true);

        if (introItemImage == null)
        {
            if (logError)
                Debug.LogError($"{nameof(IntroController)}: [아이템] 메리의 Image를 찾지 못했습니다.", introItemObject);
            return false;
        }

        return true;
    }

    private GameObject FindSpawnedIntroItemObject()
    {
        if (itemManager == null || itemManager.itemParent == null || introItemData == null)
            return null;

        Transform[] children = itemManager.itemParent.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == itemManager.itemParent)
                continue;

            if (child.name == introItemData.id)
                return child.gameObject;
        }

        return null;
    }

    private void InitializeIntro()
    {
        LogPhase("InitializeIntro 진입");

        SetCutsceneMaryActive(true);

        if (introItemObject != null)
        {
            introItemObject.SetActive(false);
            LogDebug($"아이템 메리 OFF: {introItemObject.name}");
        }

        if (realMaryObject != null)
        {
            realMaryObject.SetActive(false);
            LogDebug($"진짜 메리 OFF: {realMaryObject.name}");
        }

        ApplyDefaultSprite();

        introItemData.currentState = ItemState.Field;
        currentPhase = IntroPhase.Ready;

        LogPhase("InitializeIntro 완료");
    }

    private void PlayFirstTimeline()
    {
        currentPhase = IntroPhase.FirstTimelinePlaying;
        LogPhase($"PlayFirstTimeline 호출 / Director={(firstTimelineDirector != null ? firstTimelineDirector.name : "null")}");

        if (firstTimelineDirector != null)
        {
            firstTimelineDirector.time = 0;
            firstTimelineDirector.Evaluate();
            firstTimelineDirector.Play();
            LogDebug("Timeline 1 Play 실행");
        }
    }

    public void BeginFirstDialogueGroup()
    {
        LogPhase("BeginFirstDialogueGroup 호출");

        if (currentPhase != IntroPhase.FirstTimelinePlaying)
        {
            LogDebug("BeginFirstDialogueGroup 중단 - currentPhase가 FirstTimelinePlaying 아님");
            return;
        }

        currentPhase = IntroPhase.FirstDialoguePlaying;
        LogDebug($"대사 그룹 1 시작: {firstDialogueGroupId}");
        dialogueManager.PlayGroup(firstDialogueGroupId);
    }

    public void BeginSecondDialogueGroup()
    {
        LogPhase("BeginSecondDialogueGroup 호출");

        if (currentPhase != IntroPhase.SecondTimelinePlaying)
        {
            LogDebug("BeginSecondDialogueGroup 중단 - currentPhase가 SecondTimelinePlaying 아님");
            return;
        }

        currentPhase = IntroPhase.SecondDialoguePlaying;
        LogDebug($"대사 그룹 2 시작: {secondDialogueGroupId}");
        dialogueManager.PlayGroup(secondDialogueGroupId);
    }

    public void OnDialogueGroupCompleted(string groupId)
    {
        LogPhase($"OnDialogueGroupCompleted 호출 / groupId={groupId}");

        if (string.IsNullOrWhiteSpace(groupId))
        {
            LogDebug("OnDialogueGroupCompleted 중단 - groupId 비어 있음");
            return;
        }

        if (groupId == firstDialogueGroupId && currentPhase == IntroPhase.FirstDialoguePlaying)
        {
            LogDebug("첫 번째 대사 그룹 완료 처리 진입");
            OnFirstDialogueGroupCompleted();
            return;
        }

        if (groupId == secondDialogueGroupId && currentPhase == IntroPhase.SecondDialoguePlaying)
        {
            LogDebug("두 번째 대사 그룹 완료 처리 진입");
            OnSecondDialogueGroupCompleted();
            return;
        }

        LogDebug("OnDialogueGroupCompleted - 현재 phase와 groupId 조건이 맞지 않아 무시됨");
    }

    private void OnFirstDialogueGroupCompleted()
    {
        LogPhase("OnFirstDialogueGroupCompleted 호출");

        if (firstTimelineDirector != null && firstTimelineDirector.state == PlayState.Playing)
        {
            LogDebug("Timeline 1 정지");
            firstTimelineDirector.Stop();
        }

        SwitchToItemMary();
    }

    private void SwitchToItemMary()
    {
        LogPhase("SwitchToItemMary 호출");

        SyncItemMaryPositionFromCutsceneMary();

        SetCutsceneMaryActive(false);

        if (introItemObject != null)
        {
            introItemObject.SetActive(true);
            LogDebug($"아이템 메리 ON: {introItemObject.name}");
        }

        ApplyDefaultSprite();
        currentPhase = IntroPhase.WaitingForPlayerAction;

        LogPhase("아이템 메리 전환 완료");
    }

    private void SyncItemMaryPositionFromCutsceneMary()
    {
        Transform source = GetCutsceneMaryAnchorTransform();
        if (source == null || introItemObject == null)
        {
            LogDebug("SyncItemMaryPositionFromCutsceneMary 실패 - source 또는 introItemObject가 null");
            return;
        }

        introItemObject.transform.position = source.position;
        LogDebug($"아이템 메리 위치 동기화 완료 / sourcePos={source.position}");
    }

    private Transform GetCutsceneMaryAnchorTransform()
    {
        if (cutsceneMaryObject != null)
            return cutsceneMaryObject.transform;

        return null;
    }

    private void HandleHoldingState()
    {
        if (!isCurrentlyHeldVisual)
        {
            LogPhase("HandleHoldingState - heldSprite 적용");
            ApplyHeldSprite();
        }

        currentPhase = IntroPhase.DraggingItem;
    }

    private void HandleReleaseState()
    {
        if (isCurrentlyHeldVisual)
        {
            LogPhase("HandleReleaseState - defaultSprite 복귀");
            ApplyDefaultSprite();
        }

        currentPhase = IntroPhase.WaitingForPlayerAction;
    }

    private void CheckStorageSuccess()
    {
        if (introItemData == null)
            return;

        if (introItemData.currentState == ItemState.Storage)
        {
            LogPhase("CheckStorageSuccess - Storage 감지");
            OnItemStoredSuccess();
        }
    }

    private void ApplyDefaultSprite()
    {
        if (introItemImage != null && defaultSprite != null)
        {
            introItemImage.sprite = defaultSprite;
            LogDebug($"ApplyDefaultSprite 적용: {(defaultSprite != null ? defaultSprite.name : "null")}");
        }

        isCurrentlyHeldVisual = false;
    }

    private void ApplyHeldSprite()
    {
        if (introItemImage != null && heldSprite != null)
        {
            introItemImage.sprite = heldSprite;
            LogDebug($"ApplyHeldSprite 적용: {(heldSprite != null ? heldSprite.name : "null")}");
        }

        isCurrentlyHeldVisual = true;
    }

    private void OnItemStoredSuccess()
    {
        LogPhase("OnItemStoredSuccess 호출");

        currentPhase = IntroPhase.SuccessTransition;

        DeactivateItemMary();
        ActivateCutsceneMary();
        PlaySecondTimeline();
    }

    private void DeactivateItemMary()
    {
        if (introItemObject != null)
        {
            introItemObject.SetActive(false);
            LogDebug($"아이템 메리 OFF: {introItemObject.name}");
        }
    }

    private void ActivateCutsceneMary()
    {
        LogDebug("ActivateCutsceneMary 호출");
        SetCutsceneMaryActive(true);
    }

    private void SetCutsceneMaryActive(bool active)
    {
        LogDebug($"SetCutsceneMaryActive({active}) 호출");

        if (cutsceneMaryObject != null)
        {
            cutsceneMaryObject.SetActive(active);
            LogDebug($"cutsceneMaryObject SetActive({active})");
        }
    }

    private void PlaySecondTimeline()
    {
        currentPhase = IntroPhase.SecondTimelinePlaying;
        LogPhase($"PlaySecondTimeline 호출 / Director={(secondTimelineDirector != null ? secondTimelineDirector.name : "null")}");

        if (secondTimelineDirector != null)
        {
            secondTimelineDirector.time = 0;
            secondTimelineDirector.Evaluate();
            secondTimelineDirector.Play();
            LogDebug("Timeline 2 Play 실행");
        }
    }

    private void OnSecondDialogueGroupCompleted()
    {
        LogPhase("OnSecondDialogueGroupCompleted 호출");

        if (secondTimelineDirector != null && secondTimelineDirector.state == PlayState.Playing)
        {
            LogDebug("Timeline 2 정지");
            secondTimelineDirector.Stop();
        }

        FinalizeIntro();
    }

    public void ApplyCutsceneMaryState(string npcState)
    {
        LogDebug($"ApplyCutsceneMaryState 호출 / npcState={npcState}");

        if (string.IsNullOrWhiteSpace(npcState))
            return;

        if (cutsceneMaryImage == null)
        {
            LogDebug("ApplyCutsceneMaryState 중단 - cutsceneMaryImage가 null");
            return;
        }

        if (maryStateDictionary.TryGetValue(npcState, out Sprite sprite) && sprite != null)
        {
            cutsceneMaryImage.sprite = sprite;
            LogDebug($"cutsceneMaryImage Sprite 변경 완료: {sprite.name}");
        }
        else
        {
            LogDebug($"npcState 매핑 실패: {npcState}");
        }
    }

    public void HandleDialogueEvent(string eventKey)
    {
        LogDebug($"HandleDialogueEvent 호출 / eventKey={eventKey}");

        if (string.IsNullOrWhiteSpace(eventKey))
            return;

        if (dialogueEventDictionary.TryGetValue(eventKey, out UnityEvent unityEvent) && unityEvent != null)
        {
            LogDebug($"eventKey 실행: {eventKey}");
            unityEvent.Invoke();
        }
        else
        {
            Debug.Log($"[{nameof(IntroController)}] 등록되지 않은 eventKey: {eventKey}", this);
        }
    }

    private void BuildMaryStateDictionary()
    {
        maryStateDictionary.Clear();

        foreach (var item in maryStateSprites)
        {
            if (string.IsNullOrWhiteSpace(item.stateName))
                continue;

            maryStateDictionary[item.stateName] = item.sprite;
        }
    }

    private void BuildDialogueEventDictionary()
    {
        dialogueEventDictionary.Clear();

        foreach (var item in dialogueEventBindings)
        {
            if (string.IsNullOrWhiteSpace(item.eventKey))
                continue;

            dialogueEventDictionary[item.eventKey] = item.onEvent;
        }
    }

    private void FinalizeIntro()
    {
        LogPhase("FinalizeIntro 호출");

        ActivateRealMary();
        DestroyCutsceneMary();
        DestroyItemMary();

        currentPhase = IntroPhase.Completed;
        FinishIntro();
    }

    private void ActivateRealMary()
    {
        if (realMaryObject != null)
        {
            realMaryObject.SetActive(true);
            LogDebug($"진짜 메리 ON: {realMaryObject.name}");
        }
    }

    private void DestroyCutsceneMary()
    {
        if (cutsceneMaryContainer != null)
        {
            LogDebug($"연출 메리 컨테이너 삭제: {cutsceneMaryContainer.name}");
            Destroy(cutsceneMaryContainer);
            cutsceneMaryContainer = null;
            cutsceneMaryObject = null;
            cutsceneMaryImage = null;
            return;
        }

        if (cutsceneMaryObject != null)
        {
            LogDebug($"연출 메리 삭제: {cutsceneMaryObject.name}");
            Destroy(cutsceneMaryObject);
            cutsceneMaryObject = null;
            cutsceneMaryImage = null;
        }
    }

    private void DestroyItemMary()
    {
        if (introItemObject != null)
        {
            LogDebug($"아이템 메리 삭제: {introItemObject.name}");
            Destroy(introItemObject);
            introItemObject = null;
            introItemImage = null;
        }
    }

    private void FinishIntro()
    {
        LogPhase("인트로 연출 완료");
    }
}