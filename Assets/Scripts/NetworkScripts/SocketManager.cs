using Newtonsoft.Json;
using SocketIOClient;
using System;
using UnityEngine;
using static Player;

public class SocketManager : MonoBehaviour
{
    public PlayerDataClass getData;  // 받은 데이터


    [Header("SocketIO Setting")]
    public static SocketManager Instance { get; private set; }
    private SocketIOUnity socket;
    private string serverUrl = "http://localhost:3000";


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSocket();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    async void InitializeSocket()
    {
        try
        {
            var uri = new Uri(serverUrl);
            socket = new SocketIOUnity(uri, new SocketIOOptions()
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("Socket Connection error: " + ex.Message);
        }
        await socket.ConnectAsync();
    }

    public SocketIOUnity GetSocket()
    {
        return socket;
    }

}
