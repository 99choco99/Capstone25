using TMPro;
using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    DialogueManager DialogueManager;


    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI speakerText;

    private void Awake()
    {
        DialogueManager = GetComponent<DialogueManager>();
        if (DialogueManager != null)
        {
            Debug.Log("DialogueManager ¾øÀ½");
        }
    }


    private void Start()
    {
        DialogueManager.OnConversationStart += OpenPanel;
        DialogueManager.OnConversationEnd += ClosePanel;
        DialogueManager.OnShowLine += SetDialogue;

        dialoguePanel.SetActive(false);
    }
    private void OnDestroy()
    {
        if (DialogueManager != null)
        {
            DialogueManager.OnConversationStart -= OpenPanel;
            DialogueManager.OnConversationEnd -= ClosePanel;
            DialogueManager.OnShowLine -= SetDialogue;
        }
    }

    private void OpenPanel()
    {
        dialoguePanel.SetActive(true);
    }

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
