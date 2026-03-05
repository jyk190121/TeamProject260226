using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CustomInput = Input;
using Key = UnityEngine.InputSystem.Key;

public class ToolBarController : MonoBehaviour
{
    [Header("설정")]
    // 커서의 중심점 설정 (기본은 왼쪽 상단 0,0)
    public Vector2 hotSpot = Vector2.zero;
    public LayerMask storageLayer;
    public LayerMask itemLayer;    // Inspector에서 "Item" 레이어를 가진 프리팹 선택

    [Header("도구 버튼")]
    public Button[] toolBtns = new Button[5];

    [Header("프리팹 설정")]
    public GameObject itemPrefab;

    GameObject _currentMovingItem;
    bool _isHoldingItem = false;
    int _selectedToolIndex = -1; // 현재 어떤 도구를 선택했는지 저장

    void Start()
    {
        for (int i = 0; i < toolBtns.Length; i++)
        {
            int index = i;
            Button btn = toolBtns[i];
            if (btn == null) continue;

            btn.onClick.AddListener(() =>
            {
                _selectedToolIndex = index;
                ChangeCursorToButtonImage(btn);

                // 도구를 바꿀 때 이미 들고 있던 프리팹이 있다면 제거 (취소 처리)
                if (_isHoldingItem && _currentMovingItem != null)
                {
                    // 새로 생성한 아이템인 경우에만 Destroy, 
                    // 기존 아이템을 집은 거라면 원래 위치로 돌리는 로직이 필요할 수 있음
                    Destroy(_currentMovingItem);
                    _isHoldingItem = false;
                }
            });
        }
    }
    void Update()
    {
        // 1. 아이템을 들고 있는 상태 (이동 및 배치)
        if (_isHoldingItem && _currentMovingItem != null)
        {
            MoveItemWithMouse();

            if (CustomInput.GetMouseButtonDown(0))
            {
                TryPlaceItem();
            }
        }
        // 2. 아이템을 안 들고 있는 상태 + 0번 도구(Hand) 활성화 + 마우스 클릭 (아이템 선택)
        else if (!_isHoldingItem && _selectedToolIndex == 0 && CustomInput.GetMouseButtonDown(0))
        {
            // UI를 클릭 중일 때는 월드 레이캐스트를 무시 (버튼 누를 때 바닥 찍히는 것 방지)
            if (EventSystem.current.IsPointerOverGameObject()) return;

            TryPickUpItem();
        }
    }

    // 기존 필드에 있는 아이템을 집어 올리는 로직
    private void TryPickUpItem()
    {
        Ray ray = Camera.main.ScreenPointToRay(CustomInput.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, itemLayer))
        {
            _currentMovingItem = hit.collider.gameObject;

            // 이동 모드 활성화를 위해 레이어 변경
            SetLayerRecursive(_currentMovingItem, LayerMask.NameToLayer("Ignore Raycast"));
            _isHoldingItem = true;
            Debug.Log("아이템을 집었습니다: " + _currentMovingItem.name);
        }
    }
    private void MoveItemWithMouse()
    {
        Vector2 mPos = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(mPos);

        // Storage 레이어 위에서만 좌표를 계산 (아이템이 공중에 뜨지 않게)
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, storageLayer))
        {
            _currentMovingItem.transform.position = hit.point;
        }
        else
        {
            // Storage 영역 밖일 때의 처리 (필요시)
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mPos.x, mPos.y, 10f));
            _currentMovingItem.transform.position = worldPos;
        }
    }

    private void TryPlaceItem()
    {

        // 커스텀 Input의 GetMouseButtonDown(0) 사용
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, storageLayer))
            {
                // 배치 로직 (전과 동일)
                SetLayerRecursive(_currentMovingItem, LayerMask.NameToLayer("Item"));
                _currentMovingItem = null;
                _isHoldingItem = false;
                ResetCursor();
            }
        }

        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //RaycastHit hit;

        //// 1. 바닥(Storage)이 있는지 확인
        //if (Physics.Raycast(ray, out hit, 100f, storageLayer))
        //{
        //    // 2. (선택 사항) 해당 위치에 이미 다른 'Item' 레이어의 오브젝트가 있는지 체크
        //    // OverlapSphere 등을 사용하여 겹침 방지 로직을 추가할 수 있습니다.

        //    Debug.Log("아이템 배치 완료");
        //    _currentMovingItem.transform.position = hit.point;

        //    // 배치가 끝났으므로 레이어를 "Item"으로 변경
        //    SetLayerRecursive(_currentMovingItem, LayerMask.NameToLayer("Item"));

        //    _currentMovingItem = null;
        //    _isHoldingItem = false;
        //    ResetCursor();
        //}
    }

    // 자식 오브젝트까지 포함하여 레이어를 변경하는 헬퍼 함수
    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    public void ChangeCursorToButtonImage(Button clickedButton)
    {
        Image btnImage = clickedButton.GetComponent<Image>();

        if (btnImage != null && btnImage.sprite != null)
        {
            Texture2D texture = btnImage.sprite.texture;

            // 에러 방지를 위한 추가 체크 (디버깅용)
            if (!texture.isReadable)
            {
                Debug.LogError($"{texture.name} 이미지의 'Read/Write' 설정이 꺼져 있습니다! Inspector에서 체크해 주세요.");
                return;
            }

            Cursor.SetCursor(texture, hotSpot, CursorMode.Auto);
        }
    }

    // 필요 시 커서를 다시 기본으로 되돌리는 함수
    public void ResetCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}