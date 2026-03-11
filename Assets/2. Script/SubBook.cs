using UnityEngine;
using UnityEngine.UI;
using System;

public class SubBook : MonoBehaviour
{
    public Button myButton;
    public static Action OnSubBookClicked; // 전역 이벤트

    void Start()
    {
        if (myButton == null) myButton = GetComponent<Button>();
        myButton.onClick.AddListener(() => OnSubBookClicked?.Invoke());
    }
}