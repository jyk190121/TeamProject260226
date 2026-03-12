using System.Collections;
using UnityEngine;

public class TestNarration : MonoBehaviour
{
    public int Chapter;
    public string Type;
    public int Stage;


    private IEnumerator Start()
    {
        NarrationManager manager = FindAnyObjectByType<NarrationManager>();

        while (manager == null || !manager.isLoaded)
        {
            yield return null; // 데이터가 올 때까지 한 프레임씩 쉽니다.
        }

        manager.StartNarration(Chapter, Type, Stage);
    }
}
