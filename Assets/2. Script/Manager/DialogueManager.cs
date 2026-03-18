using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class DialogueManager : MonoBehaviour
{
    [Header("Data Load Settings")]
    [SerializeField]
    private string dialogueSheetURL =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vRVFSzHhG97l748ccZKAiYlG5U3pvm2qO8EDR5p1bV_so7HAO9KLG16tOPUi_R3NPW2yje22WM_5ard/pub?gid=501557754&single=true&output=csv";

    [SerializeField] private List<DialogueData> dialogueList = new List<DialogueData>();

    [Header("UI Reference")]
    [SerializeField] private GameObject bubblePanel;
    [SerializeField] private TextMeshProUGUI bubbleText;

    [Header("Linked Controller")]
    [SerializeField] private IntroController introController;

    private readonly Dictionary<string, List<DialogueData>> groupCache = new Dictionary<string, List<DialogueData>>();
    private Coroutine playRoutine;
    private bool isLoaded = false;
    private bool isPlaying = false;
    private string currentGroupId = string.Empty;

    public bool IsLoaded => isLoaded;
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        HideBubble();
        StartCoroutine(DownloadAllData());
    }

    private IEnumerator DownloadAllData()
    {
        isLoaded = false;
        dialogueList.Clear();
        groupCache.Clear();

        if (string.IsNullOrWhiteSpace(dialogueSheetURL))
        {
            Debug.LogError($"{nameof(DialogueManager)}: dialogueSheetURL이 비어 있습니다.", this);
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequest.Get(dialogueSheetURL))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"{nameof(DialogueManager)}: 대사 시트 로드 실패 - {www.error}", this);
                yield break;
            }

            ParseCSV(www.downloadHandler.text);
            BuildGroupCache();
            isLoaded = true;

            Debug.Log($"{nameof(DialogueManager)}: 전체 대사 로드 완료. 총 {dialogueList.Count}행", this);
        }
    }

    private void ParseCSV(string rawText)
    {
        string csvText = rawText.Replace("\r\n", "\n");
        string[] lines = csvText.Split('\n');
        string pattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] fields = Regex.Split(lines[i], pattern);
            fields = NormalizeFields(fields, 11);

            if (fields[0].Trim().StartsWith("#"))
                continue;

            DialogueData data = new DialogueData();

            data.DialogueId = CleanField(fields[0]);
            data.DialogueGroupId = CleanField(fields[1]);
            data.DialogueType = CleanField(fields[2]);
            data.NpcState = CleanField(fields[3]);
            data.Stage = ParseIntSafe(fields[4]);
            data.Sequence = ParseIntSafe(fields[5]);
            data.Text = CleanField(fields[6]);
            data.AutoAdvanceSec = ParseFloatSafe(fields[7]);
            data.NextDialogueId = CleanField(fields[8]);
            data.EventKey = CleanField(fields[9]);
            data.Remark = CleanField(fields[10]);

            if (string.IsNullOrWhiteSpace(data.DialogueGroupId) &&
                data.Sequence == 0 &&
                string.IsNullOrWhiteSpace(data.Text))
            {
                continue;
            }

            dialogueList.Add(data);
        }
    }

    private void BuildGroupCache()
    {
        groupCache.Clear();

        foreach (var group in dialogueList
                     .Where(x => !string.IsNullOrWhiteSpace(x.DialogueGroupId))
                     .GroupBy(x => x.DialogueGroupId))
        {
            groupCache[group.Key] = group
                .OrderBy(x => x.Sequence)
                .ToList();
        }
    }

    public bool HasGroup(string groupId)
    {
        return !string.IsNullOrWhiteSpace(groupId) &&
               groupCache.TryGetValue(groupId, out var lines) &&
               lines != null &&
               lines.Count > 0;
    }

    public List<DialogueData> GetGroupLines(string groupId)
    {
        if (!HasGroup(groupId))
            return new List<DialogueData>();

        return new List<DialogueData>(groupCache[groupId]);
    }

    public void PlayGroup(string groupId)
    {
        if (!isLoaded)
        {
            Debug.LogWarning($"{nameof(DialogueManager)}: 아직 대사 데이터가 로드되지 않았습니다.", this);
            return;
        }

        StopCurrentGroup();

        currentGroupId = groupId;

        if (!HasGroup(groupId))
        {
            Debug.LogWarning($"{nameof(DialogueManager)}: 그룹 '{groupId}' 대사를 찾지 못했습니다.", this);
            HideBubble();
            introController?.OnDialogueGroupCompleted(groupId);
            return;
        }

        playRoutine = StartCoroutine(PlayRoutine(groupCache[groupId], groupId));
    }

    public void StopCurrentGroup()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        isPlaying = false;
        currentGroupId = string.Empty;
        HideBubble();
    }

    private IEnumerator PlayRoutine(List<DialogueData> lines, string groupId)
    {
        isPlaying = true;

        for (int i = 0; i < lines.Count; i++)
        {
            DialogueData line = lines[i];

            introController?.ApplyCutsceneMerryState(line.NpcState);
            ShowBubble(line.Text);

            float wait = Mathf.Max(0f, line.AutoAdvanceSec);

            if (wait > 0f)
                yield return new WaitForSeconds(wait);
            else
                yield return null;

            introController?.HandleDialogueEvent(line.EventKey);
        }

        HideBubble();

        isPlaying = false;
        playRoutine = null;
        currentGroupId = string.Empty;

        introController?.OnDialogueGroupCompleted(groupId);
    }

    private void ShowBubble(string text)
    {
        if (bubblePanel != null)
            bubblePanel.SetActive(true);

        if (bubbleText != null)
            bubbleText.text = text;
    }

    private void HideBubble()
    {
        if (bubblePanel != null)
            bubblePanel.SetActive(false);

        if (bubbleText != null)
            bubbleText.text = string.Empty;
    }

    private static string[] NormalizeFields(string[] fields, int requiredLength)
    {
        if (fields.Length >= requiredLength)
            return fields;

        string[] newFields = new string[requiredLength];
        for (int i = 0; i < requiredLength; i++)
            newFields[i] = i < fields.Length ? fields[i] : string.Empty;

        return newFields;
    }

    private static string CleanField(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        string clean = raw.Trim();

        if (clean.StartsWith("\"") && clean.EndsWith("\"") && clean.Length >= 2)
            clean = clean.Substring(1, clean.Length - 2);

        return clean.Replace("\"\"", "\"").Replace("\\n", "\n").Trim();
    }

    private static int ParseIntSafe(string raw)
    {
        int.TryParse(CleanField(raw), out int value);
        return value;
    }

    private static float ParseFloatSafe(string raw)
    {
        float.TryParse(CleanField(raw), out float value);
        return value;
    }
}