using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum ClearType { Page, Chapter, EventOnly }

public class AnswerZone : MonoBehaviour
{
    [Header("정답 조건")]
    public int targetID;
    public Boolean IgnoreColor;
    public int targetColor;

    [Header("위치 및 등장 조건")]
    public bool isMainZone = false;
    public int requiredChapter = 1;
    public int requiredPage = 1;

    [Header("클리어 설정")]
    public ClearType clearType = ClearType.Page;

    [Header("Narration")]
    [SerializeField] private NarrationManager narrationManager;

    // ==========================================
    // [추가됨] 정답 제출 후 아이템을 어떻게 할지 에디터에서 선택!
    // ==========================================
    [Header("정답 아이템 처리 방식")]
    [Tooltip("체크 시 유저가 드래그해서 손을 놓은 '그 위치 그대로' 놔둡니다. 해제 시 정답존 중앙으로 자석처럼 붙습니다.")]
    public bool keepDroppedPosition = false;
    [Tooltip("체크 시 정답을 맞추면 아이템이 화면에서 아예 파괴되어 사라집니다.")]
    public bool destroyItemOnSuccess = false;

    [Header("성공 시 실행할 이벤트")]
    public UnityEvent onCorrect;

    [HideInInspector] public bool isSolved = false;

    private void OnEnable()
    {
        narrationManager = FindAnyObjectByType<NarrationManager>();
    }

    public bool CheckMatch(Item data, GameObject itemObj)
    {
        if (data == null || isSolved) return false;

        if (StageManager.Instance != null)
        {
            if (StageManager.Instance.CurrentChapter() != requiredChapter) return false;
            if (!isMainZone && StageManager.Instance.CurrentStage() != requiredPage) return false;
        }

        string baseId = data.id.Split('_')[0];
        if (int.TryParse(baseId, out int itemNumber))
        {
            if (itemNumber == targetID && (IgnoreColor || data.color == targetColor))
            {
                Success(itemObj);
                return true;
            }
        }
        return false;
    }

    private void Success(GameObject itemObj)
    {
        isSolved = true;

        // 아이템 처리 로직
        if (destroyItemOnSuccess)
        {
            itemObj.SetActive(false); // 화면에서 아예 없앰
        }
        else
        {
            // 중앙 자석 정렬 옵션을 켜뒀을 때만 위치 이동
            if (!keepDroppedPosition)
            {
                itemObj.transform.position = this.transform.position;
            }

            // 터치(클릭) 무시 처리 및 정답존에 종속시킴
            Graphic[] graphics = itemObj.GetComponentsInChildren<Graphic>();
            foreach (var g in graphics) g.raycastTarget = false;
            itemObj.transform.SetParent(this.transform);
        }

        onCorrect?.Invoke();

        if (clearType == ClearType.Page)
        {
            // 1. 스테이지 번호 증가 (예: 1-1 -> 1-2)
            StageManager.Instance.ClearStage();

            if (ItemManager.Instance != null)
            {
                // 2. 새 스테이지의 아이템 및 배경 스폰
                ItemManager.Instance.SpawnItem(StageManager.Instance.CurrentChapter(), StageManager.Instance.CurrentStage());

                // 3. ⭐️ 이어하기를 위해 현재 챕터/스테이지, 아이템 위치 등을 즉시 저장!
                ItemManager.Instance.ChangeItemPos("StageClear", Vector2.zero);

                // 4. 나레이션 재생
                narrationManager.StartNarration("1", StageManager.Instance.CurrentStage(), "Sub");
            }
        }
        else if (clearType == ClearType.Chapter)
        {
            // 1. 챕터 번호 증가 및 스테이지 1로 초기화
            StageManager.Instance.ClearChapter();

            if (ItemManager.Instance != null)
            {
                // 2. ⭐️ 챕터가 넘어갔을 때도 확실하게 저장!
                ItemManager.Instance.ChangeItemPos("ChapterClear", Vector2.zero);
            }
        }
    }
}