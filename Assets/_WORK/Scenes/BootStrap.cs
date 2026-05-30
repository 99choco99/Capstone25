using UnityEngine;
using UnityEngine.SceneManagement;

public class BootStrap : MonoBehaviour
{
    void Start()
    {
        SceneManager.LoadScene(SceneName.Login);
    }

}
