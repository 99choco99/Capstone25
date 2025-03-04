using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatNPC : MonoBehaviour, IInteractable
{
    public void Interact(Transform player)
    {
        player.transform.position = new Vector3(0, 2, -14);
        SceneManager.LoadScene("Combat");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
