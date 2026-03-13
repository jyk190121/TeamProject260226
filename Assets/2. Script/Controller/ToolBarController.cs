using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Key = UnityEngine.InputSystem.Key;

public class ToolBarController : MonoBehaviour
{
    [Header("매니저 연결")]
    public ItemManager itemManager;
    public ColorManager colorManager;

    [Header("설정")]
    // 이제 아이템 잡을 때 레이어 이름에 의존하지 않으므로 더 안전합니다.
    public string storageLayerName = "ItemStorge"; // 보관함(그림판) 구역 레이어명
    public float pickupScaleMultiplier = 1.15f;
    public Vector2 hotSpot = Vector2.zero;

    [Header("도구 버튼")]
    public Button[] toolBtns = new Button[5];
    public Image bucketColorPreview;

    // 내부 상태 관리
    GameObject _currentMovingItem;
    bool _isHoldingItem = false;
    int _selectedToolIndex = 0;
    int _currentHeldColor = 0;
    bool _isZoomActive = false;


    Vector2 _originalPos;
    Vector3 _originalScale;

    void Start()
    {
        for (int i = 0; i < toolBtns.Length; i++)
        {
            int index = i;
            if (toolBtns[i] == null) continue;
            toolBtns[i].onClick.AddListener(() => SelectTool(index));
        }
        SelectTool(0);
    }

    void Update()
    {
        HandleNumericInput();
        if (Mouse.current == null) return;

        bool isLeftDown = Mouse.current.leftButton.wasPressedThisFrame;
        bool isRightDown = Mouse.current.rightButton.wasPressedThisFrame;
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (_isHoldingItem && _currentMovingItem != null)
        {
            if (Mouse.current.leftButton.isPressed) MoveItemWithMouse(mousePos);
            if (Mouse.current.leftButton.wasReleasedThisFrame) TryPlaceItem(mousePos);
        }
        else if (isLeftDown)
        {
            ExecuteToolAction(mousePos);
        }

        if (_selectedToolIndex == 2 && isRightDown) ClearBucket();
    }

    private void ExecuteToolAction(Vector2 mousePos)
    {
        Item targetData = GetItemAtMouse(mousePos, out GameObject hitObject);
        bool inStorage = IsMouseOverStorage(mousePos);

        switch (_selectedToolIndex)
        {
            case 0: TryPickUpItem(mousePos); break;
            case 1: // 스포이트
                if (targetData != null)
                {
                    if (_currentHeldColor == 0) _currentHeldColor = targetData.color;
                    else _currentHeldColor = colorManager.MixColor(_currentHeldColor, targetData.color);
                    UpdateBucketUI();
                    Debug.Log($"<color=cyan>[Spoid]</color> 조색됨: {_currentHeldColor}");
                }
                break;
            case 2: // 페인트 통
                if (targetData != null && inStorage)
                {
                    itemManager.UpdateItemColor(targetData.id, _currentHeldColor);
                    Debug.Log($"<color=yellow>[Paint]</color> {targetData.id}에 색상 적용");
                }
                break;
            case 3: // 지우개
                if (targetData != null && inStorage)
                {
                    itemManager.UpdateItemColor(targetData.id, 0);
                    Debug.Log($"<color=white>[Eraser]</color> {targetData.id} 색상 초기화(0)");
                }
                break;

            case 4: // [추가] 돋보기 (확대/축소)
                    // 1. 진입(ZoomIn)이 있는지 먼저 체크
                ZoomInTrigger zoomIn = GetUIComponentAtMouse<ZoomInTrigger>(mousePos);
                if (zoomIn != null)
                {
                    zoomIn.Execute(this);
                    break;
                }

                // 2. 퇴장(ZoomOut)이 있는지 체크
                ZoomOutTrigger zoomOut = GetUIComponentAtMouse<ZoomOutTrigger>(mousePos);
                if (zoomOut != null)
                {
                    zoomOut.Execute(this);
                    break;
                }
                break;
        }
    }

    public void UpdateMagnifierCursor(bool isZoomed)
    {
        _isZoomActive = isZoomed;
        // 나중에 여기서 커서 이미지를 교체하면 됩니다.
    }

    private T GetUIComponentAtMouse<T>(Vector2 mousePos) where T : Component
    {
        List<RaycastResult> results = GetUIElementsAtMouse(mousePos);
        foreach (var r in results)
        {
            T component = r.gameObject.GetComponentInParent<T>();
            if (component != null) return component;
        }
        return null;
    }

    private void ClearBucket() { _currentHeldColor = 0; UpdateBucketUI(); }
    private void UpdateBucketUI() { if (bucketColorPreview != null) bucketColorPreview.color = colorManager.GetColor(_currentHeldColor); }

    // ---------------------------------------------------
    // [기능] 마우스 포인터 아래의 아이템을 안전하게 찾는 로직
    // ---------------------------------------------------
    private Item GetItemAtMouse(Vector2 mousePos, out GameObject rootObject)
    {
        rootObject = null;
        List<RaycastResult> results = GetUIElementsAtMouse(mousePos);

        foreach (var r in results)
        {
            // 1. 클릭된 오브젝트 자체에서 ID 검사
            string id = r.gameObject.name.Replace("(Clone)", "").Trim();
            Item data = itemManager.GetItemDataById(id);

            if (data != null)
            {
                rootObject = r.gameObject;
                return data;
            }

            // 2. 만약 자식(Image)을 클릭했다면, 부모에서 ID 검사
            if (r.gameObject.transform.parent != null)
            {
                id = r.gameObject.transform.parent.name.Replace("(Clone)", "").Trim();
                data = itemManager.GetItemDataById(id);

                if (data != null)
                {
                    rootObject = r.gameObject.transform.parent.gameObject;
                    return data;
                }
            }
        }
        return null;
    }

    private bool IsMouseOverStorage(Vector2 mousePos)
    {
        List<RaycastResult> results = GetUIElementsAtMouse(mousePos);
        foreach (var r in results) if (LayerMask.LayerToName(r.gameObject.layer) == storageLayerName) return true;
        return false;
    }

    // ---------------------------------------------------
    // [기능] 드래그 앤 드롭 (무조건 잡히고, 무조건 따라옴)
    // ---------------------------------------------------
    private void TryPickUpItem(Vector2 mousePos)
    {
        Item data = GetItemAtMouse(mousePos, out GameObject rootObject);

        if (data != null && data.type == ItemType.A && rootObject != null)
        {
            _currentMovingItem = rootObject;
            _isHoldingItem = true;
            _currentMovingItem.transform.SetAsLastSibling();

            _originalPos = _currentMovingItem.transform.position;
            _originalScale = _currentMovingItem.transform.localScale;
            _currentMovingItem.transform.localScale = _originalScale * pickupScaleMultiplier;

            SetUIRaycastTarget(_currentMovingItem, false);
            Debug.Log($"<color=green>[PickUp]</color> {data.id} 잡기 성공!");
        }
    }

    // [수정] 아이템을 놓는 로직에 상태(State) 갱신 추가
    private void TryPlaceItem(Vector2 mousePos)
    {
        if (_currentMovingItem == null) return;

        // 다시 클릭 가능하게 복구 (성공 시 다시 꺼짐)
        SetUIRaycastTarget(_currentMovingItem, true);

        // 1. 마우스 아래에 정답 구역(AnswerZone)이 있는지 확인
        AnswerZone zone = GetUIComponentAtMouse<AnswerZone>(mousePos);
        Item data = itemManager.GetItemDataById(_currentMovingItem.name.Replace("(Clone)", "").Trim());

        if (zone != null)
        {
            // 2. 정답 구역이 있다면 숫자 ID와 색상 대조
            if (zone.CheckMatch(data, _currentMovingItem))
            {
                // [참고] 정답 처리 시 상태를 'Used'로 바꾸는 것은 AnswerZone 스크립트 내부에서 처리하는 것이 좋습니다.
                _currentMovingItem = null;
                _isHoldingItem = false;
                return;
            }
        }

        // 3. 정답이 아니거나 정답 구역이 아니면 보관함(Storage) 구역인지 검사
        if (IsMouseOverStorage(mousePos))
        {
            string id = _currentMovingItem.name.Replace("(Clone)", "").Trim();
            _currentMovingItem.transform.localScale = _originalScale;

            // [핵심 변경] 단순 위치 업데이트가 아닌, 상태(Storage)와 위치를 함께 업데이트합니다!
            itemManager.UpdateItemStateAndPosition(id, ItemState.Storage, _currentMovingItem.transform.position);

            Debug.Log($"<color=cyan>[상태 갱신]</color> {id} 아이템이 보관함(Storage)에 들어갔습니다.");
        }
        else
        {
            // 4. 보관함도, 정답 구역도 아닌 허공에 놓았다면 원래 위치로 강제 복귀 (상태는 여전히 Field)
            _currentMovingItem.transform.position = _originalPos;
            _currentMovingItem.transform.localScale = _originalScale;
        }

        _currentMovingItem = null;
        _isHoldingItem = false;
    }

    private void MoveItemWithMouse(Vector2 mousePos)
    {
        if (_currentMovingItem == null) return;

        // [핵심] 캔버스 스케일러가 적용된 Overlay 모드에서도 마우스와 일치하도록 보정
        RectTransform rt = _currentMovingItem.GetComponent<RectTransform>();
        if (rt != null && rt.parent != null)
        {
            RectTransform parentRT = rt.parent.GetComponent<RectTransform>();
            Canvas canvas = rt.GetComponentInParent<Canvas>();
            Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRT, mousePos, cam, out Vector3 worldPoint))
            {
                rt.position = worldPoint;
            }
        }
    }

    private List<RaycastResult> GetUIElementsAtMouse(Vector2 mousePos) { PointerEventData pd = new PointerEventData(EventSystem.current) { position = mousePos }; List<RaycastResult> rs = new List<RaycastResult>(); EventSystem.current.RaycastAll(pd, rs); return rs; }
    private void SetUIRaycastTarget(GameObject obj, bool s) { Graphic[] gs = obj.GetComponentsInChildren<Graphic>(true); foreach (Graphic g in gs) g.raycastTarget = s; }

    public void SelectTool(int index)
    {
        // 1. [핵심 추가] 아이템을 잡고 이동 중일 때는 도구 전환 입력을 완전히 무시합니다.
        if (_isHoldingItem)
        {
            Debug.Log("<color=red>[경고]</color> 아이템을 들고 있는 중에는 도구를 변경할 수 없습니다!");
            return; // 여기서 함수를 종료해버림
        }

        // 2. 정상적인 도구 변경 로직
        if (index < 0 || index >= toolBtns.Length || toolBtns[index] == null) return;

        _selectedToolIndex = index;
        ChangeCursorToButtonImage(index);

        Debug.Log($"<color=white><b>[Tool Switch]</b> {index + 1}번 도구로 변경되었습니다.</color>");
    }

    public void ChangeCursorToButtonImage(int index)
    {
        switch (index)
        {
            case 0:
                CursorManager.Instance.ChangeCursor(CursorState.HandOpen);
                break;
            case 1:
                CursorManager.Instance.ChangeCursor(CursorState.Spoid);
                break;
            case 2:
                CursorManager.Instance.ChangeCursor(CursorState.Paint);
                break;
            case 3:
                CursorManager.Instance.ChangeCursor(CursorState.Glasses);
                break;
        }

    }

    public CursorState GetCurrentToolCursorState()
    {
        switch (_selectedToolIndex)
        {
            case 0: return CursorState.HandOpen;
            case 1: return CursorState.Spoid;
            case 2: return CursorState.Paint;
            case 3: return CursorState.Glasses;
            default: return CursorState.Normal;
        }
    }

    private void HandleNumericInput() { if (Keyboard.current == null) return; for (int i = 0; i < 5; i++) if (Keyboard.current[Key.Digit1 + i].wasPressedThisFrame) SelectTool(i); }
}