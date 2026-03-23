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

    [Header("설정")]
    public string storageLayerName = "ItemStorge"; // 보관함(그림판) 구역 레이어명
    public float pickupScaleMultiplier = 1.15f;    // 손으로 잡을 때 커지는 크기.

    [Header("도구 버튼")]
    public Button[] toolBtns = new Button[5];
    public Image bucketColorPreview;

    [Header("돋보기 아이콘 설정")]
    public Image magnifierBtnImage;
    public Sprite plusSprite;
    public Sprite minusSprite;

    private bool _isStudyMode = true;

    // 내부 상태 관리
    GameObject _currentMovingItem;
    bool _isHoldingItem = false;
    int _selectedToolIndex = 0;
    int _currentHeldColor = 0;
    bool _isZoomActive = false;

    Vector2 _originalPos;
    Vector3 _originalScale;

    //NPC Intro용 public
    public bool isHoldingItem => _isHoldingItem;

    void Start()
    {
        //버튼 등록
        for (int btnIndex = 0; btnIndex < toolBtns.Length; btnIndex++)
        {
            int index = btnIndex;
            if (toolBtns[btnIndex] == null) continue;
            toolBtns[btnIndex].onClick.AddListener(() => SelectTool(index));
        }
        //초기 도구 0번(손) 설정
        SelectTool(0);
    }

    void Update()
    {
        //도구 단축키 인식
        HandleNumericInput();
        
        if (Mouse.current == null) return;

        //마우스 입력 인식
        bool isLeftDown = Mouse.current.leftButton.wasPressedThisFrame;
        bool isRightDown = Mouse.current.rightButton.wasPressedThisFrame;
        Vector2 mousePos = Mouse.current.position.ReadValue();

        //Drag-and-Drop 행동 시
        if (_isHoldingItem && _currentMovingItem != null)
        {
            if (Mouse.current.leftButton.isPressed) MoveItemWithMouse(mousePos);
            if (Mouse.current.leftButton.wasReleasedThisFrame) TryPlaceItem(mousePos);
        }
        else if (isLeftDown)// 클릭 행동시
        {
            ExecuteToolAction(mousePos);
        }

        //페인트 통 비우기.
        if (_selectedToolIndex == 2 && isRightDown) ClearBucket();
    }


    // 아이템 마우스 포인터 위치로 갱신.
    private void MoveItemWithMouse(Vector2 mousePos)
    {
        if (_currentMovingItem == null) return;

        if (_currentMovingItem.TryGetComponent(out RectTransform rectTransform) && rectTransform.parent != null)
        {
            // 부모는 무조건 RectTransform이므로 형변환(Casting)으로 처리
            RectTransform parentRectTransform = (RectTransform)rectTransform.parent;
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            Camera worldCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRectTransform, mousePos, worldCamera, out Vector3 worldPoint))
            {
                rectTransform.position = worldPoint;
            }
        }
    }

    //놓을 때 아이템 장소 확인.
    private void TryPlaceItem(Vector2 mousePos)
    {
        if (_currentMovingItem == null) return;

       
        SetUIRaycastTarget(_currentMovingItem, true);

        //아이템의 정보 확인.
        AnswerZone answerZone = GetUIComponentAtMouse<AnswerZone>(mousePos);
        string id = _currentMovingItem.name.Replace("(Clone)", "").Trim();
        Item data = itemManager.GetItemDataById(id);
        NPCController npc = FindAnyObjectByType<NPCController>();

        if (answerZone != null)
        {
            if (answerZone.CheckMatch(data, _currentMovingItem))// 정답인 경우
            {
                _currentMovingItem = null;
                _isHoldingItem = false;
                return;
            }
            else//오답인 경우.
            {
                if (!IsMouseOverStorage(mousePos) && npc != null)
                {
                    if (IsThisItemForAnotherPage(data))
                    {
                        npc.ShowRandomHint(npc.wrongLocationHints);
                    }
                    else
                    {
                        npc.ShowRandomHint(npc.wrongItemHints);
                    }
                }
            }
        }

        // 아이템 스토리지 위에 있으면 그 위치에 아이템을 놓을 수 있고
        if (IsMouseOverStorage(mousePos))
        {
            itemManager.UpdateItemStateAndPosition(id, ItemState.Storage, _currentMovingItem.transform.position);
            SoundManager.Instance.PlaySfx(SfxId.HandDrop);
        }
        else // 아니라면 원래위치로 복귀
        {
            _currentMovingItem.transform.position = _originalPos;
            
        }
        _currentMovingItem.transform.localScale = _originalScale;
        _currentMovingItem = null;
        _isHoldingItem = false;
    }

    //아이템을 들고 있는 상태일 때 들고있는 아이템 아래 확인 코드
    private void SetUIRaycastTarget(GameObject obj, bool isTarget)
    {
        Graphic[] allGraphicComponents = obj.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphicComponent in allGraphicComponents)
        {
            graphicComponent.raycastTarget = isTarget;
        }
    }

    //아이템 스토리지 레이어 확인 함수.
    private bool IsMouseOverStorage(Vector2 mousePos)
    {
        List<RaycastResult> raycastResults = GetUIElementsAtMouse(mousePos);

        foreach (RaycastResult hit in raycastResults)
        {
            if (LayerMask.LayerToName(hit.gameObject.layer) == storageLayerName)
            {
                return true; 
            }
        }
        return false;
    }


    //---------------------------------------------------------------------------------------

    //서재(Study) 진입 체크
    public void EnableStudyMode()
    {
        _isStudyMode = true;

        // 서재로 오면 무조건 손(0번) 도구로 강제 변경
        SelectTool(0);
        Debug.Log("<color=cyan>[Toolbar]</color> 서재 모드 활성화: 손 도구 고정 및 다른 도구 잠금");
    }

    //Story(MainStory, SubStory) 진입 체크
    public void EnableStoryMode()
    {
        _isStudyMode = false;
        Debug.Log("<color=cyan>[Toolbar]</color> 스토리 모드 활성화: 모든 도구 사용 가능");
    }

    // 마우스 클릭시 행동 로직.
    private void ExecuteToolAction(Vector2 mousePos)
    {
        Item targetData = GetItemAtMouse(mousePos, out GameObject hitObject);
        bool inStorage = IsMouseOverStorage(mousePos);

        switch (_selectedToolIndex)
        {
            case 0: //손
                TryPickUpItem(mousePos);
                SoundManager.Instance.PlaySfx(SfxId.HandUp);
                break;

            case 1: // 스포이트
                if (targetData != null)
                {
                    if (_currentHeldColor == 0) _currentHeldColor = targetData.color;
                    else _currentHeldColor = ColorManager.MixColor(_currentHeldColor, targetData.color);

                    UpdateBucketUI();
                    SoundManager.Instance.PlaySfx(SfxId.Spoid);
                    Debug.Log($"<color=cyan>[Spoid]</color> 조색됨: {_currentHeldColor}");
                }
                break;

            case 2: // 페인트 통
                if (targetData != null && inStorage)
                {
                    if (_currentHeldColor == 0)
                    {
                        Debug.Log("<color=white>통이 비어있어 색을 칠할 수 없습니다!</color>");
                        return;
                    }

                    if (_currentHeldColor == 7)
                    {
                        Debug.Log("<color=red>색이 너무 탁해져서(검정) 칠할 수 없습니다!</color>");
                        return;
                    }

                    itemManager.UpdateItemColor(targetData.id, _currentHeldColor);
                    SoundManager.Instance.PlaySfx(SfxId.Paint);
                    Debug.Log($"<color=yellow>[Paint]</color> {targetData.id}에 색상 적용");
                }
                break;

            case 3: // 돋보기 (확대/축소)
                ZoomInTrigger zoomIn = GetUIComponentAtMouse<ZoomInTrigger>(mousePos);
                if (zoomIn != null)
                {
                    zoomIn.Execute(this);
                    SoundManager.Instance.PlaySfx(SfxId.ZoomIn);
                    break;
                }

                ZoomOutTrigger zoomOut = GetUIComponentAtMouse<ZoomOutTrigger>(mousePos);
                if (zoomOut != null)
                {
                    zoomOut.Execute(this);
                    SoundManager.Instance.PlaySfx(SfxId.ZoomOut);
                    break;
                }
                break;
        }
    }

    //Drag & Drop
    private void TryPickUpItem(Vector2 mousePos)
    {
        Item data = null;

        if (!MouseClickManager.Instance.IsClickBlocked)
        {
            data = GetItemAtMouse(mousePos, out GameObject rootObject);

            if (data != null && data.type == ItemType.A && rootObject != null)
            {
                _currentMovingItem = rootObject;
                _isHoldingItem = true;
                _currentMovingItem.transform.SetAsLastSibling();

                _originalPos = _currentMovingItem.transform.position;
                _originalScale = _currentMovingItem.transform.localScale;
                _currentMovingItem.transform.localScale = _originalScale * pickupScaleMultiplier;

                Debug.Log($"<color=green>[PickUp]</color> {data.id} 잡기 성공!");
            }
        }
    }

    //페인트 통
    private void UpdateBucketUI() 
    {
        if (bucketColorPreview != null) bucketColorPreview.color = ColorManager.GetColor(_currentHeldColor); 
    }
    private void ClearBucket()
    {
        _currentHeldColor = 0;
        SoundManager.Instance.PlaySfx(SfxId.EmptyPaint);
        UpdateBucketUI();
    }

    // 돋보기 커서 상태와 UI 아이콘 변경을 통합 함수
    public void SetZoomState(bool isZoomed)
    {
        _isZoomActive = isZoomed;

        if (magnifierBtnImage != null && plusSprite != null && minusSprite != null)
        {
            magnifierBtnImage.sprite = isZoomed ? minusSprite : plusSprite;
        }
    }

    //마우스 아래의 UI 요소를 뚫고 특정 스크립트(T) 탐색
    private T GetUIComponentAtMouse<T>(Vector2 mousePos) where T : Component
    {
        List<RaycastResult> raycastResults = GetUIElementsAtMouse(mousePos);

        foreach (RaycastResult hitResult in raycastResults)
        {
            T component = hitResult.gameObject.GetComponentInParent<T>();
            if (component != null) return component;
        }
        return null;
    }

   
    //마우스 포인터 아래의 아이템을 안전하게 찾는 로직
    private Item GetItemAtMouse(Vector2 mousePos, out GameObject rootObject)
    {
        rootObject = null;
        List<RaycastResult> raycastResults = GetUIElementsAtMouse(mousePos);

        foreach (RaycastResult hitResult in raycastResults)
        {
            //클릭된 오브젝트(hitResult) 자체의 이름에서 ID 추출 시도
            string currentId = hitResult.gameObject.name.Replace("(Clone)", "").Trim();
            Item foundData = itemManager.GetItemDataById(currentId);

            if (foundData != null)
            {
                rootObject = hitResult.gameObject;
                return foundData;
            }

            //만약 자식(아이콘 등)을 클릭했다면, 부모 오브젝트의 이름에서 ID 재확인
            if (hitResult.gameObject.transform.parent != null)
            {
                string parentId = hitResult.gameObject.transform.parent.name.Replace("(Clone)", "").Trim();
                foundData = itemManager.GetItemDataById(parentId);

                if (foundData != null)
                {
                    rootObject = hitResult.gameObject.transform.parent.gameObject;
                    return foundData;
                }
            }
        }

        return null;
    }

    //현재 마우스 위치를 관통하는 모든 UI 요소들을 수집
    private List<RaycastResult> GetUIElementsAtMouse(Vector2 mousePos)
    {
        // 1. 마우스 위치 정보를 담은 이벤트 데이터 생성
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current) { position = mousePos };
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        return raycastResults;
    }


    //도구 선택.
    public void SelectTool(int index)
    {
        //아이템을 잡고 이동 중일 때는 도구 전환 무시
        if (_isHoldingItem)
        {
            Debug.Log("<color=red>[경고]</color> 아이템을 들고 있는 중에는 도구를 변경할 수 없습니다!");
            return;
        }

        //서재에서는 다른 도구 전환 무시
        if (_isStudyMode && index != 0)
        {
            Debug.Log("<color=yellow>[알림]</color> 서재에서는 다른 도구를 사용할 수 없습니다.");
            return;
        }

        //정상적인 도구 변경 로직
        if (index < 0 || index >= toolBtns.Length || toolBtns[index] == null) return;

        _selectedToolIndex = index;
        ChangeCursorToButtonImage(index);

        Debug.Log($"<color=white><b>[Tool Switch]</b> {index + 1}번 도구로 변경되었습니다.</color>");
    }

    // 커서 이미지 반영
    public void ChangeCursorToButtonImage(int index)
    {
        switch (index)
        {
            case 0:
                UpdateHandCursorState(Mouse.current.position.ReadValue());
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

    //커서의 기능 상태 반영
    public CursorState GetCurrentToolCursorState()
    {
        switch (_selectedToolIndex)
        {
            case 0:
                // 클릭(드래그) 중이면 무조건 쥔 손
                if (Mouse.current.leftButton.isPressed) return CursorState.HandClosed;

                // 마우스 아래 아이템이 A타입이면 반 쥔 손
                Item hoverItem = GetItemAtMouse(Mouse.current.position.ReadValue(), out _);
                if (hoverItem != null && hoverItem.type == ItemType.A) return CursorState.HandHalf;

                // 그 외에는 펴진 손
                return CursorState.HandOpen;

            case 1: return CursorState.Spoid;
            case 2: return CursorState.Paint;
            case 3: return CursorState.Glasses;
            default: return CursorState.Normal;
        }
    }

    // 상태에 따른 손 모양 3종 이미지 반영
    private void UpdateHandCursorState(Vector2 mousePos)
    {
        // 클릭 중 -> 완전히 쥔 손
        if (Mouse.current.leftButton.isPressed)
        {
            CursorManager.Instance.ChangeCursor(CursorState.HandClosed);
            return;
        }

        // A타입 아이템 오버 -> 반 쥔 손
        Item hoverItem = GetItemAtMouse(mousePos, out _);
        if (hoverItem != null && hoverItem.type == ItemType.A)
        {
            CursorManager.Instance.ChangeCursor(CursorState.HandHalf);
        }
        // 평상시 -> 펴진 손
        else
        {
            CursorManager.Instance.ChangeCursor(CursorState.HandOpen);
        }
    }

    //
    private void HandleNumericInput()
    {
        if (Keyboard.current == null) return;

        // StudyMode면 숫자 단축키 자체를 먹통으로 만듦
        if (_isStudyMode) return;

        for (int toolIndex = 0; toolIndex < 5; toolIndex++)
            if (Keyboard.current[Key.Digit1 + toolIndex].wasPressedThisFrame) SelectTool(toolIndex);
    }

    //AnswerZone을 통한 아이템 정답 판별
    private bool IsThisItemForAnotherPage(Item itemData)
    {
        if (itemData == null) return false;
        AnswerZone[] allZones = Object.FindObjectsByType<AnswerZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var answerZone in allZones)
        {
            if (int.TryParse(itemData.id.Split('_')[0], out int itemIdInt))
            {
                if (answerZone.targetID == itemIdInt)
                {
                    if (!answerZone.gameObject.activeInHierarchy)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}