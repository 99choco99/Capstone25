using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPC_Name : MonoBehaviour
{
    TextMeshProUGUI NPCName;
    void Awake()
    {
        NPCName = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        NPCName.text = transform.parent.parent.name;
    }
}
