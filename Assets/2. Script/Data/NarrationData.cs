using UnityEngine;

[System.Serializable]
public class NarrationData
{
    public string DGRP;         // Dialogue Group 
    public string DialogueType; // Main, Sub, NARRATION 등
    public string NpcState;     // STANDING, SURPRISED 등 
    public int Stage;           // 스테이지 묶음 
    public int Sequence;        // 재생 순서 
    public string Text;         // 대사 본문
    public float Delay;         // 대기 시간 
}
