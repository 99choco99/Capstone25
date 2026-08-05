
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class DialogueNode : Node
{
    public string GUID;
    public string DialogueText;
    public bool EntryPoint = false;

    public Vector2 position;

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        position = newPos.position;

    }
}
