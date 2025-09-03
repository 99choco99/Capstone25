using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnNPC : NPC
{
    public override void Interact(PlayerController player)
    {
        LoadingScene.LoadScene("Main");
        player.transform.position = new Vector3(335, 6, 305);
    }
}
