using UnityEngine;

public class ClickObjectNameTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(mouseRay, out RaycastHit hitInfo))
            {
                GameObject clickedObject = hitInfo.collider.gameObject;

                Debug.Log("클릭한 오브젝트 이름 : " + clickedObject.name);
            }
            else
            {
                Debug.Log("아무것도 클릭되지 않음");
            }
        }
    }
}