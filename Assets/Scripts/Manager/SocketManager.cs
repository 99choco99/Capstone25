using Firesplash.GameDevAssets.SocketIO; // Asset의 네임스페이스 사용
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample.DistributedAuthority;
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

    //씬 전환중인 플래그
    private bool isSceneChangeInProgress = false;
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
            if (!string.IsNullOrEmpty(userId))
            {
                sioCom.Instance.Emit("initialize", userId,true);
            }

        });

        sioCom.Instance.On("initializeComplete", (payload) =>
        {
            myInitialData = JsonConvert.DeserializeObject<NetworkPlayerData>(payload);
            if (PublicAPIManager.Instance != null && PublicAPIManager.Instance.PlayerData != null)
            {
                PublicAPIManager.Instance.PlayerData.OnPlayerDataLoaded += HandlePlayerDataLoadedForRespawn;
                PublicAPIManager.Instance.PlayerData.OnPlayerDataLoadFailed += HandlePlayerDataLoadFailedForRespawn;

                // 이제 데이터 로드를 요청
                PublicAPIManager.Instance.RequestPlayerData();
            }
            else
            {
                // [예외 처리]
                Debug.LogError("PublicAPIManager or PlayerDataAPI is missing. Cannot fetch updated stats. Loading scene anyway...");
                LoadSceneFromInitialData();
            }
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


        // 죽었을 때 혹은 씬 이동시
        sioCom.Instance.On("respawn", (string payload) =>
        {
            if (!string.IsNullOrEmpty(userId))
            {
                //씬 로드 완료 리스너를 제거 (새로 initialize할 것이므로)
                SceneManager.sceneLoaded -= OnSceneLoaded;

                // 기존 맵에 있던 플레이어 객체들 제거
                foreach (var player in networkPlayers.Values)
                {
                    Destroy(player);
                }
                networkPlayers.Clear();

                sioCom.Instance.Emit("initialize", userId, true);
            }
        });


        sioCom.Instance.On("updateGold", (string payload) =>
        {
            try
            {
                // 1. 올바른 방법으로 JSON을 파싱합니다.
                var data = JsonConvert.DeserializeObject<GoldUpdateData>(payload);

                int newGoldAmount = data.gold;

                Debug.Log($"물품 판매됨. 서버 골드: {newGoldAmount}");

                DataManager.Instance.Player.Stats.SetGold(newGoldAmount);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Socket.IO] updateGold 파싱 오류: {e.Message}\nPayload: {payload}");
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

        if (!player.TryGetComponent<Player>(out var playerComponent))
        {
            Debug.LogError("Player 프리팹에 Player.cs 컴포넌트가 없습니다! 스폰을 중단합니다.");
            Destroy(player);
            return;
        }

        if (isLocal)
        {
            isSceneChangeInProgress = false;

            // ========== 로컬 플레이어 설정 ==========
            playerComponent.IsLocalPlayer = true;
            player.GetComponent<PlayerInput>().enabled = true;
            playerComponent.InputHandler.enabled = true;
            playerComponent.StateMachine.enabled = true;
            playerComponent.PreviewCamera.enabled = true;

            if (player.TryGetComponent<CharacterController>(out var controller))
            {
                controller.enabled = true;
            }

            if (DataManager.Instance != null)
            {
                DataManager.Instance.Register(playerComponent);
            }
            Name name = playerComponent.GetComponentInChildren<Name>();
            name.gameObject.SetActive(false);

        }
        else
        {
            // ========== 더미 (원격) 플레이어 설정 ==========
            playerComponent.IsLocalPlayer = false;

            player.GetComponent<PlayerInput>().enabled = false;

            playerComponent.InputHandler.enabled = false;
            playerComponent.StateMachine.enabled = false;
            playerComponent.Motor.enabled = false;            
            playerComponent.Stats.enabled = false;            
            playerComponent.Interaction.enabled = false;      
            playerComponent.TargetingSystem.enabled = false;  
            playerComponent.Combat.enabled = false;           
            playerComponent.animatorManager.enabled = false;  
            playerComponent.Inventory.enabled = false;        
            playerComponent.Quest.enabled = false;            
            playerComponent.Dialogue.enabled = false;         
            playerComponent.Equipment.enabled = false;        
            playerComponent.localAPI.enabled = false;
            playerComponent.PreviewCamera.enabled = false;
            playerComponent.PlayerUIManager.gameObject.SetActive(false);

            // PlayerInteractUI도 비활성화
            if (player.TryGetComponent<PlayerInteractUI>(out var interactUI))
            {
                interactUI.enabled = false;
            }

            if (player.TryGetComponent<CharacterController>(out var controller))
            {
                controller.enabled = false;
            }

            networkPlayers.Add(data.id, player);
            Name nameTag = player.GetComponentInChildren<Name>();
            if (nameTag != null)
            {
                nameTag.SetNickname(data.nickname);
            }
        }
    }




    // ========== 서버로 데이터를 보내는 함수들 ==========

    //플레이어가 씬이 바뀔때
    public void EmitSceneChange(string sceneName, Vector3 position)
    {
        if (isSceneChangeInProgress) { return; }
        isSceneChangeInProgress = true;

        var localPlayer = DataManager.Instance.Player;
        if (localPlayer != null)
        {
            if (localPlayer.TryGetComponent<PlayerInput>(out var playerInput))
            {
                playerInput.enabled = false;
            }
            if (localPlayer.TryGetComponent<PlayerMotor>(out var playerMotor))
            {
                playerMotor.enabled = false;
            }
        }

        DataManager.Instance.SaveData();

        // 서버로 보낼 데이터 구성 (씬 이름, 새 위치)
        var json = new
        {
            scene = sceneName,
            pos = new { x = position.x, y = position.y, z = position.z }
        };
        var data = JsonConvert.SerializeObject(json);


        sioCom.Instance.Emit("requestSceneChange", data, false);
    }

    //플레이어가 움직일때 (좌표 동기화)
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

    //플레이어가 움직일때 (애니메이션 동기화)
    public void EmitPlayerMoveAnimation(float vertical, float horizontal)
    {
        NetworkAnimationData data = new();
        data.id = userId;
        data.vertical = vertical;
        data.horizontal = horizontal;
        var json = JsonConvert.SerializeObject(data);

        sioCom.Instance.Emit("playerAnimation", json, false);
    }

    //플레이어가 공격했을 때 (동기화)
    public void EmitPlayerAttack()
    {
        sioCom.Instance.Emit("playerAttack");
    }
    //플레이어 죽었을 떄.
    public void EmitPlayerDied()
    {
        sioCom.Instance.Emit("playerDied");
    }

    //서버 연결 메소드
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

    // initializeComplete 이후 PlayerData 로드가 성공했을 때
    private void HandlePlayerDataLoadedForRespawn()
    {
        PublicAPIManager.Instance.PlayerData.OnPlayerDataLoaded -= HandlePlayerDataLoadedForRespawn;
        PublicAPIManager.Instance.PlayerData.OnPlayerDataLoadFailed -= HandlePlayerDataLoadFailedForRespawn;

        Debug.Log("Respawn: Fetched latest player data (HP reset). Loading scene...");

        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadSceneFromInitialData();
    }

    //PlayerData 로드 실패 시 예외 처리
    private void HandlePlayerDataLoadFailedForRespawn(string error)
    {
        PublicAPIManager.Instance.PlayerData.OnPlayerDataLoaded -= HandlePlayerDataLoadedForRespawn;
        PublicAPIManager.Instance.PlayerData.OnPlayerDataLoadFailed -= HandlePlayerDataLoadFailedForRespawn;

        Debug.LogError($"Respawn: Failed to fetch updated player data ({error}). Loading scene with potentially stale stats...");

        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadSceneFromInitialData();
    }

    // 씬 로드 로직
    private void LoadSceneFromInitialData()
    {
        string sceneToLoad = "Main";
        if (myInitialData != null && !string.IsNullOrEmpty(myInitialData.currentSceneName))
        {
            sceneToLoad = myInitialData.currentSceneName;
        }
        LoadingScene.LoadScene(sceneToLoad);
    }

    private void SpawnAndNotifyServer()
    {
        SpawnPlayer(myInitialData, true);
        sioCom.Instance.Emit("LoadSceneComplete");
    }

    // 씬 로딩 완료 시 호출될 함수
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Loading")
        {
            SpawnAndNotifyServer();
        }
    }

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
    public string nickname;
    public string currentSceneName;
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

public class GoldUpdateData
{
    public int gold;
}
