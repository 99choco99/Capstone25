using UniversalGraph;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : UIBase
{
    [Header("대화문")]
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;


    [Header("선택지")]
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Transform choiceButtonContainer;

    private List<Button> choiceButtons = new();

    public override void Init()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnShowLine += SetDialogue;
            DialogueManager.Instance.OnShowChoices += ShowChoices;
        }
        else
        {
            Debug.LogError("[DialougeManager 없음] ");
        }

        HideAllChoices();
        gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnShowLine -= SetDialogue;
            DialogueManager.Instance.OnShowChoices -= ShowChoices;
        }
    }


    //대화문 설정
    private void SetDialogue(DialogueNodeData line)
    {
        speakerText.text = line.SpeakerName;
        dialogueText.text = line.DialogueText;
        HideAllChoices();
    }

    //선택지 띄우기
    private void ShowChoices(List<DialogueChoiceData> choices)
    {
        HideAllChoices();

        for(int i = 0; i< choices.Count; i++)
        {
            Button btn;
            if (choiceButtons.Count > i)
            {
                btn = choiceButtons[i];
            }
            else
            {
                GameObject choiceObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                btn = choiceObj.GetComponent<Button>();
                choiceButtons.Add(btn);
            }

            btn.gameObject.SetActive(true);

            var tmpText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null) tmpText.text = choices[i].ChoiceText;

            DialogueChoiceData currentChoice = choices[i];
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                HideAllChoices();
                DialogueManager.Instance.OnSelectionChoice(currentChoice);
            });
        }

        for (int i = choices.Count; i <choiceButtons.Count; i++)
        {
            choiceButtons[i].gameObject.SetActive(false);
        }
    }

    //선택지 숨기기
    private void HideAllChoices()
    {
        foreach (Button btn in choiceButtons)
        {
            btn.gameObject.SetActive(false);
        }
    }
}
