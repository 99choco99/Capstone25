using System.Collections;
using TMPro;
using UnityEngine;


public class NPC : MonoBehaviour, IInteractable
{
    [SerializeField] TextMeshProUGUI NPCName;
    protected Animator anim;

    [Header("Data Identifiers")]
    public int id;
    public string NPC_Name;
    public string InteractionPrompt => NPCName.text;
    public string DefaultDialogueKey => $"NPC_{id}_DEFAULT";


    private void Start()
    {
        anim = GetComponent<Animator>();
        NPCName = GetComponentInChildren<TextMeshProUGUI>();
        NPCName.text = transform.name;
    }

    public virtual void Interact(GameObject interactor) {
        StartCoroutine(LookAtPlayer(interactor.transform));

        if (interactor.TryGetComponent<Player>(out Player player))
        {
            var contexts = player.Quest.GetCurrentQuestContextForNPC(id);

            if (contexts.Count == 0)
            {
                // 관련된 퀘스트가 하나도 없을 때
                DialogueManager.Instance.StartConversation(DefaultDialogueKey, null);
            }
            else if (contexts.Count == 1)
            {
                // 딱 1개일 때는 선택지 없이 바로 재생
                DialogueManager.Instance.StartConversation(contexts[0].dialogueKey, contexts[0].onSubmitAction);
            }
            else
            {
                DialogueLine temp = new();
                temp.choices = new();

                //퀘스트가 2개 이상일 때
                if (DialogueManager.Instance.dialogueRegistry.TryGetValue(DefaultDialogueKey, out var defaultLines)
                    && defaultLines.Count > 0)
                {
                    DialogueLine baseLine = defaultLines[defaultLines.Count - 1];

                    temp.speakerName = baseLine.speakerName;
                    temp.sentence = baseLine.sentence;
                    temp.eventKey = baseLine.eventKey;
                }
                else
                {
                    temp.speakerName = NPC_Name;
                    temp.sentence = "어떤 일로 오셨습니까?";
                }


                foreach (var context in contexts)
                {
                    DialogueChoice choice = new DialogueChoice();
                    choice.choiceText = context.prefix + context.questTitle;
                    choice.nextDialogueKey = context.dialogueKey;

                    choice.onChoiceSelected_Runtime += () =>
                    {
                        DialogueManager.Instance.StartConversation(context.dialogueKey, context.onSubmitAction);
                    };

                    temp.choices.Add(choice);
                }
                DialogueManager.Instance.StartConversation(temp);

            }
        }

    }


    //NPC가 player를 바라봄
    IEnumerator LookAtPlayer(Transform target)
    {
        Vector3 dir = target.position - transform.position;     // NPC가 바라볼 방향
        dir.y = 0;
        Quaternion Targetrot = Quaternion.LookRotation(dir);   // NPC가 바라볼 회전
        float ElapsedTime = 0f; // 경과된 시간
        float rotationDuration = 0.5f; // 회전 시간
        float rotationSpeed = 0.5f;  // 회전 속도

        while (ElapsedTime < rotationDuration)
        {
            float time = ElapsedTime / rotationDuration;
            transform.rotation = Quaternion.Slerp(transform.rotation, Targetrot, time);
            ElapsedTime += Time.deltaTime * rotationSpeed;
            yield return null;
        }
        transform.rotation = Targetrot;
    }
}
