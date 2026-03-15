using System.Collections;
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
}