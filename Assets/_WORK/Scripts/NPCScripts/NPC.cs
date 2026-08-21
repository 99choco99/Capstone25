using UniversalGraph;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Dialogue")]
    [Tooltip("기본 대화")]
    [SerializeField] private DialogueContainer defaultDialogue;

    [Header("Quest Graphs")]
    [Tooltip("관련 퀘스트")]
    public List<QuestContainer> questGraphs = new();

    private void Start()
    {
        anim = GetComponent<Animator>();
        NPCName = GetComponentInChildren<TextMeshProUGUI>();
        NPCName.text = transform.name;
    }

    public virtual void Interact(GameObject interactor)
    {
        StartCoroutine(LookAtPlayer(interactor.transform));

        if (defaultDialogue != null)
        {
            DialogueManager.Instance.StartConversation(
                defaultDialogue, gameObject, interactor);
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
