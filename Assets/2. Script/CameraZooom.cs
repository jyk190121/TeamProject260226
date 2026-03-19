using Unity.Cinemachine; // 그대로 사용
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CameraZoom : MonoBehaviour
{
    public static CameraZoom Instance { get; private set; }

    [Header("Cinemachine 3.0+")]
    // [수정] CinemachineVirtualCamera 대신 CinemachineCamera를 사용합니다.
    public CinemachineCamera mainCam;
    public CinemachineCamera zoomCam;

    [Header("Fade UI")]
    public Image whiteOverlay;
    public GameObject Canvas;
    public float zoomDuration = 1.5f;
    public float fadeDuration = 1.0f;

    void Awake()
    {
        Instance = this;
        if (whiteOverlay != null)
        {
            Color c = whiteOverlay.color;
            c.a = 0f;
            whiteOverlay.color = c;
            whiteOverlay.gameObject.SetActive(false);
        }
    }

    public IEnumerator PlayStartSequence()
    {
        Canvas.gameObject.SetActive(false);
        if (mainCam != null) mainCam.Priority = 0;
        zoomCam.Priority = 20;

        // 줌인이 완료될 때까지 대기
        yield return new WaitForSeconds(zoomDuration);

        whiteOverlay.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            Color c = whiteOverlay.color;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            whiteOverlay.color = c;
            yield return null;
        }

        //yield return new WaitForSeconds(0.5f);
    }
}