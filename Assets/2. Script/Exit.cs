using UnityEngine;

public class Exit : MonoBehaviour
{
    public void ExitGame()
    {
        print("게임 종료 로직 실행");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
    }
}
