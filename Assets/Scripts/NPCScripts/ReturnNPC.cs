using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnNPC : NPC
{
    public override void Interact(Transform player)
    {
        LoadingScene.LoadScene("Main");
        player.transform.position = new Vector3(335, 6, 305);
    }
}
