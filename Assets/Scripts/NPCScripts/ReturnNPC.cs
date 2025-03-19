using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnNPC : MonoBehaviour, IInteractable
{
    public void Interact(Transform player)
    {
        LoadingScene.LoadScene("Main");
        player.transform.position = new Vector3(335, 6, 305);
    }
}
