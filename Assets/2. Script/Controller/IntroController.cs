using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

public class IntroController : MonoBehaviour
{
    /// <summary>
    /// 인트로 진행 단계를 관리하는 내부 상태.
    /// 
    /// 왜 필요한가:
    /// - 1차 타임라인 중에는 플레이어 입력 관련 판정을 하면 안 된다.
    /// - 성공 처리 중인데 다시 성공 처리로 들어가면 안 된다.
    /// - 2차 타임라인 중에는 다시 집힘/놓임 판정을 하면 안 된다.
    /// 
    /// 즉, 지금 인트로가 어느 단계에 있는지 명확히 구분하기 위해 사용한다.
    /// </summary>
    private enum IntroPhase
    {
        None,                   // 아직 아무 것도 시작하지 않은 상태
        Ready,                  // 초기화 완료, 1차 타임라인 시작 직전 상태
        FirstTimelinePlaying,   // 1차 타임라인 재생 중
        WaitingForPlayerAction, // 플레이어가 NPC 아이템을 집을 수 있는 대기 상태
        DraggingItem,           // NPC 아이템을 실제로 집어서 이동 중인 상태
        SuccessTransition,      // 성공 판정 후 최종 전환 처리 중
        SecondTimelinePlaying,  // 2차 타임라인 재생 중
        Completed               // 인트로 전체 종료
    }

    [Header("인트로 대상")]
    [SerializeField] private GameObject introItemObject;
    // 인트로에서 실제로 손으로 집어서 옮기는 NPC 아이템 오브젝트

    [SerializeField] private Image introItemImage;
    // 위 오브젝트에서 실제로 보이는 Image 컴포넌트
    // 기본 Sprite, 집힌 Sprite를 여기서 교체한다

    [SerializeField] private Item introItemData;
    // 이 NPC 아이템이 사용하는 Item ScriptableObject
    // ToolBarController가 ItemStorage에 놓으면 currentState를 Storage로 바꾸므로,
    // 이 값을 보고 성공 여부를 판정한다

    [Header("Sprite")]
    [SerializeField] private Sprite defaultSprite;
    // 기본 상태 Sprite

    [SerializeField] private Sprite heldSprite;
    // 손으로 집었을 때 보여줄 Sprite

    [Header("최종 결과")]
    [SerializeField] private GameObject finalBoardNpcObject;
    // 그림판 자리에 미리 배치해 둔 최종 NPC 오브젝트
    // 성공하면 introItemObject는 끄고, 이 오브젝트를 켠다

    [Header("Timeline")]
    [SerializeField] private PlayableDirector firstTimelineDirector;
    // 1차 타임라인
    // 역할:
    // - 빼꼼 등장
    // - 다시 숨기
    // - 걸어나오기
    // - 패널 등장 전후 연출

    [SerializeField] private PlayableDirector secondTimelineDirector;
    // 2차 타임라인
    // 역할:
    // - 그림판에 배치된 뒤의 마무리 연출
    // - 대사
    // - Idle 상태 정착
    //
    // 대사는 이 타임라인 안에 포함된다고 가정한다

    [Header("연결 대상")]
    [SerializeField] private ToolBarController toolBarController;
    // 현재 손으로 아이템을 들고 있는지 확인하기 위한 참조
    // ToolBarController의 IsHoldingItem 프로퍼티를 사용한다

    private IntroPhase currentPhase = IntroPhase.None;
    // 현재 인트로 단계 저장

    private bool isCurrentlyHeldVisual = false;
    // 현재 heldSprite가 적용된 상태인지 저장
    //
    // 왜 필요한가:
    // - heldSprite를 매 프레임 반복 적용하지 않기 위해
    // - 놓았을 때 기본 Sprite로 되돌려야 하는 시점을 명확히 판단하기 위해

    private void Awake()
    {
        // introItemImage를 인스펙터에 직접 연결하지 않았을 경우,
        // introItemObject의 자식들 중에서 Image를 자동으로 찾는다.
        if (introItemImage == null && introItemObject != null)
        {
            introItemImage = introItemObject.GetComponentInChildren<Image>(true);
        }
    }

    private void OnEnable()
    {
        // 각 타임라인이 끝났을 때 후속 처리를 하기 위해 stopped 이벤트 등록
        if (firstTimelineDirector != null)
            firstTimelineDirector.stopped += OnFirstTimelineStopped;

        if (secondTimelineDirector != null)
            secondTimelineDirector.stopped += OnSecondTimelineStopped;
    }

    private void OnDisable()
    {
        // 오브젝트가 비활성화되거나 제거될 때 이벤트 중복 등록을 막기 위해 해제
        if (firstTimelineDirector != null)
            firstTimelineDirector.stopped -= OnFirstTimelineStopped;

        if (secondTimelineDirector != null)
            secondTimelineDirector.stopped -= OnSecondTimelineStopped;
    }

    private void Start()
    {
        // 자동 시작은 하지 않는다.
        // 외부 씬 스크립트가 PlayIntro()를 호출해야 시작한다.
        PlayIntro();

        // 시작 시 최종 결과 오브젝트는 보이면 안 되므로 꺼둔다.
        if (finalBoardNpcObject != null)
        {
            finalBoardNpcObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 손으로 아이템을 집고 놓는 판정은
        // 1차 타임라인이 끝난 뒤에만 필요하다.
        if (currentPhase != IntroPhase.WaitingForPlayerAction &&
            currentPhase != IntroPhase.DraggingItem)
        {
            return;
        }

        // 먼저 성공 판정을 본다.
        // 성공이면 더 이상 집힘/놓임 판정을 볼 필요가 없다.
        CheckStorageSuccess();

        if (currentPhase == IntroPhase.SuccessTransition ||
            currentPhase == IntroPhase.SecondTimelinePlaying ||
            currentPhase == IntroPhase.Completed)
        {
            return;
        }

        // 손으로 아이템을 들고 있는지 여부를 본다.
        // IsHoldingItem이 true면 집은 상태, false면 놓은 상태다.
        if (toolBarController != null && toolBarController.isHoldingItem)
        {
            HandleHoldingState();
        }
        else
        {
            HandleReleaseState();
        }
    }

    /// <summary>
    /// 외부 씬 스크립트가 호출하는 인트로 시작 함수.
    /// 
    /// 예:
    /// sceneController에서 npcIntro.PlayIntro() 호출
    /// </summary>
    public void PlayIntro()
    {
        // 이미 진행 중이면 중복 시작 방지
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

        InitializeIntro();
        PlayFirstTimeline();
    }

    /// <summary>
    /// 인트로 실행 전 필수 참조가 모두 연결되어 있는지 확인한다.
    /// 하나라도 빠져 있으면 실행하지 않는다.
    /// </summary>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (introItemObject == null)
        {
            Debug.LogError($"{nameof(IntroController)}: introItemObject가 비어 있습니다.", this);
            isValid = false;
        }

        if (introItemImage == null)
        {
            Debug.LogError($"{nameof(IntroController)}: introItemImage가 비어 있습니다.", this);
            isValid = false;
        }

        if (introItemData == null)
        {
            Debug.LogError($"{nameof(IntroController)}: introItemData가 비어 있습니다.", this);
            isValid = false;
        }

        if (finalBoardNpcObject == null)
        {
            Debug.LogError($"{nameof(IntroController)}: finalBoardNpcObject가 비어 있습니다.", this);
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

        if (toolBarController == null)
        {
            Debug.LogError($"{nameof(IntroController)}: toolBarController가 비어 있습니다.", this);
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// 인트로 시작 전에 필요한 상태를 초기화한다.
    /// </summary>
    private void InitializeIntro()
    {
        // 이동용 인트로 NPC 아이템은 켠다.
        if (introItemObject != null)
        {
            introItemObject.SetActive(true);
        }

        // 그림판 자리에 미리 둔 최종 결과 NPC는 꺼둔다.
        if (finalBoardNpcObject != null)
        {
            finalBoardNpcObject.SetActive(false);
        }

        // 시작 시에는 기본 Sprite 상태여야 한다.
        ApplyDefaultSprite();

        // 인트로 시작 전에는 무조건 Field 상태라고 가정한다.
        // 만약 다른 시스템에서 이미 세팅해 준다면 이 줄은 빼도 된다.
        introItemData.currentState = ItemState.Field;

        currentPhase = IntroPhase.Ready;
    }

    /// <summary>
    /// 1차 타임라인을 시작한다.
    /// </summary>
    private void PlayFirstTimeline()
    {
        currentPhase = IntroPhase.FirstTimelinePlaying;
        firstTimelineDirector.Play();
    }

    /// <summary>
    /// 1차 타임라인 종료 이벤트 수신.
    /// </summary>
    private void OnFirstTimelineStopped(PlayableDirector director)
    {
        if (director != firstTimelineDirector)
            return;

        if (currentPhase != IntroPhase.FirstTimelinePlaying)
            return;

        OnFirstTimelineFinished();
    }

    /// <summary>
    /// 1차 타임라인이 끝난 뒤 호출된다.
    /// 이제부터 플레이어가 NPC 아이템을 집을 수 있다.
    /// </summary>
    private void OnFirstTimelineFinished()
    {
        ApplyDefaultSprite();
        currentPhase = IntroPhase.WaitingForPlayerAction;
    }

    /// <summary>
    /// 현재 손으로 아이템을 들고 있는 상태일 때의 처리.
    /// 
    /// IsHoldingItem이 true가 된 순간부터 heldSprite를 적용한다.
    /// </summary>
    private void HandleHoldingState()
    {
        // 이미 heldSprite 상태라면 다시 바꿀 필요 없음
        if (!isCurrentlyHeldVisual)
        {
            ApplyHeldSprite();
        }

        currentPhase = IntroPhase.DraggingItem;
    }

    /// <summary>
    /// 현재 손에서 아이템을 놓은 상태일 때의 처리.
    /// 
    /// 여기서는 성공 여부를 먼저 보지 않는다.
    /// 성공 여부는 CheckStorageSuccess()에서 이미 먼저 판단했다.
    /// 
    /// 즉 이 함수는:
    /// - 방금 놓았는데 성공은 아니었고
    /// - 그 전에는 집힌 상태였던 경우
    /// 기본 Sprite로 되돌리는 역할만 한다.
    /// </summary>
    private void HandleReleaseState()
    {
        // 손에 들고 있지 않은데,
        // 이전에 heldSprite 상태였다면 실패 또는 원위치 복귀로 간주하고 기본 Sprite로 복구
        if (isCurrentlyHeldVisual)
        {
            ApplyDefaultSprite();
        }

        currentPhase = IntroPhase.WaitingForPlayerAction;
    }

    /// <summary>
    /// 성공 판정.
    /// 
    /// ToolBarController는 ItemStorage 영역에 아이템을 놓으면
    /// introItemData.currentState를 Storage로 바꾼다.
    /// 
    /// 여기서는 그 값을 보고 성공 여부를 판단한다.
    /// </summary>
    private void CheckStorageSuccess()
    {
        if (introItemData == null)
            return;

        if (introItemData.currentState == ItemState.Storage)
        {
            OnItemStoredSuccess();
        }
    }

    /// <summary>
    /// 기본 Sprite를 적용한다.
    /// </summary>
    private void ApplyDefaultSprite()
    {
        if (introItemImage != null && defaultSprite != null)
        {
            introItemImage.sprite = defaultSprite;
        }

        isCurrentlyHeldVisual = false;
    }

    /// <summary>
    /// 손에 집힌 상태 Sprite를 적용한다.
    /// </summary>
    private void ApplyHeldSprite()
    {
        if (introItemImage != null && heldSprite != null)
        {
            introItemImage.sprite = heldSprite;
        }

        isCurrentlyHeldVisual = true;
    }

    /// <summary>
    /// 그림판 배치 성공 시 호출되는 인트로 전용 성공 처리.
    /// 
    /// 예전 AnswerZone.Success() 역할 중
    /// 인트로에 필요한 후처리만 여기서 담당한다.
    /// </summary>
    private void OnItemStoredSuccess()
    {
        currentPhase = IntroPhase.SuccessTransition;

        DeactivateIntroItem();
        ActivateFinalBoardNpc();
        PlaySecondTimeline();
    }

    /// <summary>
    /// 이동용 인트로 NPC 아이템을 끈다.
    /// </summary>
    private void DeactivateIntroItem()
    {
        if (introItemObject != null)
        {
            introItemObject.SetActive(false);
        }
    }

    /// <summary>
    /// 그림판 자리에 미리 배치한 최종 NPC 오브젝트를 켠다.
    /// </summary>
    private void ActivateFinalBoardNpc()
    {
        if (finalBoardNpcObject != null)
        {
            finalBoardNpcObject.SetActive(true);
        }
    }

    /// <summary>
    /// 2차 타임라인을 시작한다.
    /// 대사와 마무리 연출은 이 타임라인 안에 포함된다고 가정한다.
    /// </summary>
    private void PlaySecondTimeline()
    {
        currentPhase = IntroPhase.SecondTimelinePlaying;
        secondTimelineDirector.Play();
    }

    /// <summary>
    /// 2차 타임라인 종료 이벤트 수신.
    /// </summary>
    private void OnSecondTimelineStopped(PlayableDirector director)
    {
        if (director != secondTimelineDirector)
            return;

        if (currentPhase != IntroPhase.SecondTimelinePlaying)
            return;

        OnSecondTimelineFinished();
    }

    /// <summary>
    /// 2차 타임라인 종료 후 인트로 완료 처리.
    /// </summary>
    private void OnSecondTimelineFinished()
    {
        currentPhase = IntroPhase.Completed;
        FinishIntro();
    }

    /// <summary>
    /// 인트로 전체 종료.
    /// 필요하면 여기서 외부 시스템에 튜토리얼 종료 신호를 보낼 수 있다.
    /// </summary>
    private void FinishIntro()
    {
        Debug.Log($"{nameof(IntroController)}: 인트로 연출이 완료되었습니다.", this);
    }
}