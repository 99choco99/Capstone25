using Firesplash.GameDevAssets.SocketIO; // Asset의 네임스페이스 사용
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static PublicAPIManager;

public class SocketManager : MonoBehaviour
{
    public static SocketManager instance;
    public GameObject playerPrefab;

    // SocketIOCommunicator 컴포넌트를 담을 변수
    private SocketIOCommunicator sioCom;
    private string userId;
    private NetworkPlayerData myInitialData = null;


    // 서버에 접속된 플레이어들을 관리
    private Dictionary<string, GameObject> networkPlayers = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {

        sioCom = GetComponent<SocketIOCommunicator>();

        // ========== 서버로부터 오는 이벤트 리스너 설정 ==========

        // 서버에 성공적으로 연결되었을 때
        sioCom.Instance.On("connect", (string payload) =>
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (!string.IsNullOrEmpty(userId))
            {
                sioCom.Instance.Emit("initialize", userId,true);
            }


        });

        sioCom.Instance.On("initializeComplete", (payload) =>
        {
            myInitialData = JsonConvert.DeserializeObject<NetworkPlayerData>(payload);
            LoadingScene.LoadScene("Main");
        });

        // 다른 플레이어들의 현재 목록을 받았을 때
        sioCom.Instance.On("currentPlayers", (string payload) =>
        {
            Debug.Log("Received current players: " + payload);

            // 받은 JSON 문자열을 PlayerDictionary 클래스로 파싱
            var playerList = JsonConvert.DeserializeObject<NetworkPlayerList>(payload);

            foreach (var playerData in playerList.players)
            {
                if (playerData.id != this.userId && !networkPlayers.ContainsKey(playerData.id))
                {
                    SpawnPlayer(playerData, false);
                }
            }
        });

        // 새로운 플레이어가 접속했을 때
        sioCom.Instance.On("newPlayer", (string payload) =>
        {
            NetworkPlayerData playerData = JsonConvert.DeserializeObject<NetworkPlayerData>(payload);

            SpawnPlayer(playerData,false);
        });


        //다른 플레이어의 움직임을 업데이트
        sioCom.Instance.On("updatePlayerMovement", (string payload) =>
        {
            var data = JsonConvert.DeserializeObject<NetworkPlayerData>(payload);
            if (networkPlayers.ContainsKey(data.id))
            {
                var playerObject = networkPlayers[data.id];
                if (playerObject.TryGetComponent<NetworkPlayer>(out var networkPlayer))
                {
                    networkPlayer.UpdatePosition(new Vector3(data.position.x,data.position.y,data.position.z));
                    networkPlayer.UpdateRotation(new Quaternion(data.rotation.x, data.rotation.y, data.rotation.z, data.rotation.w));
                }
            }

        });


        //이동 애니메이션 동기화
        sioCom.Instance.On("updatePlayerAnimation", (string payload) =>
        {
            var data = JsonConvert.DeserializeObject<NetworkAnimationData>(payload);

            if (networkPlayers.ContainsKey(data.id))
            {
                var playerObject = networkPlayers[data.id];
                if (playerObject.TryGetComponent<NetworkPlayer>(out var networkPlayer))
                {
                    networkPlayer.UpdateMoveAnimation(data.horizontal,data.vertical);
                }
            }
        });

        //공격 애니메이션 업데이트
        sioCom.Instance.On("updateAttack", (string payload) =>
        {
            var data = JsonConvert.DeserializeObject<NetworkAttackData>(payload);
            if (networkPlayers.ContainsKey(data.id))
            {
                Animator anim = networkPlayers[data.id].GetComponent<Animator>();
                if (anim != null) anim.SetTrigger("attack");
            }
        });


        // 다른 플레이어의 접속이 끊겼을 때 서버가 보내주는 커스텀 이벤트
        sioCom.Instance.On("playerDisconnected", (string payload) =>
        {

            string id = payload.Replace("\"", "");
            Debug.Log($"Another player has disconnected: {id}");

            if (networkPlayers.ContainsKey(id))
            {
                Destroy(networkPlayers[id]);
                networkPlayers.Remove(id);
            }
        });


        // 플레이어가 연결을 끊었을 때
        sioCom.Instance.On("disconnect", (string payload) =>
        {
            // 이 Asset은 payload에 따옴표가 포함될 수 있어 제거해줍니다.
            string id = payload.Replace("\"", "");
            Debug.Log($"Player disconnected: {id}");

            if (networkPlayers.ContainsKey(id))
            {
                Destroy(networkPlayers[id]);
                networkPlayers.Remove(id);
            }
        });
    }



    void SpawnPlayer(NetworkPlayerData data, bool isLocal)
    {
        GameObject player = Instantiate(playerPrefab, data.position.ToVector3(),data.rotation.ToQuaternion());

        if (isLocal)
        {
            if (player.TryGetComponent<Player>(out var playerComponent))
            {
                playerComponent.IsLocalPlayer = true;
                playerComponent.InputHandler.enabled = true;
                playerComponent.StateMachine.enabled = true;
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.Register(playerComponent);
                }
                Name name = playerComponent.GetComponentInChildren<Name>();
                name.gameObject.SetActive(false);
            }

        }
        else
        {
            player.GetComponent<PlayerInput>().enabled = false;

            networkPlayers.Add(data.id, player);
        }
    }




    // ========== 서버로 데이터를 보내는 함수들 ==========


    public void EmitPlayerMovement(Vector3 position, Quaternion rotation)
    {
        var json = new
        {
            position = new { x = position.x, y = position.y, z = position.z },
            rotation = new { x = rotation.x, y = rotation.y, z = rotation.z, w = rotation.w } // w 추가
        };
        var data = JsonConvert.SerializeObject(json);

        sioCom.Instance.Emit("playerMovement",data,false);

    }

    public void EmitPlayerMoveAnimation(float vertical, float horizontal)
    {
        NetworkAnimationData data = new();
        data.id = userId;
        data.vertical = vertical;
        data.horizontal = horizontal;
        var json = JsonConvert.SerializeObject(data);

        sioCom.Instance.Emit("playerAnimation", json, false);
    }

    public void EmitPlayerAttack()
    {
        sioCom.Instance.Emit("playerAttack");
    }


    public void ConnectToServer(string userId)
    {

        if(userId == null) { Debug.LogError("유저 아이디 에러"); return; }

        this.userId = userId;
        // 이미 연결 중이거나 연결된 상태가 아닐 때만 연결을 시도
        if (!sioCom.Instance.IsConnected())
        {
            Debug.Log("Connecting to server via button press...");
            sioCom.Instance.Connect();
        }
    }


    // [추가] 씬 로딩 완료 시 호출될 함수
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name != "Loading")
        {
            SpawnPlayer(myInitialData, true);
            sioCom.Instance.Emit("LoadSceneComplete");
        }

    }


    // OnDestroy는 Unity 오브젝트가 파괴될 때 호출됨
    private void OnApplicationQuit()
    {
        if (sioCom != null && sioCom.Instance.IsConnected())
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            sioCom.Instance.Close();
        }
    }
}



public class NetworkPlayerData
{
    public string id;
    public NetworKPosition position;
    public NetworkRotation rotation;
}

public class NetworKPosition
{
    public float x, y, z;
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

public class NetworkRotation
{
    public float x, y, z, w;
    public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
}

public class NetworkPlayerList
{
    public List<NetworkPlayerData> players;
}

public class NetworkAnimationData
{
    public string id;
    public float vertical;
    public float horizontal;
    public bool isSprinting;
}
public class NetworkAttackData
{
    public string id;
}
