using UnityEngine;

public class TestNarration : MonoBehaviour
{
    public int Chapter;
    public string Type;
    public int Stage;


    private void Start()
    {
        FindAnyObjectByType<NarrationManager>().StartNarration(Chapter, Type, Stage);
    }
}
