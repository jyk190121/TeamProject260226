using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class NarrationManager : MonoBehaviour
{
    public List<NarrationData> narrationList = new List<NarrationData>();
    public TextMeshProUGUI narrationText;

    void Awake() { LoadCSV(); }

    void LoadCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("NarrationData");
        if (csvFile == null) return;

        string csvText = csvFile.text.Replace("\r\n", "\n");
        string[] lines = csvText.Split('\n');
        string pattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] fields = Regex.Split(lines[i], pattern);

            if (fields.Length < 6) continue; // 컬럼이 6개인지 확인

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

                // 추가: Delay 컬럼 읽기
                data.Delay = float.Parse(fields[5].Trim());

                narrationList.Add(data);
            }
            catch { continue; }
        }
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

            // 마지막 문장이 아닐 때만 해당 대사의 Delay만큼 대기
            if (i < lines.Count - 1)
            {
                // 각 대사 데이터에 저장된 개별 Delay 값을 사용합니다.
                yield return new WaitForSeconds(lines[i].Delay);
            }
            else
            {
                // 마지막 문장은 고정
                yield break;
            }
        }
    }
}