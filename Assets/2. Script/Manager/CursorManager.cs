using UnityEngine;
using System.Collections.Generic;

// 7개의 커서 이미지
// - 일반 커서, 모레시계 커서, 손모양(펴진 손, 완전히 쥔손, 반 쥔손) 커서,스포이드 커서, 페인트 커서, 지우개 커서, 돋보기 커서)
// 상태값에 따라 변경 (외부에서 호출하는 방식)

public enum CursorState
{
    Normal,         // 일반
    Loading,        // 모래시계
    HandOpen,       // 펴진 손
    HandHalf,       // 반 쥔 손
    HandClosed,     // 완전히 쥔 손
    Spoid,          // 스포이드
    Paint,          // 페인트
    Eraser,         // 지우개
    Glasses         // 돋보기
}
public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [System.Serializable]
    public struct CursorData
    {
        public CursorState state;
        public Texture2D texture;
        public Vector2 hotspot; // 커서의 어느 지점이 클릭 기준점인지 (예: 스포이드 끝)
    }

    [Header("커서 데이터 리스트")]
    public List<CursorData> cursorDataList;

    private Dictionary<CursorState, CursorData> cursorDict = new Dictionary<CursorState, CursorData>();
    private CursorState currentState;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 기본 커서로 설정
        ChangeCursor(CursorState.Normal);
    }

    // 리스트를 딕셔너리로 변환하여 검색 속도 최적화
    void InitDictionary()
    {
        foreach (var data in cursorDataList)
        {
            if (!cursorDict.ContainsKey(data.state))
            {
                cursorDict.Add(data.state, data);
            }
        }
    }

    // 외부 호출 함수
    public void ChangeCursor(CursorState newState)
    {
        if (cursorDict.ContainsKey(newState))
        {
            CursorData data = cursorDict[newState];

            // 유니티 API를 사용하여 커서 변경
            // CursorMode.Auto는 시스템에 따라 하드웨어/소프트웨어 커서를 자동 선택합니다.
            Cursor.SetCursor(data.texture, data.hotspot, CursorMode.Auto);

            currentState = newState;
        }
        else
        {
            print($"{newState} 상태에 해당하는 커서 데이터가 없습니다.");
        }
    }

    // 현재 상태 확인용 (필요 시)
    public CursorState GetCurrentState() => currentState;
}