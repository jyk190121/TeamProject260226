using UnityEngine;

[System.Serializable]
public class NarrationData
{
    public int Chapter;    // Stage ID (1, 2, 3...)
    public string Type;    // Main / Sub
    public int Stage;      // 장면 구분 (기존의 Stage 컬럼)
    public int Sequence;   // 문장 순서
    public string Text;    // 나레이션 내용
    public float Delay;    // 나레이션 간 딜레이 (초 단위)
}
