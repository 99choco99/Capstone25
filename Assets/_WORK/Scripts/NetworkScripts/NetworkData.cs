using System.Collections.Generic;
using UnityEngine;

/// <summary>기존 서버 API의 데이터를 보존합니다. 로컬 저장은 PlayerData만 사용합니다.</summary>
[System.Serializable]
public class ServerPlayerData : PlayerData
{
    public string id;
    public string nickname;
    public string currentSceneName;
    public float speed;

    public float posX;
    public float posY;
    public float posZ;
    public float rotX;
    public float rotY;
    public float rotZ;
    public float rotW;

    public Vector3 GetPosition() => new Vector3(posX, posY, posZ);
    public Quaternion GetRotation() => new Quaternion(rotX, rotY, rotZ, rotW);
}

public class NetworkPlayerData
{
    public string id;
    public NetworkPosition position;
    public NetworkRotation rotation;
    public string nickname;
    public string currentSceneName;
}

public class NetworkPosition
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

