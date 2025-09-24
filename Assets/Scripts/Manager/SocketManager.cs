using Firesplash.GameDevAssets.SocketIO; // Asset의 네임스페이스 사용
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static APIManager;

public class SocketManager : MonoBehaviour
{
    public static SocketManager instance;
    public GameObject playerPrefab;

    // SocketIOCommunicator 컴포넌트를 담을 변수
    private SocketIOCommunicator sioCom;
    private string userId;


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
        // NetworkManager 오브젝트에 붙어있는 SocketIOCommunicator 컴포넌트를 가져옴
        sioCom = GetComponent<SocketIOCommunicator>();

        // ========== 서버로부터 오는 이벤트 리스너 설정 ==========

        // 서버에 성공적으로 연결되었을 때
        sioCom.Instance.On("connect", (string payload) =>
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log(userId);
            if (!string.IsNullOrEmpty(userId))
            {
                sioCom.Instance.Emit("initialize", userId,true);
            }
            // TODO: 여기에 내 로컬 플레이어를 생성하는 로직 추가
            LoadingScene.LoadScene("Main");
        });

        // 다른 플레이어들의 현재 목록을 받았을 때 (JSON 문자열로 받음)
        sioCom.Instance.On("currentPlayers", (string payload) =>
        {
            Debug.Log("Received current players: " + payload);

            // 받은 JSON 문자열을 PlayerDictionary 클래스로 파싱
            var playerList = JsonConvert.DeserializeObject<NetworkPlayerList>(payload);

            foreach (var playerData in playerList.players)
            {
                if (playerData.id != this.userId)
                {
                    SpawnPlayer(playerData.id, playerData.position);
                }
            }
        });

        // 새로운 플레이어가 접속했을 때
        sioCom.Instance.On("newPlayer", (string payload) =>
        {
            NetworkPlayerData playerData = JsonConvert.DeserializeObject<NetworkPlayerData>(payload);

            SpawnPlayer(playerData.id , playerData.position);
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
                    networkPlayer.UpdatePosition(new Vector3(data.position.x, data.position.y, data.position.z));
                }
            }

        });

        //애니메이션 동기화
        sioCom.Instance.On("updateAnimation", (string payload) =>
        {
            // [수정] JsonConvert.DeserializeObject 사용
            var data = JsonConvert.DeserializeObject<NetworkAnimationData>(payload);
            if (networkPlayers.ContainsKey(data.id))
            {
                Animator anim = networkPlayers[data.id].GetComponent<Animator>();
                if (anim != null) anim.SetBool("isMoving", data.isMoving);
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

    void SpawnPlayer(string id, NetworkPosition pos)
    {
        var spawnPosition = new Vector3(pos.x, pos.y, pos.z);
        GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

        var controller = player.GetComponent<PlayerInputHandler>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        networkPlayers.Add(id, player);
    }


    // ========== 서버로 데이터를 보내는 함수들 ==========

    public void EmitPlayerMovement(Vector3 position, Quaternion rotation)
    {
        var json = new
        {
            position = new { x = position.x, y = position.y, z = position.z },
            rotation = new { x = rotation.x, y = rotation.y, z = rotation.z}
        };
        var data = JsonConvert.SerializeObject(json);
        // [수정] JsonConvert.SerializeObject 사용
        sioCom.Instance.Emit("playerMovement",data,false);

    }

    public void EmitPlayerAnimation(bool isMoving)
    {
        var animData = new { isMoving = isMoving };
        // [수정] JsonConvert.SerializeObject 사용
        sioCom.Instance.Emit("playerAnimation", JsonConvert.SerializeObject(animData), false);
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
        GameManager.instance.ChangeState(GameState.Gameplay);
        GameObject myPlayerObj = Instantiate(playerPrefab);
        Player playerComponent = myPlayerObj.GetComponent<Player>();
        if (playerComponent != null)
        {
            playerComponent.IsLocalPlayer = true;
            playerComponent.InputHandler.enabled = true;
            playerComponent.StateMachine.enabled = true;
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
    public NetworkPosition position;
}

public class NetworkPosition
{
    public float x;
    public float y;
    public float z;
}

public class NetworkRotation
{
    public float x;
    public float y;
    public float z;
}

public class NetworkPlayerList
{
    public List<NetworkPlayerData> players;
}

public class NetworkAnimationData
{
    public string id;
    public bool isMoving;
}

public class NetworkAttackData
{
    public string id;
}
