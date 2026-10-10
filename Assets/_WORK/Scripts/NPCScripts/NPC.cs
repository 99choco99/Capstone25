using System.Collections;
using TMPro;
using UnityEngine;
using UniversalGraph;

public class NPC : MonoBehaviour, IInteractable
{
    [SerializeField] private QuestContainer questContainer;
    [SerializeField] private DialogueEntryPoint acceptEntryPoint;
    [SerializeField] private DialogueEntryPoint InprogressEntryPoint;
    [SerializeField] private DialogueEntryPoint reportEntryPoint;
    [SerializeField] private int reportTargetId = 1;
    [SerializeField, Min(0.01f)] private float rotationDuration = 1f; // 회전 시간

    [SerializeField] TextMeshProUGUI NPCName;

    public string InteractionPrompt => NPCName.text;

    private void Start()
    {
        NPCName = GetComponentInChildren<TextMeshProUGUI>();
        NPCName.text = transform.name;
    }

    public virtual void Interact(GameObject interactor)
    {
        if (DialogueManager.Instance.IsConversationActive || questContainer == null) return;

        GameManager.Instance.QuestController.QuestProgress.TryGetValue(questContainer.QuestId, out QuestProgress progress);
        QuestState state = progress?.state ?? QuestState.NotStarted;
        DialogueEntryPoint entryPoint;

        switch (state)
        {
            case QuestState.NotStarted:
                entryPoint = acceptEntryPoint;
                break;
            case QuestState.InProgress:
                entryPoint = InprogressEntryPoint;
                break;
            case QuestState.CanComplete:
                QuestManager.ProcessObjectivesByEvent(GameManager.Instance.QuestController, QuestEventKeys.Report, reportTargetId, 1);
                entryPoint = reportEntryPoint;
                break;
            default:
                return;
        }

        if (entryPoint.Container != null
            && DialogueManager.Instance.StartConversation(entryPoint, new DialogueExecutionContext(gameObject, interactor, GameManager.Instance.QuestController)))
        {
            StartCoroutine(LookAtPlayer(interactor.transform));
        }
    }

    //NPC가 player를 바라봄
    IEnumerator LookAtPlayer(Transform target)
    {
        Vector3 dir = target.position - transform.position;     // NPC가 바라볼 방향
        dir.y = 0;
        Quaternion Targetrot = Quaternion.LookRotation(dir);   // NPC가 바라볼 회전
        float ElapsedTime = 0f; // 경과된 시간

        while (ElapsedTime < rotationDuration)
        {
            float time = ElapsedTime / rotationDuration;  // 회전 속도
            transform.rotation = Quaternion.Slerp(transform.rotation, Targetrot, time);
            ElapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.rotation = Targetrot;
    }
}
