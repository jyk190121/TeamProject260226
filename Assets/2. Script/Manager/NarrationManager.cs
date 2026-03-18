using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NarrationManager : MonoBehaviour
{
    [Header("Data Load Settings")]
    public List<NarrationData> narrationList = new List<NarrationData>();
    public bool isLoaded = false;

    [SerializeField] private string narrationSheetURL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQo_EGkH-TAJnVVeBUWpvJf7PQB5t0vSkOpQWedjPuYTLLvAHZMrA-9FkFfuDboMg/pub?gid=218350455&single=true&output=csv";
    [SerializeField] private string npcDialogueSheetURL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vRVFSzHhG97l748ccZKAiYlG5U3pvm2qO8EDR5p1bV_so7HAO9KLG16tOPUi_R3NPW2yje22WM_5ard/pub?gid=501557754&single=true&output=csv";

    [Header("UI Reference")]
    public GameObject bottomPanel;
    public GameObject bubblePanel;
    public TextMeshProUGUI bottomText;
    public TextMeshProUGUI bubbleText;

    [Header("NPC Visuals")]
    public Image npcImage;
    public List<NPCStateSprite> npcStateSprites = new List<NPCStateSprite>();
    private Dictionary<string, Sprite> stateDictionary = new Dictionary<string, Sprite>();

    [System.Serializable]
    public struct NPCStateSprite { public string stateName; public Sprite sprite; }

    // ==========================================
    // [추가] 나레이션 클립 등록
    //
    // Inspector에서 클립을 등록할 때 key 규칙:
    //   "{DialogueType}{Stage}-{Sequence}"
    //   예) "Main1-1", "Main1-2", "Sub2-3"
    //
    // Image 1의 폴더 구조와 동일한 네이밍을 사용하면 됩니다.
    // ==========================================
    [Header("Narration Clips")]
    [SerializeField] private List<NarrationClipEntry> narrationClips = new List<NarrationClipEntry>();
    private Dictionary<string, AudioClip> clipDictionary = new Dictionary<string, AudioClip>();

    [System.Serializable]
    public class NarrationClipEntry
    {
        [Tooltip("형식: {DialogueType}{Stage}-{Sequence}  예) Main1-1, Sub2-3")]
        public string key;
        public AudioClip clip;
    }

    // ==========================================
    // [추가] 재생 이력 관리
    //
    // key 형식: "{DialogueType}_{Stage}"  예) "Main_1", "Sub_2"
    // 세션 내에서만 유지되며, 앱을 끄면 초기화됩니다.
    // 추후 PlayerPrefs로 전환 시 MarkPlayed / IsPlayed 두 메서드만 수정하면 됩니다.
    // ==========================================
    private HashSet<string> _playedSet = new HashSet<string>();

    // ==========================================
    // [추가] 코루틴 및 재생 상태 관리
    // StopAllCoroutines() 대신 개별 변수로 관리하여
    // 다른 코루틴에 영향을 주지 않습니다.
    // ==========================================
    private Coroutine _currentRoutine = null;
    private bool _isPlaying = false;
    private string _currentPlayKey = null; // 스킵 시 이력에 추가할 키

    void Awake()
    {
        stateDictionary.Clear();
        foreach (var item in npcStateSprites)
            stateDictionary[item.stateName] = item.sprite;

        // [추가] 클립 딕셔너리 초기화
        clipDictionary.Clear();
        foreach (var entry in narrationClips)
        {
            if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                clipDictionary[entry.key] = entry.clip;
        }

        StartCoroutine(DownloadAllData());
    }

    // ==========================================
    // [추가] C키 스킵 처리
    // 나레이션 재생 중에만 동작합니다.
    // ==========================================
    void Update()
    {
        if (!_isPlaying) return;

        if (Input.GetKeyDown(Key.C))
        {
            SkipNarration();
        }
    }

    IEnumerator DownloadAllData()
    {
        isLoaded = false;
        narrationList.Clear();

        yield return StartCoroutine(DownloadRoutine(narrationSheetURL, "Narration"));
        yield return StartCoroutine(DownloadRoutine(npcDialogueSheetURL, "NPC Dialogue"));

        isLoaded = true;
        Debug.Log($"[NarrationManager] 전체 데이터 로드 완료: {narrationList.Count}행");
    }

    IEnumerator DownloadRoutine(string url, string label)
    {
        if (string.IsNullOrEmpty(url)) yield break;
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) ParseCSV(www.downloadHandler.text);
            else Debug.LogError($"[NarrationManager] {label} 로드 실패: {www.error}");
        }
    }

    void ParseCSV(string rawText)
    {
        string csvText = rawText.Replace("\r\n", "\n");
        string[] lines = csvText.Split('\n');
        string pattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] fields = Regex.Split(lines[i], pattern);

            if (fields.Length < 8) continue;

            try
            {
                NarrationData data = new NarrationData();
                data.DGRP = fields[1].Trim();
                data.DialogueType = fields[2].Trim();
                data.NpcState = fields[3].Trim();
                data.Stage = int.Parse(fields[4].Trim());
                data.Sequence = int.Parse(fields[5].Trim());

                string cleanText = fields[6].Trim();
                if (cleanText.StartsWith("\"") && cleanText.EndsWith("\""))
                    cleanText = cleanText.Substring(1, cleanText.Length - 2);
                data.Text = cleanText.Replace("\"\"", "\"").Replace("\\n", "\n");

                data.Delay = float.Parse(fields[7].Trim());
                narrationList.Add(data);
            }
            catch { continue; }
        }
    }

    // ==========================================
    // [수정] StartNarration
    //
    // 기존 대비 추가된 기능:
    //   1. 재생 이력 체크 → 이미 재생된 경우 무시
    //   2. 데이터 미적재 시 경고
    //   3. 클릭 차단 (MouseClickManager)
    //   4. StopAllCoroutines() → 개별 코루틴 변수로 교체
    // ==========================================
    public void StartNarration(string dgrp, int stage, string targetType)
    {
        // 1. 재생 이력 체크
        string historyKey = MakeHistoryKey(targetType, stage);
        if (IsPlayed(historyKey))
        {
            Debug.Log($"[NarrationManager] 이미 재생된 나레이션입니다: {historyKey}");
            return;
        }

        // 2. 데이터 필터링
        var group = narrationList
            .Where(x => x.DGRP == dgrp &&
                        x.Stage == stage &&
                        x.DialogueType.Equals(targetType, System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Sequence)
            .ToList();

        if (group.Count == 0)
        {
            Debug.LogWarning($"[NarrationManager] 데이터를 찾을 수 없음: DGRP={dgrp}, Stage={stage}, Type={targetType}");
            return;
        }

        // 3. 기존 코루틴 정리
        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }

        // 4. 재생 시작
        _currentPlayKey = historyKey;
        _currentRoutine = StartCoroutine(PlayAutoRoutine(group));
    }

    // ==========================================
    // [수정] PlayAutoRoutine
    //
    // 기존 대비 추가된 기능:
    //   1. 클릭 차단 시작
    //   2. 클립 키 생성 후 SoundManager로 나레이션 클립 재생
    //   3. 마지막 항목 완료 후 → 이력 저장 + 클릭 복구 + UI 정리
    // ==========================================
    IEnumerator PlayAutoRoutine(List<NarrationData> lines)
    {
        _isPlaying = true;
        MouseClickManager.Instance?.SetClickEnable(false);

        foreach (var line in lines)
        {
            // 1. NPC 표정 업데이트
            if (npcImage != null && stateDictionary.ContainsKey(line.NpcState))
                npcImage.sprite = stateDictionary[line.NpcState];

            // 2. UI 텍스트 업데이트
            UpdateUI(line);

            // 3. 나레이션 클립 재생
            // 클립 키: "{DialogueType}{Stage}-{Sequence}"  예) "Main1-1", "Sub2-3"
            string clipKey = MakeClipKey(line.DialogueType, line.Stage, line.Sequence);
            if (clipDictionary.TryGetValue(clipKey, out AudioClip clip))
            {
                SoundManager.Instance?.PlayNarrationClip(clip);
            }
            else
            {
                Debug.LogWarning($"[NarrationManager] 클립을 찾을 수 없습니다: {clipKey}");
            }

            // 4. Delay 대기 (마지막 항목은 대기 없이 바로 종료)
            if (line != lines.Last())
                yield return new WaitForSeconds(line.Delay);
        }

        // 5. 재생 완료 처리
        FinishNarration();
    }

    // ==========================================
    // [추가] 나레이션 완료 공통 처리
    // PlayAutoRoutine 완료 시 & SkipNarration 호출 시 모두 사용
    // ==========================================
    private void FinishNarration()
    {
        // 이력 저장
        if (_currentPlayKey != null)
            MarkPlayed(_currentPlayKey);

        // UI 정리
        if (bottomPanel != null) bottomPanel.SetActive(false);
        if (bubblePanel != null) bubblePanel.SetActive(false);

        // 사운드 정리
        SoundManager.Instance?.StopNarration();

        // 상태 초기화
        _isPlaying = false;
        _currentRoutine = null;
        _currentPlayKey = null;

        // 클릭 복구
        MouseClickManager.Instance?.SetClickEnable(true);

        Debug.Log("[NarrationManager] 나레이션 재생 완료");
    }

    // ==========================================
    // [추가] C키 스킵 처리
    // 현재 재생 중인 코루틴을 중단하고 완료 처리합니다.
    // 스킵해도 재생 이력에 기록되어 다시 재생되지 않습니다.
    // ==========================================
    private void SkipNarration()
    {
        if (_currentRoutine != null)
        {
            StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }

        Debug.Log($"[NarrationManager] 나레이션 스킵: {_currentPlayKey}");
        FinishNarration();
    }

    void UpdateUI(NarrationData line)
    {
        if (bottomPanel != null) bottomPanel.SetActive(false);
        if (bubblePanel != null) bubblePanel.SetActive(false);

        if (line.DialogueType.Equals("Main") || line.DialogueType.Equals("Sub"))
        {
            if (bottomPanel != null) bottomPanel.SetActive(true);
            if (bottomText != null) bottomText.text = line.Text;
        }
        else
        {
            if (bubblePanel != null) bubblePanel.SetActive(true);
            if (bubbleText != null) bubbleText.text = line.Text;
        }
    }

    // ==========================================
    // [추가] 재생 이력 헬퍼 메서드
    //
    // 추후 PlayerPrefs로 전환 시 이 두 메서드만 수정하면 됩니다.
    //   MarkPlayed  → PlayerPrefs.SetInt(key, 1)
    //   IsPlayed    → PlayerPrefs.GetInt(key, 0) == 1
    // ==========================================
    private void MarkPlayed(string key) => _playedSet.Add(key);
    private bool IsPlayed(string key) => _playedSet.Contains(key);

    // ==========================================
    // [추가] 키 생성 헬퍼 메서드
    // ==========================================

    /// <summary>
    /// 재생 이력 키 생성.
    /// 형식: "{DialogueType}_{Stage}"  예) "Main_1", "Sub_2"
    /// </summary>
    private string MakeHistoryKey(string dialogueType, int stage)
        => $"{dialogueType}_{stage}";

    /// <summary>
    /// 클립 딕셔너리 조회 키 생성.
    /// 형식: "{DialogueType}{Stage}-{Sequence}"  예) "Main1-1", "Sub2-3"
    /// Inspector에 등록한 NarrationClipEntry의 key와 반드시 일치해야 합니다.
    /// </summary>
    private string MakeClipKey(string dialogueType, int stage, int sequence)
        => $"{dialogueType}{stage}-{sequence}";

    // ==========================================
    // [추가] 외부에서 재생 이력을 초기화할 때 사용
    // 예) 디버그 / 테스트 / 챕터 리셋 등
    // ==========================================
    public void ClearPlayedHistory() => _playedSet.Clear();
}
