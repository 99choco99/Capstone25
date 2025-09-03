using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] DialogueManager dialogueManager;
    IInteractable interactObject;

    private readonly int layerMask = 1 << 8;
    public Collider select;
    Collider[] hits;
    public int selectIndex = 0;
    public int preselectIndex = 0;

    bool isWorking;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        dialogueManager = GetComponentInChildren<DialogueManager>();
    }

    void Update()
    {
        hits = GetInteractObject();
        if (hits.Length > 0)
        {
            SelectObject(hits);
            if (!isWorking && player.interaction)
            {
                isWorking = true;
                if (select.gameObject.TryGetComponent<QuestNPC>(out QuestNPC npc))
                {
                    // NPC라면 대화상태 진입
                    dialogueManager.StartConversation(npc);
                }
                if (select.gameObject.TryGetComponent(out interactObject))
                {
                    interactObject.Interact(player);  //상호작용
                }

                StartCoroutine("WaitTime", 0.5f);
            }
        }
    }

    public Collider[] GetInteractObject()
    {
        hits = Physics.OverlapSphere(transform.position, player.interactRange,layerMask);
        if (hits != null)
        {
            return hits;
        }
        return null;
    }

    void SelectObject(Collider[] hits)
    {
        select = hits[0];
        //이전 Index 저장
        if (preselectIndex != selectIndex)
        {
            preselectIndex = selectIndex;
        }

        //마우스 휠로 선택할 InteractObject 정하기
        if (selectIndex < hits.Length - 1 && player.scroll > 0)
        {
            selectIndex += 1;
        }
        else if (selectIndex > 0 && player.scroll < 0)
        {
            selectIndex -= 1;
        }
        if (selectIndex > hits.Length - 1 || selectIndex < 0) { selectIndex = 0; }
        select = hits[selectIndex];
    }


    IEnumerator WaitTime(float time)
    {
        yield return new WaitForSeconds(time);
        isWorking = false;
    }
}
