using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    [Header("스킵")]
    [SerializeField] private bool enableSkip = true;

    [Header("연출용 Merry")]
    [SerializeField] private GameObject cutsceneMerryObject;
    [SerializeField] private GameObject cutsceneMerryContainer;
    [SerializeField] private Image cutsceneMerryImage;

    [Header("진짜 Merry")]
    [SerializeField] private GameObject realMerryObject;

    [Header("아이템용 Merry 데이터")]
    [SerializeField] private Item introItemData;

    [Header("아이템용 Merry Sprite")]
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

    [Header("연출 Merry 상태 Sprite")]
    [SerializeField] private List<NPCStateSprite> merryStateSprites = new List<NPCStateSprite>();

    [Header("대사 이벤트 매핑")]
    [SerializeField] private List<DialogueEventBinding> dialogueEventBindings = new List<DialogueEventBinding>();

    private readonly Dictionary<string, Sprite> merryStateDictionary = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, UnityEvent> dialogueEventDictionary = new Dictionary<string, UnityEvent>();
    private readonly HashSet<string> invokedEventKeys = new HashSet<string>();

    private GameObject introItemObject;
    private Image introItemImage;

    private IntroPhase currentPhase = IntroPhase.None;
    private bool isCurrentlyHeldVisual = false;
    private bool isBootRoutineRunning = false;

    private Coroutine introBootRoutine;

    private void Awake()
    {
        BuildMerryStateDictionary();
        BuildDialogueEventDictionary();

        if (cutsceneMerryImage == null && cutsceneMerryObject != null)
        {
            cutsceneMerryImage = cutsceneMerryObject.GetComponentInChildren<Image>(true);
        }
    }

    private void Start()
    {
        if (toolBarController == null)
            toolBarController = FindAnyObjectByType<ToolBarController>();

        if (itemManager == null)
            itemManager = ItemManager.Instance;

        if (dialogueManager == null)
            dialogueManager = FindAnyObjectByType<DialogueManager>();

        if (realMerryObject != null && !GameSceneManager.Instance.GetContinue())
        {
            realMerryObject.SetActive(false);
        }
        else cutsceneMerryContainer.SetActive(false);


        if (!GameSceneManager.Instance.GetContinue())
            PlayIntro();
    }

    private void Update()
    {
        HandleSkipInput();

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
        if (isBootRoutineRunning)
            return;

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

        introBootRoutine = StartCoroutine(PlayIntroRoutine());
    }

    private IEnumerator PlayIntroRoutine()
    {
        isBootRoutineRunning = true;

        if (!ValidateReferences())
        {
            isBootRoutineRunning = false;
            introBootRoutine = null;
            yield break;
        }

        yield return new WaitUntil(() => dialogueManager != null && dialogueManager.IsLoaded);

        while (!TryResolveIntroItemRuntimeReferences(false))
        {
            yield return null;
        }

        if (!TryResolveIntroItemRuntimeReferences(true))
        {
            isBootRoutineRunning = false;
            introBootRoutine = null;
            yield break;
        }

        InitializeIntro();
        PlayFirstTimeline();

        isBootRoutineRunning = false;
        introBootRoutine = null;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (cutsceneMerryObject == null)
        {
            Debug.LogError($"{nameof(IntroController)}: cutsceneMerryObject가 비어 있습니다.", this);
            isValid = false;
        }

        if (realMerryObject == null)
        {
            Debug.LogError($"{nameof(IntroController)}: realMerryObject가 비어 있습니다.", this);
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

        return isValid;
    }

    private bool TryResolveIntroItemRuntimeReferences(bool logError)
    {
        introItemObject = FindSpawnedIntroItemObject();

        if (introItemObject == null)
        {
            if (logError)
                Debug.LogError($"{nameof(IntroController)}: 런타임에 생성된 [아이템] Merry를 찾지 못했습니다. introItemData.id={(introItemData != null ? introItemData.id : "null")}", this);
            return false;
        }

        introItemImage = introItemObject.GetComponentInChildren<Image>(true);

        if (introItemImage == null)
        {
            if (logError)
                Debug.LogError($"{nameof(IntroController)}: [아이템] Merry의 Image를 찾지 못했습니다.", introItemObject);
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
        SetCutsceneMerryActive(true);

        if (introItemObject != null)
            introItemObject.SetActive(false);

        if (realMerryObject != null)
            realMerryObject.SetActive(false);

        ApplyDefaultSprite();

        introItemData.currentState = ItemState.Field;
        currentPhase = IntroPhase.Ready;
    }

    private void PlayFirstTimeline()
    {
        currentPhase = IntroPhase.FirstTimelinePlaying;

        if (firstTimelineDirector != null)
        {
            firstTimelineDirector.time = 0;
            firstTimelineDirector.Evaluate();
            firstTimelineDirector.Play();
        }
    }

    public void BeginFirstDialogueGroup()
    {
        if (currentPhase != IntroPhase.FirstTimelinePlaying)
            return;

        currentPhase = IntroPhase.FirstDialoguePlaying;
        dialogueManager.PlayGroup(firstDialogueGroupId);
    }

    public void BeginSecondDialogueGroup()
    {
        if (currentPhase != IntroPhase.SecondTimelinePlaying)
            return;

        currentPhase = IntroPhase.SecondDialoguePlaying;
        dialogueManager.PlayGroup(secondDialogueGroupId);
    }

    public void OnDialogueGroupCompleted(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            return;

        if (groupId == firstDialogueGroupId && currentPhase == IntroPhase.FirstDialoguePlaying)
        {
            OnFirstDialogueGroupCompleted();
            return;
        }

        if (groupId == secondDialogueGroupId && currentPhase == IntroPhase.SecondDialoguePlaying)
        {
            OnSecondDialogueGroupCompleted();
            return;
        }
    }

    private void OnFirstDialogueGroupCompleted()
    {
        if (firstTimelineDirector != null && firstTimelineDirector.state == PlayState.Playing)
        {
            firstTimelineDirector.Stop();
        }

        SwitchToItemMerry();
    }

    private void SwitchToItemMerry()
    {
        MouseClickManager.Instance.SetClickEnable(true);

        SyncItemMerryPositionFromCutsceneMerry();

        SetCutsceneMerryActive(false);

        if (introItemObject != null)
            introItemObject.SetActive(true);

        ApplyDefaultSprite();
        currentPhase = IntroPhase.WaitingForPlayerAction;
    }

    private void SyncItemMerryPositionFromCutsceneMerry()
    {
        Transform source = GetCutsceneMerryAnchorTransform();
        if (source == null || introItemObject == null)
            return;

        introItemObject.transform.position = source.position;
    }

    private Transform GetCutsceneMerryAnchorTransform()
    {
        if (cutsceneMerryObject != null)
            return cutsceneMerryObject.transform;

        return null;
    }

    private void HandleHoldingState()
    {
        if (!isCurrentlyHeldVisual)
        {
            ApplyHeldSprite();
        }

        currentPhase = IntroPhase.DraggingItem;
    }

    private void HandleReleaseState()
    {
        if (isCurrentlyHeldVisual)
        {
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
            OnItemStoredSuccess();
        }
    }

    private void ApplyDefaultSprite()
    {
        if (introItemImage != null && defaultSprite != null)
        {
            introItemImage.sprite = defaultSprite;
        }

        isCurrentlyHeldVisual = false;
    }

    private void ApplyHeldSprite()
    {
        if (introItemImage != null && heldSprite != null)
        {
            introItemImage.sprite = heldSprite;
        }

        isCurrentlyHeldVisual = true;
    }

    private void OnItemStoredSuccess()
    {
        currentPhase = IntroPhase.SuccessTransition;

        DeactivateItemMerry();
        ActivateCutsceneMerry();
        PlaySecondTimeline();
    }

    private void DeactivateItemMerry()
    {
        if (introItemObject != null)
        {
            introItemObject.SetActive(false);
        }
    }

    private void ActivateCutsceneMerry()
    {
        SetCutsceneMerryActive(true);
    }

    private void SetCutsceneMerryActive(bool active)
    {
        if (cutsceneMerryObject != null)
        {
            cutsceneMerryObject.SetActive(active);
        }
    }

    private void PlaySecondTimeline()
    {
        MouseClickManager.Instance.SetClickEnable(false);
        currentPhase = IntroPhase.SecondTimelinePlaying;

        if (secondTimelineDirector != null)
        {
            secondTimelineDirector.time = 0;
            secondTimelineDirector.Evaluate();
            secondTimelineDirector.Play();
        }
    }

    private void OnSecondDialogueGroupCompleted()
    {
        if (secondTimelineDirector != null && secondTimelineDirector.state == PlayState.Playing)
        {
            secondTimelineDirector.Stop();
            MouseClickManager.Instance.SetClickEnable(true);
        }

        FinalizeIntro();
    }

    public void ApplyCutsceneMerryState(string npcState)
    {
        if (string.IsNullOrWhiteSpace(npcState))
            return;

        if (cutsceneMerryImage == null)
            return;

        if (merryStateDictionary.TryGetValue(npcState, out Sprite sprite) && sprite != null)
        {
            cutsceneMerryImage.sprite = sprite;
        }
    }

    public void HandleDialogueEvent(string eventKey)
    {
        if (string.IsNullOrWhiteSpace(eventKey))
            return;

        if (invokedEventKeys.Contains(eventKey))
            return;

        invokedEventKeys.Add(eventKey);

        if (dialogueEventDictionary.TryGetValue(eventKey, out UnityEvent unityEvent) && unityEvent != null)
        {
            unityEvent.Invoke();
        }
        else
        {
            Debug.Log($"[{nameof(IntroController)}] 등록되지 않은 eventKey: {eventKey}", this);
        }
    }

    private void BuildMerryStateDictionary()
    {
        merryStateDictionary.Clear();

        foreach (var item in merryStateSprites)
        {
            if (string.IsNullOrWhiteSpace(item.stateName))
                continue;

            merryStateDictionary[item.stateName] = item.sprite;
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
        ActivateRealMerry();
        DestroyCutsceneMerry();
        DestroyItemMerry();

        currentPhase = IntroPhase.Completed;
        FinishIntro();
    }

    private void ActivateRealMerry()
    {
        if (realMerryObject != null)
        {
            realMerryObject.SetActive(true);
        }
    }

    private void DestroyCutsceneMerry()
    {
        if (cutsceneMerryContainer != null)
        {
            Destroy(cutsceneMerryContainer);
            cutsceneMerryContainer = null;
            cutsceneMerryObject = null;
            cutsceneMerryImage = null;
            return;
        }

        if (cutsceneMerryObject != null)
        {
            Destroy(cutsceneMerryObject);
            cutsceneMerryObject = null;
            cutsceneMerryImage = null;
        }
    }

    private void DestroyItemMerry()
    {
        if (introItemObject != null)
        {
            Destroy(introItemObject);
            introItemObject = null;
            introItemImage = null;
        }
    }

    private void FinishIntro()
    {
        Debug.Log($"{nameof(IntroController)}: 인트로 연출 완료", this);
    }

    private void HandleSkipInput()
    {
        if (!enableSkip)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            SkipWholeIntro();
            return;
        }

        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            SkipToItemReadyState();
        }
    }

    private void SkipToItemReadyState()
    {
        if (currentPhase != IntroPhase.Ready &&
            currentPhase != IntroPhase.FirstTimelinePlaying &&
            currentPhase != IntroPhase.FirstDialoguePlaying)
        {
            return;
        }

        CancelBootRoutineIfNeeded();
        dialogueManager?.StopCurrentGroup();

        ForceDirectorToEnd(firstTimelineDirector);
        InvokeAllEventKeysInGroup(firstDialogueGroupId);

        SwitchToItemMerry();
    }

    private void SkipWholeIntro()
    {
        if (currentPhase == IntroPhase.Completed)
            return;

        CancelBootRoutineIfNeeded();
        dialogueManager?.StopCurrentGroup();

        ForceDirectorToEnd(firstTimelineDirector);
        ForceDirectorToEnd(secondTimelineDirector);

        InvokeAllEventKeysInGroup(firstDialogueGroupId);
        InvokeAllEventKeysInGroup(secondDialogueGroupId);

        FinalizeIntro();
    }

    private void CancelBootRoutineIfNeeded()
    {
        if (introBootRoutine != null)
        {
            StopCoroutine(introBootRoutine);
            introBootRoutine = null;
        }

        isBootRoutineRunning = false;
    }

    private void InvokeAllEventKeysInGroup(string groupId)
    {
        if (dialogueManager == null)
            return;

        List<DialogueData> lines = dialogueManager.GetGroupLines(groupId);
        if (lines == null || lines.Count == 0)
            return;

        for (int i = 0; i < lines.Count; i++)
        {
            string eventKey = lines[i].EventKey;
            if (!string.IsNullOrWhiteSpace(eventKey))
            {
                HandleDialogueEvent(eventKey);
            }
        }
    }

    private void ForceDirectorToEnd(PlayableDirector director)
    {
        if (director == null || director.playableAsset == null)
            return;

        double duration = director.duration;
        if (duration <= 0d)
            return;

        director.time = duration;
        director.Evaluate();
        director.Pause();
    }
}