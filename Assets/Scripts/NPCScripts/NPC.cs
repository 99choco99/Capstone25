using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPC : MonoBehaviour, IInteractable
{
    [SerializeField] TextMeshProUGUI NPCName;
    public PlayerController currentTalkingPlayer;
    protected Animator anim;
    public int id;

    private void Start()
    {
        anim = GetComponent<Animator>();
        NPCName = GetComponentInChildren<TextMeshProUGUI>();
        NPCName.text = transform.name;
    }

    public virtual void Interact(PlayerController player) {
        currentTalkingPlayer = player;
        StartCoroutine(RotationLerp(player.transform));
    }


    //NPC가 player를 바라봄
    IEnumerator RotationLerp(Transform player)
    {
        Vector3 dir = player.position - transform.position;     // NPC가 바라볼 방향
        Quaternion Targetrot = Quaternion.LookRotation(dir);   // NPC가 바라볼 회전
        Quaternion InverseTargetrot = Quaternion.LookRotation(-dir); //플레이어가 바라볼 회전
        float ElapsedTime = 0f; // 경과된 시간
        float rotationDuration = 0.5f; // 회전 시간
        float rotationSpeed = 0.5f;  // 회전 속도

        while (ElapsedTime < rotationDuration)
        {
            float time = ElapsedTime / rotationDuration;
            transform.rotation = Quaternion.Slerp(transform.rotation, Targetrot, time);
            player.rotation = Quaternion.Slerp(player.rotation, InverseTargetrot,time);
            ElapsedTime += Time.deltaTime * rotationSpeed;
            yield return null;
        }
        transform.rotation = Targetrot;
    }
}
