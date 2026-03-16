using UnityEngine;


/// <summary>
/// 색 규칙 계산 전용 매니저
/// </summary>
public class ColorManager : MonoBehaviour
{
    /// <summary>
    /// 색상 규칙
    /// Empty = 0
    /// Red = 1
    /// Green = 2
    /// Blue = 4
    /// Yellow = 3 (Red + Green)
    /// Magenta = 5 (Red + Blue)
    /// Cyan = 6 (Green + Blue)
    /// Black = 7 (Red + Green + Blue)
    /// </summary>
    private enum PaintColor
    {
        Empty = 0,
        Red = 1,
        Green = 2,
        Yellow = 3,
        Blue = 4,
        Magenta = 5,
        Cyan = 6,
        Black = 7
    }

    [Header("색상")]
    [SerializeField] private Color emptyColor = Color.white;
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private Color greenColor = Color.green;
    [SerializeField] private Color blueColor = Color.blue;
    [SerializeField] private Color yellowColor = Color.yellow;
    [SerializeField] private Color magentaColor = Color.magenta;
    [SerializeField] private Color cyanColor = Color.cyan;
    [SerializeField] private Color blackColor = Color.black;

    /// <summary>
    /// 현재 색 상태에 새 원색을 적용한 결과 int 값을 반환
    /// 무시 대상이면 현재 값을 그대로 반환
    /// </summary>
    /// <param name="currentColorValue"></param>
    /// <param name="newPrimaryColorValue"></param>
    /// <returns></returns>
    public int MixColor(int currentColorValue, int newPrimaryColorValue)
    {
        PaintColor currentColor = ToPaintColor(currentColorValue);
        PaintColor newPrimaryColor = ToPaintColor(newPrimaryColorValue);

        //새 입력은 원색만 허용
        if (!IsPrimaryColor(newPrimaryColor))
            return currentColorValue;

        //Black 상태면 추가 입력 무시
        if (currentColor == PaintColor.Black)
            return currentColorValue;

        //Empty 상태면 새 원색 그대로 적용
        if (currentColor == PaintColor.Empty)
            return (int)newPrimaryColor;

        //이미 현재 상태에 포함된 원색이면 무시
        if (ContainsPrimary(currentColor, newPrimaryColor))
            return currentColorValue;

        int mixedValue = currentColorValue + newPrimaryColorValue;

        //유효 범위를 벗어나면 현재 값 유지
        if (!IsValidColorValue(mixedValue))
            return currentColorValue;

        return mixedValue;
    }

    /// <summary>
    /// 특정 색으로 페인트통 색 값을 설정할 때 사용
    /// 유효한 색 값이면 그대로 반환
    /// 유효하지 않으면 0 반환 (Empty)
    /// </summary>
    /// <param name="colorValue"></param>
    /// <returns></returns>
    public int SetPaintColor(int colorValue)
    {
        return IsValidColorValue(colorValue) ? colorValue : (int)PaintColor.Empty;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="colorValue"></param>
    /// <returns></returns>
    public Color GetColor(int colorValue)
    {
        switch (ToPaintColor(colorValue))
        {
            case PaintColor.Empty:
                return emptyColor;

            case PaintColor.Red:
                return redColor;

            case PaintColor.Green:
                return greenColor;

            case PaintColor.Blue:
                return blueColor;

            case PaintColor.Yellow:
                return yellowColor;

            case PaintColor.Magenta:
                return magentaColor;

            case PaintColor.Cyan:
                return cyanColor;

            case PaintColor.Black:
                return blackColor;

            default:
                return emptyColor;
        }
    }

    private PaintColor ToPaintColor(int colorValue)
    {
        if (System.Enum.IsDefined(typeof(PaintColor), colorValue))
            return (PaintColor)colorValue;

        return PaintColor.Empty;
    }

    private bool IsPrimaryColor(PaintColor color)
    {
        return  color == PaintColor.Red ||
                color == PaintColor.Green ||
                color == PaintColor.Blue;
    }

    private bool ContainsPrimary(PaintColor currentColor, PaintColor primaryColor)
    {
        if(!IsPrimaryColor(primaryColor)) 
            return false;

        return (((int)currentColor & (int)primaryColor) != 0);
    }

    private bool IsValidColorValue(int colorValue)
    {
        return colorValue >= (int)PaintColor.Empty &&
            colorValue <= (int)PaintColor.Black;
    }
}
