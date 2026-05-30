using UnityEngine;

public class MainCamera : MonoBehaviour
{
    public static MainCamera Instance;

    private void Start()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }

    }
}
