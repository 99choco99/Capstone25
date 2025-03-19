
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatNPC : MonoBehaviour, IInteractable
{
    public void Interact(Transform player)
    {
        LoadingScene.LoadScene("Combat");
        transform.LookAt(player);
        player.transform.position = new Vector3(0, 2, -14);
    }

}
