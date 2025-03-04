using UnityEngine;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;

    public PlayerController player;


    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        Destroy(gameObject);
    }


    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                return null;
            }
            return instance;
        }
    }
}
