using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OutlineHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("테두리로 사용할 자식 오브젝트")]
    public GameObject outlineObject;

    void Start()
    {
        // 시작할 때 테두리는 꺼둠
        if (outlineObject != null)
            outlineObject.SetActive(false);
    }

    // 마우스가 버튼 위로 올라왔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (outlineObject != null)
            outlineObject.SetActive(true);
    }

    // 마우스가 버튼 밖으로 나갔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        if (outlineObject != null)
            outlineObject.SetActive(false);
    }

    // 버튼이 비활성화될 때 테두리도 강제로 꺼주기 (버튼 클릭 후 팝업 뜰 때 잔상 방지)
    void OnDisable()
    {
        if (outlineObject != null)
            outlineObject.SetActive(false);
    }
}