using Firesplash.GameDevAssets.SocketIO; // Asset의 네임스페이스 사용
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;


public class SocketManager : MonoBehaviour
{

    // SocketIOCommunicator 컴포넌트를 담을 변수
    private SocketIOCommunicator sioCom;
    private string userId;

    //서버 이벤트
    public event Action<bool> OnServerConnected;                //서버 연결 여부
    public event Action<bool> OnServerDisconnected;

    public event Action<PlayerData> OnLocalPlayerJoined;                    //내 플레이어 참석
    public event Action<List<NetworkPlayerData>> OnCurrentPlayersReceived;  //현재 접속된 플레이어 목록 받음.
    public event Action<NetworkPlayerData> OnRemotePlayerJoined;            //플레이어 참석
    public event Action<string> OnRemotePlayerLeft;                         //플레이어 나감
    public event Action<NetworkPosition> OnRemotePlayerMoved;               //플레이어 움직임
    public event Action<NetworkAnimationData> OnRemotePlayerAnimated;       //플레이어 애니메이션

    // 서버 연결 메소드
    public void ConnectToServer(string userId)
    {
        if (userId == null) { Debug.LogError("유저 아이디 에러"); return; }

        this.userId = userId;

        // 이미 연결 중이거나 연결된 상태가 아닐 때만 연결을 시도
        if (!sioCom.Instance.IsConnected())
        {
            // 이벤트 리스너 등록
            sioCom.Instance.On("connect", OnSocketConnect);
            sioCom.Instance.On("connect_error", OnSocketConnectError);
            sioCom.Instance.On("disconnect", OnSocketDisconnect);

            // 연결 시도
            try
            {
                sioCom.Instance.Connect();
            }
            catch (UriFormatException e)
            {
                Debug.LogError($"주소 형식이 잘못되었습니다: {e.Message}");
            }
        }
    }

    // --- 콜백 메서드들 ---
    private void OnSocketConnect(string data)
    {
        Debug.Log("서버 연결 성공!");
        OnServerConnected?.Invoke(true);
    }

    private void OnSocketConnectError(string message)
    {
        Debug.LogError($"서버 연결 실패 (에러 내용): {message}");
        OnServerConnected?.Invoke(false);
    }

    private void OnSocketDisconnect(string reason)
    {
        Debug.LogWarning($"서버와 연결이 끊어졌습니다. 사유: {reason}");
        OnServerConnected?.Invoke(false);
    }

    void Start()
    {
        sioCom = GetComponent<SocketIOCommunicator>();

        // ========== 서버로부터 오는 이벤트 리스너 설정 ==========

        // 서버에 성공적으로 연결되었을 때

        // 다른 플레이어들의 현재 목록을 받았을 때
        sioCom.Instance.On("OnReceivedCurrentPlayers", (string payload) =>
        {
            List<NetworkPlayerData> data = JsonConvert.DeserializeObject<List<NetworkPlayerData>>(payload); 
            OnCurrentPlayersReceived?.Invoke(data);
        });

        // 새로운 플레이어가 접속했을 때
        sioCom.Instance.On("OnRemotePlayerJoined", (string payload) =>
        {
            NetworkPlayerData data = JsonConvert.DeserializeObject<NetworkPlayerData>(payload);
            OnRemotePlayerJoined?.Invoke(data);
        });


        //다른 플레이어의 움직임을 업데이트
        sioCom.Instance.On("updatePlayerMovement", (string payload) =>
        {
            NetworkPosition data = JsonConvert.DeserializeObject<NetworkPosition>(payload);
            OnRemotePlayerMoved?.Invoke(data);
        });


        //이동 애니메이션 동기화
        sioCom.Instance.On("updatePlayerAnimation", (string payload) =>
        {

        });

        //공격 애니메이션 업데이트
        sioCom.Instance.On("updateAttack", (string payload) =>
        {

        });


        // 죽었을 때 혹은 씬 이동시
        sioCom.Instance.On("respawn", (string payload) =>
        {

        });


        sioCom.Instance.On("updateGold", (string payload) =>
        {
            try
            {
                // 1. 올바른 방법으로 JSON을 파싱합니다.
                var data = JsonConvert.DeserializeObject<GoldUpdateData>(payload);

                int newGoldAmount = data.gold;

                Debug.Log($"물품 판매됨. 서버 골드: {newGoldAmount}");

            }
            catch (Exception e)
            {
                Debug.LogError($"[Socket.IO] updateGold 파싱 오류: {e.Message}\nPayload: {payload}");
            }
        });

        // 다른 플레이어의 접속이 끊겼을 때 서버가 보내주는 커스텀 이벤트
        sioCom.Instance.On("playerDisconnected", (string payload) =>
        {

        });


        // 플레이어가 연결을 끊었을 때
        sioCom.Instance.On("disconnect", (string payload) =>
        {

        });
    }


    // ========== 서버로 데이터를 보내는 함수들 ==========

    //씬 참여 요청
    public void EmitJoinScene(PlayerData data, string SceneName)
    {
        var json = new
        {
            userId = data.id,
            sceneName = SceneName,
            position = new { x = data.posX, y = data.posY, z = data.posZ },
            rotation = new { x = data.rotX, y = data.rotY, z = data.rotZ, w = data.rotW }
        };

        string sendingData = JsonConvert.SerializeObject(json);
        sioCom.Instance.Emit("RequestJoinScene", sendingData, false);
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

    private void OnApplicationQuit()
    {
        if (sioCom != null && sioCom.Instance.IsConnected())
        {
            sioCom.Instance.Close();
        }
    }
}
