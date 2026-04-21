using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public string id;
    public string nickname;

    public float maxHp;
    public float currentHp;
    public int level;
    public int exp;


    public float damage;
    public float defense;
    public float speed;
    public bool dead;
    public int gold;
    public int AbilityPoint;


    public float posX;
    public float posY;
    public float posZ;

    public float rotX;
    public float rotY;
    public float rotZ;
    public float rotW;


    public Vector3 ToVector3() => new Vector3(posX,posY,posZ);
    public Quaternion ToRotation() => new Quaternion(rotX, rotY, rotZ, rotW);
    public string currentSceneName;


    public void SetGold(float gold)
    {

    }

    public void SetExp(float exp)
    {

    }

    public void SetTransform(NetworkPosition pos, NetworkRotation rot)
    {
        posX = pos.x;
        posY = pos.y;
        posZ = pos.z;

        rotX = rot.x;
        rotY = rot.y;
        rotZ = rot.z;
        rotW = rot.w;

        //OnChangedPosition?.Invoke(this, pos);
    }

    public void SetScene(string SceneName)
    {
        currentSceneName = SceneName;

        //OnChangedScene?.invoke();
    }

    public void SetAbility()
    {
        
    }
}

