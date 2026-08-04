using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Container", menuName = "Dialogue/Dialogue Container")]
public class DialogueContainer : ScriptableObject
{
    public List<NodeLinkData> NodeLinks = new();
    public List<DialogueNodeData> DialogueNodeData = new();
}
