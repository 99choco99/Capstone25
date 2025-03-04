using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnNPC : MonoBehaviour, IInteractable
{
    public void Interact(Transform player)
    {
        player.transform.position = new Vector3(335, 6, 305);
        SceneManager.LoadScene("Main");
    }
}
