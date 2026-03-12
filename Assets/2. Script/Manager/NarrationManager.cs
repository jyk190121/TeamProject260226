using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static System.Net.WebRequestMethods;

public class NarrationManager : MonoBehaviour
{
    [Header("Data Load Settings")]
    public List<NarrationData> narrationList = new List<NarrationData>();
    public bool isLoaded = false;

    [SerializeField] private string narrationSheetURL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQo_EGkH-TAJnVVeBUWpvJf7PQB5t0vSkOpQWedjPuYTLLvAHZMrA-9FkFfuDboMg/pub?gid=218350455&single=true&output=csv"; // 나레이션 시트 URL
    [SerializeField] private string npcDialogueSheetURL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vRVFSzHhG97l748ccZKAiYlG5U3pvm2qO8EDR5p1bV_so7HAO9KLG16tOPUi_R3NPW2yje22WM_5ard/pub?gid=501557754&single=true&output=csv"; // NPC 대사 시트 URL

    [Header("UI Reference")]
    public GameObject bottomPanel;    // Main, Sub용 UI
    public GameObject bubblePanel;    // NARRATION, HINT 등 NPC 말풍선 UI
    public TextMeshProUGUI bottomText;
    public TextMeshProUGUI bubbleText;

    [Header("NPC Visuals")]
    public Image npcImage;
    public List<NPCStateSprite> npcStateSprites = new List<NPCStateSprite>();
    private Dictionary<string, Sprite> stateDictionary = new Dictionary<string, Sprite>();

    [System.Serializable]
    public struct NPCStateSprite { public string stateName; public Sprite sprite; }

    void Awake()
    {
        stateDictionary.Clear();
        foreach (var item in npcStateSprites)
            stateDictionary[item.stateName] = item.sprite;

        StartCoroutine(DownloadAllData());
    }

    IEnumerator DownloadAllData()
    {
        isLoaded = false;
        narrationList.Clear();

        // 두 시트 순차 로드
        yield return StartCoroutine(DownloadRoutine(narrationSheetURL, "Narration"));
        yield return StartCoroutine(DownloadRoutine(npcDialogueSheetURL, "NPC Dialogue"));

        isLoaded = true;
        Debug.Log($"[Manager] 전체 데이터 로드 완료: {narrationList.Count}행");
    }

    IEnumerator DownloadRoutine(string url, string label)
    {
        if (string.IsNullOrEmpty(url)) yield break;
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) ParseCSV(www.downloadHandler.text);
            else Debug.LogError($"[Manager] {label} 로드 실패: {www.error}");
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

    // DGRP, Stage, DialogueType 세 가지를 모두 체크하여 시작
    public void StartNarration(string dgrp, int stage, string targetType)
    {
        var group = narrationList
            .Where(x => x.DGRP == dgrp &&
                        x.Stage == stage &&
                        x.DialogueType.Equals(targetType, System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Sequence)
            .ToList();

        if (group.Count > 0)
        {
            StopAllCoroutines();
            StartCoroutine(PlayAutoRoutine(group));
        }
        else
        {
            Debug.LogWarning($"[Manager] 데이터를 찾을 수 없음: DGRP={dgrp}, Stage={stage}, Type={targetType}");
        }
    }

    IEnumerator PlayAutoRoutine(List<NarrationData> lines)
    {
        foreach (var line in lines)
        {
            // 1. NPC 표정 업데이트
            if (stateDictionary.ContainsKey(line.NpcState))
                npcImage.sprite = stateDictionary[line.NpcState];

            // 2. 타입에 따른 UI 분기 실행
            UpdateUI(line);

            if (line != lines.Last())
                yield return new WaitForSeconds(line.Delay);
        }
    }

    void UpdateUI(NarrationData line)
    {
        bottomPanel.SetActive(false);
        bubblePanel.SetActive(false);

        // Main/Sub은 하단, 나머지는 말풍선
        if (line.DialogueType.Equals("Main") || line.DialogueType.Equals("Sub"))
        {
            bottomPanel.SetActive(true);
            bottomText.text = line.Text;
        }
        else
        {
            bubblePanel.SetActive(true);
            bubbleText.text = line.Text;
        }
    }
}