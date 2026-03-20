using System;
using System.Buffers.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum ClearType { Page, Chapter, EventOnly }

public class AnswerZone : MonoBehaviour
{
    [Header("정답 조건")]
    public int targetID;
    public bool IgnoreColor;
    public int targetColor;

    [Header("위치 및 등장 조건")]
    public bool isMainZone = false;
    public int requiredChapter = 1;
    public int requiredPage = 1;

    [Header("클리어 설정")]
    public ClearType clearType = ClearType.Page;

    [Header("Narration")]
    [SerializeField] private NarrationManager narrationManager;

    public float duration = 0f;


    [Header("성공 시 실행할 이벤트")]
    public UnityEvent onCorrect;

    //아이템의 정답 상태 변수
    [HideInInspector] public bool isSolved = false;

    private void OnEnable()
    {
        narrationManager = FindAnyObjectByType<NarrationManager>();
    }

    // 아이템 정답 체크
    public bool CheckMatch(Item data, GameObject itemObj)
    {
        if (data == null || isSolved) return false;

        // 인스펙터의 설정된 챕터, 스테이지 인가?
        if (StageManager.Instance != null)
        {
            if (StageManager.Instance.CurrentChapter() != requiredChapter) return false;
            if (!isMainZone && StageManager.Instance.CurrentStage() != requiredPage) return false;

        }

        // 정답 아이템의 ID, 색상 체크
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

    // 성공시 로직
    private void Success(GameObject itemObj)
    {
        // 정답으로 변경.
        isSolved = true;

        // 정답용 아이템의 SO에 접근하여 정답이니 Used로 변경.
        string itemId = itemObj.name.Replace("(Clone)", "").Trim();
        if (ItemManager.Instance != null)
        {
            Item data = ItemManager.Instance.GetItemDataById(itemId);
            if (data != null) data.currentState = ItemState.Used;
        }



        // 터치(클릭) 무시 처리 및 정답존에 종속시킴
        itemObj.transform.position = this.transform.position;
        itemObj.transform.SetParent(this.transform);
        Graphic[] allGraphicComponents = itemObj.GetComponentsInChildren<Graphic>();
        foreach (var graphicComponent in allGraphicComponents) graphicComponent.raycastTarget = false;

        // 인스펙터상 설정한 이벤트 실행
        onCorrect?.Invoke();

        // Clear한 위치가 스테이지(Page)인지 챕터인지 확인하여 상승작업 실행
        if (clearType == ClearType.Page)
        {
            //스테이지(Page) 번호 증가
            StageManager.Instance.ClearStage();

            if (ItemManager.Instance != null)
            {
                // 새 스테이지의 아이템 및 배경 스폰
                ItemManager.Instance.SpawnItem(StageManager.Instance.CurrentChapter(), StageManager.Instance.CurrentStage());

                // 이어하기를 위해 현재 챕터/스테이지(Page), 아이템 위치 등을 즉시 저장
                ItemManager.Instance.ChangeItemPos("StageClear", Vector2.zero);

                // 나레이션 재생
                narrationManager.StartNarration("1", StageManager.Instance.CurrentStage(), "Sub");
            }
        }
        else if (clearType == ClearType.Chapter)
        {
            // 챕터 번호 증가 및 스테이지 1로 초기화
            StageManager.Instance.ClearChapter();

            if (ItemManager.Instance != null)
            {
                //챕터가 넘어갔을 때도 확실하게 저장
                ItemManager.Instance.ChangeItemPos("ChapterClear", Vector2.zero);
            }
        }
    }

    public void ResetZone()
    {
        isSolved = false;

        // 만약 Success에서 이 정답존의 자식으로 아이템을 넣었다면 정리
        foreach (Transform child in transform)
        {
            // 생성된 아이템(Clone)이 자식으로 있다면 파괴 (ItemManager에서 관리하지만 확실히 하기 위해)
            if (child.name.Contains("(Clone)"))
            {
                Destroy(child.gameObject);
            }
        }

        Debug.Log($"{gameObject.name} 정답존 리셋 완료");
    }
}