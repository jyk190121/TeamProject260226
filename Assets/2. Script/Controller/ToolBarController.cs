using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Key = UnityEngine.InputSystem.Key;

public class ToolBarController : MonoBehaviour
{
    [Header("매니저 연결")]
    public ItemManager itemManager; // 인스펙터에서 ItemManager 오브젝트를 연결해주세요.

    [Header("설정")]
    public Vector2 hotSpot = Vector2.zero;

    [Header("UI 레이어 이름 매칭")]
    public string itemLayerName = "Item";
    public string storageLayerName = "Storage"; // 또는 ItemStorage 등 인스펙터에 맞게 입력

    [Header("도구 버튼")]
    public Button[] toolBtns = new Button[5];

    [Header("프리팹 설정")]
    public GameObject itemPrefab;

    GameObject _currentMovingItem;
    bool _isHoldingItem = false;
    int _selectedToolIndex = -1;

    void Start()
    {
        for (int i = 0; i < toolBtns.Length; i++)
        {
            int index = i;
            Button btn = toolBtns[i];
            if (btn == null) continue;

            btn.onClick.AddListener(() => SelectTool(index));
        }
    }

    void Update()
    {
        HandleNumericInput();

        if (Mouse.current == null) return;

        bool isLeftClick = Mouse.current.leftButton.wasPressedThisFrame;
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // 1. 아이템을 들고 있는 상태
        if (_isHoldingItem && _currentMovingItem != null)
        {
            MoveItemWithMouse(mousePos);

            if (isLeftClick)
            {
                TryPlaceItem(mousePos);
            }
        }
        // 2. 0번 도구(Hand) + 빈손 + 마우스 클릭
        else if (!_isHoldingItem && _selectedToolIndex == 0 && isLeftClick)
        {
            TryPickUpItem(mousePos);
        }
    }

    // ---------------------------------------------------
    // UI 전용 클릭 감지 및 집기 로직
    // ---------------------------------------------------
    private void TryPickUpItem(Vector2 mousePos)
    {
        List<RaycastResult> results = GetUIElementsAtMouse(mousePos);

        foreach (var result in results)
        {
            // 클릭한 UI들 중 "Item" 레이어를 가진 녀석을 찾음
            if (LayerMask.LayerToName(result.gameObject.layer) == itemLayerName)
            {
                _currentMovingItem = result.gameObject;
                _isHoldingItem = true;

                // 마우스를 가리지 않도록 Raycast Target 끄기
                SetUIRaycastTarget(_currentMovingItem, false);

                Debug.Log($"<color=lime><b>[Action]</b> Canvas 아이템 집기: {_currentMovingItem.name}</color>");

                // [핵심] 사전 작업: 클릭한 아이템의 SO 데이터를 읽어옵니다.
                ReadAndPrintItemSO(_currentMovingItem);

                return; // 하나 집었으면 종료
            }
        }
    }

    // ---------------------------------------------------
    // 클릭한 아이템의 SO 데이터를 읽어오는 함수 (사전 작업)
    // ---------------------------------------------------
    private void ReadAndPrintItemSO(GameObject targetObj)
    {
        if (itemManager == null)
        {
            // 만약 인스펙터 연결을 깜빡했다면 싱글톤으로 대체 접근 시도
            if (ItemManager.Instance != null) itemManager = ItemManager.Instance;
            else
            {
                Debug.LogError("<color=red>[Error]</color> ToolBarController에 ItemManager가 연결되지 않았습니다!");
                return;
            }
        }

        // 유니티에서 생성된 프리팹의 "(Clone)" 문자열을 제거하여 순수 ID 추출
        string searchId = targetObj.name.Replace("(Clone)", "").Trim();

        // ItemManager를 통해 SO 데이터 접근
        Item data = itemManager.GetItemDataById(searchId);

        if (data != null)
        {
            Debug.Log($"<color=yellow><b>=== [SO 데이터 읽기 성공] ===</b></color>\n" +
                      $" - ID: {data.id}\n" +
                      $" - Stage Index: {data.stageIndex}\n" +
                      $" - Type: {data.type}\n" +
                      $" - OriPos: {data.oriPos}\n" +
                      $" - ChangePos: {data.changePos}\n" +
                      $" - Color: {data.color}\n" +
                      $" - isGround: {data.isGround}\n" +
                      $"<color=yellow>===============================</color>");
        }
        else
        {
            Debug.Log($"<color=red>[SO 매핑 실패]</color> ItemManager에서 ID '{searchId}'를 찾을 수 없습니다.");
        }
    }

    // ---------------------------------------------------
    // 아이템 이동 및 배치
    // ---------------------------------------------------
    private void MoveItemWithMouse(Vector2 mousePos)
    {
        // Canvas (Screen Space - Overlay) 환경에서는 마우스 좌표가 곧 UI 좌표입니다.
        _currentMovingItem.transform.position = mousePos;
    }

    private void TryPlaceItem(Vector2 mousePos)
    {
        List<RaycastResult> results = GetUIElementsAtMouse(mousePos);
        bool canPlace = false;

        foreach (var result in results)
        {
            if (LayerMask.LayerToName(result.gameObject.layer) == storageLayerName)
            {
                canPlace = true;
                break;
            }
        }

        if (canPlace)
        {
            SetUIRaycastTarget(_currentMovingItem, true);

            // ItemManager에 위치 업데이트 요청 (필요시 활성화)
            // if (itemManager != null) itemManager.ChangeItemPos(_currentMovingItem.name.Replace("(Clone)", "").Trim(), _currentMovingItem.transform.position);

            _currentMovingItem = null;
            _isHoldingItem = false;
            ResetCursor();
            Debug.Log("<color=blue>[Action] Storage UI 영역에 배치 완료</color>");
        }
        else
        {
            Debug.Log("<color=red>[Fail]</color> Storage 영역이 아닙니다.");
        }
    }

    // ---------------------------------------------------
    // UI Raycast 공통 헬퍼 함수
    // ---------------------------------------------------
    private List<RaycastResult> GetUIElementsAtMouse(Vector2 mousePos)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = mousePos };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results;
    }

    private void SetUIRaycastTarget(GameObject obj, bool state)
    {
        Graphic[] graphics = obj.GetComponentsInChildren<Graphic>();
        foreach (Graphic g in graphics)
        {
            g.raycastTarget = state;
        }
    }

    // ---------------------------------------------------
    // 도구 및 커서 로직
    // ---------------------------------------------------
    private void HandleNumericInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current[Key.Digit1].wasPressedThisFrame) SelectTool(0);
        else if (Keyboard.current[Key.Digit2].wasPressedThisFrame) SelectTool(1);
        else if (Keyboard.current[Key.Digit3].wasPressedThisFrame) SelectTool(2);
        else if (Keyboard.current[Key.Digit4].wasPressedThisFrame) SelectTool(3);
        else if (Keyboard.current[Key.Digit5].wasPressedThisFrame) SelectTool(4);
    }

    private void SelectTool(int index)
    {
        if (index < 0 || index >= toolBtns.Length || toolBtns[index] == null) return;
        _selectedToolIndex = index;
        ChangeCursorToButtonImage(toolBtns[index]);
        Debug.Log($"<color=white><b>[Tool]</b> {index + 1}번 도구 선택됨</color>");

        if (_isHoldingItem && _currentMovingItem != null)
        {
            Destroy(_currentMovingItem);
            _isHoldingItem = false;
            ResetCursor();
        }
    }

    public void ChangeCursorToButtonImage(Button clickedButton)
    {
        Image btnImage = clickedButton.GetComponent<Image>();
        if (btnImage != null && btnImage.sprite != null)
        {
            Texture2D texture = btnImage.sprite.texture;
            if (texture.isReadable) Cursor.SetCursor(texture, hotSpot, CursorMode.Auto);
            else Debug.LogError($"{texture.name} 이미지의 'Read/Write' 설정이 꺼져 있습니다!");
        }
    }

    public void ResetCursor() { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); }
}