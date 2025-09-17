using TMPro;
using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI speakerText;

    private void Start()
    {
        DialogueManager.instance.OnConversationStart += OpenPanel;
        DialogueManager.instance.OnConversationEnd += ClosePanel;
        DialogueManager.instance.OnShowLine += SetDialogue;

        dialoguePanel.SetActive(false);
    }
    private void OnDestroy()
    {
        if (DialogueManager.instance != null)
        {
            DialogueManager.instance.OnConversationStart -= OpenPanel;
            DialogueManager.instance.OnConversationEnd -= ClosePanel;
            DialogueManager.instance.OnShowLine -= SetDialogue;
        }
    }

    private void OpenPanel()
    {
        dialoguePanel.SetActive(true);
    }

    // [추가] 패널을 닫는 함수
    private void ClosePanel()
    {
        dialoguePanel.SetActive(false);
    }

    private void SetDialogue(DialogueLine line)
    {
        speakerText.text = line.speakerName;
        dialogueText.text = line.sentence;
    }

}
