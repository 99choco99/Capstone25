using System.Collections.Generic;
using UnityEngine;

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

