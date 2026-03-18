using UnityEngine;

/// <summary>
///  색 규칙 계산 전용 static 유틸리티
/// Empty=0 Red=1 Blue=2 Purple=3 Yellow=4 Orange=5 Green=6 Black=7
/// </summary>
public static class ColorManager 
{
    // 룩업 테이블 (8*8, 정적 초기화)
    // [currentColorValue, newColorValue] -> resultColorValue
    private static readonly int[,] MixTable = BuildMixTable();

    private static int[,] BuildMixTable()
    {
        int[,] t = new int[8, 8];

        for (int cur = 0; cur < 8; cur++)
            for (int next = 0; next < 8; next++)
                t[cur, next] = ComputeMix(cur, next);


        return t;
    }

    private static int ComputeMix(int cur, int next)
    {
        // new == Empty -> 유지
        if (next == 0) return cur;
        // cur == Black -> 유지
        if (cur == 7) return cur;
        // cur == Empty -> new
        if (cur == 0) return next;

        bool curPrimary = cur == 1 || cur == 2 || cur == 4;
        bool nextPrimary = next == 1 || next == 2 || next == 4;
        bool curMixed = cur == 3 || cur == 5 || cur == 6;
        bool nextMixed = next == 3 || next == 5 || next == 6;

        if (curPrimary && nextPrimary)
        {
            if (cur == next) return cur;
            int sum = cur + next;
            return (sum >= 0 && sum <= 7) ? sum : cur;
        }

        if (curPrimary && nextMixed)
            return (cur + next == 7) ? 7 : next;

        if (curMixed && nextPrimary)
            return (cur + next == 7) ? 7 : cur;

        if (curMixed && nextMixed)
            return (cur == next) ? cur : 7;

        return cur;
    }

    /// <summary>
    /// 현재 색에 새 색을 혼합한 결과값 반환
    /// </summary>
    public static int MixColor(int current, int next)
    {
        // 범위 벗어난 값 방어
        if ((uint)current > 7 || (uint)next > 7) return current;
        return MixTable[current, next];
    }

    /// <summary>
    /// 테스트용 색 바로 박기
    /// </summary>
    public static int SetPaintColor(int colorValue)
        => ((uint)colorValue <= 7) ? colorValue : 0;

    private static readonly Color[] ColorTable =
    {
        Color.white,            //0 Empty
        Color.red,              //1 Red
        Color.blue,             //2 Blue
        Color.purple,           //3 Purple
        Color.yellow,           //4 Yellow
        Color.orange,           //5 Orange
        Color.green,            //6 Green
        Color.black,            //7 Black
    };

    public static Color GetColor(int colorValue)
        => ((uint)colorValue <= 7) ? ColorTable[colorValue] : ColorTable[0];
}





//using UnityEngine;


///// <summary>
///// 색 규칙 계산 전용 매니저
///// </summary>
//public class ColorManager : MonoBehaviour
//{
//    /// <summary>
//    /// 색상 규칙
//    /// Empty  = 0
//    /// Red    = 1
//    /// Blue   = 2
//    /// Purple = 3 (Red + Blue)
//    /// Yellow = 4
//    /// Orange = 5 (Red + Yellow)
//    /// Green  = 6 (Blue + Yellow)
//    /// Black  = 7 (Red + Blue + Yellow)
//    /// </summary>
//    private enum PaintColor
//    {
//        Empty = 0,
//        Red = 1,
//        Blue = 2,
//        Purple = 3,
//        Yellow = 4,
//        Orange = 5,
//        Green = 6,
//        Black = 7
//    }

//    [Header("색상")]
//    [SerializeField] private Color emptyColor = Color.white;
//    [SerializeField] private Color redColor = Color.red;
//    [SerializeField] private Color blueColor = Color.blue;
//    [SerializeField] private Color yellowColor = Color.yellow;
//    [SerializeField] private Color purpleColor = Color.purple;
//    [SerializeField] private Color orangeColor = Color.orange;
//    [SerializeField] private Color greenColor = Color.green;
//    [SerializeField] private Color blackColor = Color.black;


//    /// <summary>
//    /// 현재 색 상태에 새 원색을 적용한 결과 int 값을 반환
//    /// 무시 대상이면 현재 값을 그대로 반환
//    /// </summary
//    public int MixColor(int currentColorValue, int newColorValue)
//    {
//        PaintColor currentColor = ToPaintColor(currentColorValue);
//        PaintColor newColor = ToPaintColor(newColorValue);

//        // 1. newColor가 empty면 무시
//        if (IsEmpty(newColor))
//            return currentColorValue;

//        // 2. currentColor가 Black이면
//        if (IsBlack(currentColor))
//            return currentColorValue;

//        // 3. currentColor가 empty일 때 원색, 혼합색 그대로 return
//        if (IsEmpty(currentColor))
//            return newColorValue;

//        // 4. currentColor가 원색일 때
//        if (IsPrimaryColor(currentColor))
//        {
//            // 4-1 newColor가 원색일 때
//            if (IsPrimaryColor(newColor))
//            {
//                //4-1-1 서로 같으면 무시
//                if (currentColor == newColor)
//                    return currentColorValue;

//                //4-1-2 다른 원색이면 섞기
//                int mixedValue = currentColorValue + newColorValue;
//                return IsValidColorValue(mixedValue) ? mixedValue : currentColorValue;
//            }

//            // 4-2 newColor가 혼합색일 때
//            if (IsMixedColor(newColor))
//            {
//                // 4-2-1 cur+new = 7이면 Black
//                if (currentColorValue + newColorValue == (int)PaintColor.Black)
//                    return (int)PaintColor.Black;

//                // 4-2-2 7이 아닐 때는 newColor
//                return newColorValue;
//            }

//            // 방어용 혹시 모르는 나머지 무시
//            return currentColorValue;
//        }

//        // 5. currentColor가 혼합생일 때
//        if (IsMixedColor(currentColor))
//        {
//            // 5-1 newColor가 원색일 때
//            if (IsPrimaryColor(newColor))
//            {
//                // 5-1-1 합이 7이면 Black
//                if (currentColorValue + newColorValue == (int)PaintColor.Black)
//                    return (int)PaintColor.Black;

//                // 5-1-2 나머지 무시
//                return currentColorValue;
//            }

//            // 5-2 newColor가 혼합색일 때
//            if (IsMixedColor(newColor))
//            {
//                // 5-2-1 같은 혼합색이면 무시
//                if (currentColor == newColor)
//                    return currentColorValue;

//                // 5-2-2 다른 혼합색이면 Black
//                return (int)PaintColor.Black;
//            }

//            //방어용
//            return currentColorValue;
//        }

//        return currentColorValue;
//    }


//    /// <summary>
//    /// 특정 색으로 페인트통 색 값을 설정할 때 사용
//    /// 유효한 색 값이면 그대로 반환
//    /// 유효하지 않으면 0 반환 (Empty)
//    /// </summary>
//    /// <param name="colorValue"></param>
//    /// <returns></returns>
//    public int SetPaintColor(int colorValue)
//    {
//        return IsValidColorValue(colorValue) ? colorValue : (int)PaintColor.Empty;
//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <param name="colorValue"></param>
//    /// <returns></returns>
//    public Color GetColor(int colorValue)
//    {
//        switch (ToPaintColor(colorValue))
//        {
//            case PaintColor.Empty:
//                return emptyColor;

//            case PaintColor.Red:
//                return redColor;

//            case PaintColor.Blue:
//                return blueColor;

//            case PaintColor.Yellow:
//                return yellowColor;

//            case PaintColor.Purple:
//                return purpleColor;

//            case PaintColor.Orange:
//                return orangeColor;

//            case PaintColor.Green:
//                return greenColor;

//            case PaintColor.Black:
//                return blackColor;

//            default:
//                return emptyColor;
//        }
//    }

//    private PaintColor ToPaintColor(int colorValue)
//    {
//        if (System.Enum.IsDefined(typeof(PaintColor), colorValue))
//            return (PaintColor)colorValue;

//        return PaintColor.Empty;
//    }

//    private bool IsPrimaryColor(PaintColor color)
//    {
//        return  color == PaintColor.Red ||
//                color == PaintColor.Blue ||
//                color == PaintColor.Yellow;
//    }

//    private bool IsMixedColor(PaintColor color)
//    {
//        return  color == PaintColor.Purple ||
//                color == PaintColor.Orange ||
//                color == PaintColor.Green;
//    }

//    private bool IsEmpty(PaintColor color)
//    {
//        return color == PaintColor.Empty;
//    }

//    private bool IsBlack(PaintColor color)
//    {
//        return color == PaintColor.Black;
//    }

//    private bool IsValidColorValue(int colorValue)
//    {
//        return colorValue >= (int)PaintColor.Empty &&
//            colorValue <= (int)PaintColor.Black;
//    }
//}
