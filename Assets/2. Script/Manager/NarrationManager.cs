using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions; // 정규식 사용을 위해 필요
using System.Linq;

public class NarrationManager : MonoBehaviour
{
    public List<NarrationData> narrationList = new List<NarrationData>();

    void Awake()
    {
        LoadCSV();
    }

    void LoadCSV()
    {
        // 1. Resources 폴더의 'NarrationData.csv' 파일을 읽어옴
        TextAsset csvFile = Resources.Load<TextAsset>("NarrationData");
        if (csvFile == null) return;

        // 2. 줄바꿈을 기준으로 행을 나눔
        string[] lines = csvFile.text.Split('\n');

        // 3. 정규식: 쉼표로 나누되, 큰따옴표 안의 쉼표는 구분자로 취급하지 않음
        string pattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";

        for (int i = 1; i < lines.Length; i++) // 첫 줄(헤더) 제외
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = Regex.Split(lines[i], pattern);

            NarrationData data = new NarrationData();
            data.Chapter = int.Parse(fields[0]);
            data.Type = fields[1];
            data.Stage = int.Parse(fields[2]);
            data.Sequence = int.Parse(fields[3]);

            // 양 끝의 큰따옴표(") 제거 및 엑셀의 줄바꿈 문자 처리
            string cleanText = fields[4].Trim();
            if (cleanText.StartsWith("\"") && cleanText.EndsWith("\""))
            {
                cleanText = cleanText.Substring(1, cleanText.Length - 2);
            }
            data.Text = cleanText.Replace("\"\"", "\""); // 연속된 큰따옴표 치환

            narrationList.Add(data);
        }
    }

    // 특정 상황의 데이터만 뽑아오는 함수
    public List<NarrationData> GetNarrationGroup(int id, string type, int subStage)
    {
        return narrationList.Where(x => x.Chapter == id && x.Type == type && x.Stage == subStage)
                            .OrderBy(x => x.Sequence)
                            .ToList();
    }
}