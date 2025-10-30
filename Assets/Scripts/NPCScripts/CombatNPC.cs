
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatNPC : NPC
{

    public override void Interact(Player player)
    {
        LoadingScene.LoadScene("Combat");
        transform.LookAt(player.transform);

    }

}
