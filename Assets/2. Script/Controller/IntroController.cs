using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

public class IntroController : MonoBehaviour
{
    private enum IntroPhase
    {
        None,
        Ready,
        FirstTimelinePlaying,
        WaitingForPlayerAction,
        DraggingItem,
        SuccessTransition,
        SecondTimelinePlaying,
        Completed
    }

    [Header("연출용 메리")]
    [SerializeField] private GameObject cutsceneMaryObject;
    [SerializeField] private GameObject cutsceneMaryContainer;

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

    private GameObject introItemObject;
    private Image introItemImage;

    private IntroPhase currentPhase = IntroPhase.None;
    private bool isCurrentlyHeldVisual = false;

    private void OnEnable()
    {
        if (firstTimelineDirector != null)
            firstTimelineDirector.stopped += OnFirstTimelineStopped;

        if (secondTimelineDirector != null)
            secondTimelineDirector.stopped += OnSecondTimelineStopped;
    }

    private void OnDisable()
    {
        if (firstTimelineDirector != null)
            firstTimelineDirector.stopped -= OnFirstTimelineStopped;

        if (secondTimelineDirector != null)
            secondTimelineDirector.stopped -= OnSecondTimelineStopped;
    }

    private void Start()
    {
        if (toolBarController == null)
            toolBarController = FindAnyObjectByType<ToolBarController>();

        if (itemManager == null)
            itemManager = ItemManager.Instance;

        if (realMaryObject != null)
            realMaryObject.SetActive(false);

        if (cutsceneMaryObject != null)
            cutsceneMaryObject.SetActive(false);

        PlayIntro();
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
        if (currentPhase == IntroPhase.FirstTimelinePlaying ||
            currentPhase == IntroPhase.WaitingForPlayerAction ||
            currentPhase == IntroPhase.DraggingItem ||
            currentPhase == IntroPhase.SuccessTransition ||
            currentPhase == IntroPhase.SecondTimelinePlaying)
        {
            Debug.LogWarning($"{nameof(IntroController)}: 이미 인트로가 진행 중입니다.", this);
            return;
        }

        if (!ValidateReferences())
            return;

        if (!ResolveIntroItemRuntimeReferences())
            return;

        InitializeIntro();
        PlayFirstTimeline();
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

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

        return isValid;
    }

    private bool ResolveIntroItemRuntimeReferences()
    {
        introItemObject = FindSpawnedIntroItemObject();

        if (introItemObject == null)
        {
            Debug.LogError($"{nameof(IntroController)}: 런타임에 생성된 [아이템] 메리를 찾지 못했습니다.", this);
            return false;
        }

        introItemImage = introItemObject.GetComponentInChildren<Image>(true);

        if (introItemImage == null)
        {
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
        if (cutsceneMaryObject != null)
            cutsceneMaryObject.SetActive(true);

        if (introItemObject != null)
            introItemObject.SetActive(false);

        if (realMaryObject != null)
            realMaryObject.SetActive(false);

        ApplyDefaultSprite();

        introItemData.currentState = ItemState.Field;
        currentPhase = IntroPhase.Ready;
    }

    private void PlayFirstTimeline()
    {
        currentPhase = IntroPhase.FirstTimelinePlaying;
        firstTimelineDirector.Play();
    }

    private void OnFirstTimelineStopped(PlayableDirector director)
    {
        if (director != firstTimelineDirector)
            return;

        if (currentPhase != IntroPhase.FirstTimelinePlaying)
            return;

        OnFirstTimelineFinished();
    }

    private void OnFirstTimelineFinished()
    {
        SyncItemMaryPositionFromCutsceneMary();

        if (cutsceneMaryObject != null)
            cutsceneMaryObject.SetActive(false);

        if (introItemObject != null)
            introItemObject.SetActive(true);

        ApplyDefaultSprite();
        currentPhase = IntroPhase.WaitingForPlayerAction;
    }

    private void SyncItemMaryPositionFromCutsceneMary()
    {
        if (cutsceneMaryObject == null || introItemObject == null)
            return;

        introItemObject.transform.position = cutsceneMaryObject.transform.position;
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

        DeactivateItemMary();
        ActivateCutsceneMary();
        PlaySecondTimeline();
    }

    private void DeactivateItemMary()
    {
        if (introItemObject != null)
        {
            introItemObject.SetActive(false);
        }
    }

    private void ActivateCutsceneMary()
    {
        if (cutsceneMaryObject != null)
        {
            cutsceneMaryObject.SetActive(true);
        }
    }

    private void PlaySecondTimeline()
    {
        currentPhase = IntroPhase.SecondTimelinePlaying;
        secondTimelineDirector.Play();
    }

    private void OnSecondTimelineStopped(PlayableDirector director)
    {
        if (director != secondTimelineDirector)
            return;

        if (currentPhase != IntroPhase.SecondTimelinePlaying)
            return;

        OnSecondTimelineFinished();
    }

    /// <summary>
    /// 2차 타임라인 종료 후 처리.
    /// 
    /// 최종 흐름:
    /// 1. 진짜 메리 켜기
    /// 2. 연출용 메리 삭제
    /// 3. Hierarchy에 생성된 아이템 메리 프리팹 삭제
    /// 4. 인트로 종료
    /// </summary>
    private void OnSecondTimelineFinished()
    {
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
        }
    }

    private void DestroyCutsceneMary()
    {
        if (cutsceneMaryContainer != null)
        {
            Destroy(cutsceneMaryContainer);
            cutsceneMaryContainer = null;
        }
    }

    /// <summary>
    /// Hierarchy에 생성된 [아이템] 메리 프리팹을 삭제한다.
    /// 
    /// 주의:
    /// ItemManager 내부 딕셔너리에는 키가 남아 있을 수 있지만,
    /// 현재 구조에서는 인트로 이후 이 아이템을 다시 사용할 계획이 없으므로
    /// 일단 GameObject 제거만 해도 흐름상 문제는 크지 않다.
    /// </summary>
    private void DestroyItemMary()
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
        Debug.Log($"{nameof(IntroController)}: 인트로 연출이 완료되었습니다.", this);
    }
}