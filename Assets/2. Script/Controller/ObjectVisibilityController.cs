using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("스토리 연출/오브젝트 등장 조정기")]
public class ObjectVisibilityController : MonoBehaviour
{
    [Header("--- 🎬 오브젝트 등장 조정기 ---")]
    [Header("등장할 화면 위치")]
    [Tooltip("기본화면: Sub, 챕터화면: Main, 특정 확대화면: Zoom_Desk 등")]
    public string locationID = "Sub";

    [Tooltip("체크 시 Sub 페이지를 무시하고 해당 Main 챕터 내내 적용됩니다.")]
    public bool isMainObject = false;

    public int requiredChapter = 1;
    public int requiredPage = 1;

    public enum VisibilityType { OnlyThisPage, FromThisPageOnward }

    [Header("표시 방식 (이 페이지만 켤지, 이때부터 쭈욱 켤지)")]
    public VisibilityType visibilityType = VisibilityType.OnlyThisPage;

    [Header("상태 잠금")]
    [Tooltip("체크되면 ItemManager가 이 오브젝트의 활성화 상태를 강제로 바꾸지 못합니다.")]
    public bool isSolved = false;

    //리셋
    public void ResetState()
    {
        isSolved = false; // 잠금 해제

        if (gameObject.name == "NoClock") gameObject.SetActive(true);
        else if (gameObject.name == "YesClock") gameObject.SetActive(false);
        else if (gameObject.name == "Stage1_Door") gameObject.SetActive(true);
    }
}

[AddComponentMenu("스토리 연출/기믹 진행도 카운터")]
public class GimmickCounter : MonoBehaviour
{
    [Header("--- 🔢 기믹 진행도 카운터 ---")]
    [Header("이 페이지를 넘어가기 위한 목표 클리어 횟수")]
    public int targetCount = 3;
    private int currentCount = 0;

    [Header("목표 달성 시 실행할 이벤트")]
    public UnityEvent onTargetReached;

    private void OnEnable()
    {
        currentCount = 0;
    }

    public void AddClearCount()
    {
        currentCount++;
        if (currentCount >= targetCount)
        {
            onTargetReached?.Invoke();
        }
    }
}