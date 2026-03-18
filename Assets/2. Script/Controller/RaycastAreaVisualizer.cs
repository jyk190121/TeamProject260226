using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode] // 에디터 모드에서도 실시간으로 보이게 함
public class ChildRaycastVisualizer : MonoBehaviour
{
    [Header("시각화 설정")]
    public Color gizmoColor = Color.cyan; // 자식 영역은 청록색으로 표시
    public bool lookAtInactiveChildren = true; // 비활성화된 자식도 포함할지 여부

    void OnDrawGizmos()
    {
        // 부모 오브젝트 자신을 포함하여 자식들의 모든 Image 컴포넌트를 가져옴
        Image[] allImages = GetComponentsInChildren<Image>(lookAtInactiveChildren);

        if (allImages == null || allImages.Length == 0) return;

        Gizmos.color = gizmoColor;

        foreach (Image img in allImages)
        {
            // 부모 자신은 제외하고 자식만 그리고 싶다면 아래 주석 해제
            // if (img.gameObject == this.gameObject) continue;

            // Raycast Target이 꺼져있는 자식은 렌더링 영역만 체크하고 지나감 (선택 사항)
            // if (!img.raycastTarget) continue;

            RectTransform rt = img.rectTransform;
            if (rt == null) continue;

            // 자식 오브젝트의 로컬 행렬을 기즈모에 적용 (회전, 스케일 반영)
            Gizmos.matrix = rt.localToWorldMatrix;

            // Raycast Padding 값 가져오기
            Vector4 padding = img.raycastPadding;
            Rect r = rt.rect;

            // 패딩이 적용된 실제 클릭 판정 영역 계산
            Rect paddedRect = new Rect(
                r.x + padding.x,
                r.y + padding.y,
                r.width - (padding.x + padding.z),
                r.height - (padding.y + padding.w)
            );

            // 실제 판정 영역을 큐브 형태로 선 그리기
            Gizmos.DrawWireCube(paddedRect.center, paddedRect.size);
        }
    }
}