using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class QuestStateList : MonoBehaviour
{
    Dictionary<string, int> activeQuests;
    List<string> completedQuests;
    List<string> unlockedQuests;
}
