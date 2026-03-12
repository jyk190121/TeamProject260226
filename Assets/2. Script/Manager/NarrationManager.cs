using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class NarrationManager : MonoBehaviour
{
    public List<NarrationData> narrationList = new List<NarrationData>();
    public TextMeshProUGUI narrationText;

    public bool isLoaded = false;   // 불러 왔는가?

    private string sheetURL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQo_EGkH-TAJnVVeBUWpvJf7PQB5t0vSkOpQWedjPuYTLLvAHZMrA-9FkFfuDboMg/pub?gid=218350455&single=true&output=csv";

    void Awake() { StartCoroutine(DownloadCSV(sheetURL)); }

    IEnumerator DownloadCSV(string url)
    {
        isLoaded = false;  // 불러오기 시작

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("구글 시트에서 최신 데이터를 불러왔습니다.");
                ParseCSV(www.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning("온라인 연결 실패. 로컬 데이터를 불러옵니다: " + www.error);
                LoadLocalCSV();
            }
        }

        isLoaded = true;  // 로드 완료
    }

    // 로컬 Resources 폴더에서 불러오기 (백업용)
    void LoadLocalCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("NarrationData");
        if (csvFile != null) ParseCSV(csvFile.text);
    }

    // 공통 파싱 로직
    void ParseCSV(string rawText)
    {
        narrationList.Clear(); // 리스트 초기화 후 새로 담기

        string csvText = rawText.Replace("\r\n", "\n");
        string[] lines = csvText.Split('\n');
        string pattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] fields = Regex.Split(lines[i], pattern);

            if (fields.Length < 6) continue;

            try
            {
                NarrationData data = new NarrationData();
                data.Chapter = int.Parse(fields[0].Trim());
                data.Type = fields[1].Trim();
                data.Stage = int.Parse(fields[2].Trim());
                data.Sequence = int.Parse(fields[3].Trim());

                string cleanText = fields[4].Trim();
                if (cleanText.StartsWith("\"") && cleanText.EndsWith("\""))
                    cleanText = cleanText.Substring(1, cleanText.Length - 2);
                data.Text = cleanText.Replace("\"\"", "\"").Replace("\\n", "\n");

                data.Delay = float.Parse(fields[5].Trim());

                narrationList.Add(data);
            }
            catch { continue; }
        }
        Debug.Log($"파싱 완료: {narrationList.Count}개의 문장을 로드했습니다.");
    }

    public void StartNarration(int chapter, string type, int stage)
    {
        var group = narrationList
            .Where(x => x.Chapter == chapter && x.Type == type && x.Stage == stage)
            .OrderBy(x => x.Sequence)
            .ToList();

        if (group.Count > 0)
        {
            StopAllCoroutines();
            StartCoroutine(PlayAutoRoutine(group));
        }
    }

    IEnumerator PlayAutoRoutine(List<NarrationData> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            narrationText.text = lines[i].Text;
            if (i < lines.Count - 1)
            {
                yield return new WaitForSeconds(lines[i].Delay);
            }
            else
            {
                yield break;
            }
        }
    }

}
