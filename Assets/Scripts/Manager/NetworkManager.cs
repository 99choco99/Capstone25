using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;

    [HideInInspector] public APIManager API;
    [HideInInspector] public SocketManager socket;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            API = GetComponent<APIManager>();
            socket = GetComponent<SocketManager>();
        } else
        {
            Destroy(gameObject);
        }
    }
}
