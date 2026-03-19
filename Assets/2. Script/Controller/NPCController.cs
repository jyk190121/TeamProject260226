using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCController : MonoBehaviour
{
    public enum NPCState
    {
        Idle = 0,
        Hint = 1,    // 잠시 후 자동으로 Idle 복귀
        Talk = 2,    // NPC 대화 / 퍼즐 클리어 연출용
    }

    [Header("NPC SpriteRenderer")]
    [SerializeField] private Image image;

    [Header("Sprites")]
    [SerializeField] private Sprite idleSprite;     
    [SerializeField] private Sprite hintSprite;     
    [SerializeField] private Sprite talkSprite;

    [Header("State Settings")]
    [SerializeField] private NPCState startState = NPCState.Idle;
    [SerializeField] private float hintStateDuration = 1.0f;


    [Header("UI - Bubble")]
    [SerializeField] private GameObject speechBubble;
    [SerializeField] private TextMeshProUGUI hintText;

    private Sprite[] spriteTable;
    private NPCState currentState;
    private Coroutine returnCoroutine;
    private WaitForSeconds hintWait;

    public NPCState CurrentState => currentState;

    // ── 초기화 ────────────────────────────────────────────

    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();

        if (image == null)
        {
            Debug.LogError($"{nameof(NPCController)}: Image 참조가 없습니다.", this);
            return;
        }

        spriteTable = new Sprite[]
        {
            idleSprite,
            hintSprite  != null ? hintSprite  : idleSprite,
            talkSprite  != null ? talkSprite  : idleSprite,
        };

        hintWait = new WaitForSeconds(hintStateDuration);
    }

    private void Start()
    {
        currentState = startState;
        ApplySprite();
    }

    #region 공개 API

    /// <summary>외부에서 NPC 상태 변경</summary>
    public void SetState(NPCState newState)
    {
        if (currentState == newState) return;

        // Talk 상태 중에는 Hint 진입 차단 (Talk은 명시적 해제 전까지 유지)
        if (currentState == NPCState.Talk && newState == NPCState.Hint) return;

        StopReturnCoroutine();
        currentState = newState;
        ApplySprite();

        if (currentState == NPCState.Hint)
            returnCoroutine = StartCoroutine(ReturnToIdleRoutine());
    }

    /// <summary>강제로 Idle 복귀 (퍼즐 리셋 등)</summary>
    public void ResetToIdle()
    {
        StopReturnCoroutine();
        currentState = NPCState.Idle;
        ApplySprite();
    }

    #endregion

    private void ApplySprite()
    {
        if (image == null) return;
        int idx = (int)currentState;
        image.sprite = (idx >= 0 && idx < spriteTable.Length)
            ? spriteTable[idx]
            : spriteTable[0];
    }

    private void StopReturnCoroutine()
    {
        if (returnCoroutine == null) return;
        StopCoroutine(returnCoroutine);
        returnCoroutine = null;
    }

    private IEnumerator ReturnToIdleRoutine()
    {
        yield return hintWait;
        ResetToIdle();
    }



    // NPC가 힌트 대사를 출력하고 힌트 표정으로 바꿉니다.
    public void ShowHint(string message)
    {
        // 이미 Talk(중요 대화) 중이라면 힌트를 표시하지 않음
        if (currentState == NPCState.Talk) return;

        // 1. 상태 및 스프라이트 변경
        SetState(NPCState.Hint);

        // 2. 말풍선 켜기 및 텍스트 설정
        if (speechBubble != null) speechBubble.SetActive(true);
        if (hintText != null) hintText.text = message;

        // 3. ReturnToIdleRoutine이 이미 실행 중이면 멈추고 새로 시작 (시간 초기화)
        StopReturnCoroutine();
        returnCoroutine = StartCoroutine(ReturnToIdleWithBubble());
    }

    private IEnumerator ReturnToIdleWithBubble()
    {
        yield return hintWait; // 지정된 시간(hintStateDuration)만큼 대기

        // 말풍선 끄기 및 상태 복구
        if (speechBubble != null) speechBubble.SetActive(false);
        ResetToIdle();
    }
}