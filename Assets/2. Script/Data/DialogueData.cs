using UnityEngine;

[System.Serializable]
public class DialogueData
{
    public string DialogueId;       // dialogueId
    public string DialogueGroupId;  // dialogueGroupId
    public string DialogueType;     // dialogueType
    public string NpcState;         // npcState
    public int Stage;               // Stage
    public int Sequence;            // seq
    public string Text;             // text
    public float AutoAdvanceSec;    // autoAdvanceSec
    public string NextDialogueId;   // nextDialogueId
    public string EventKey;         // eventKey
    public string Remark;           // remark
}