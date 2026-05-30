using UnityEngine;

[System.Serializable]
public class PlayerData
{
    [Header("Identity & Routing")]
    public string id;
    public string nickname;
    public string currentSceneName;

    [Header("Stats & Progression")]
    public int level;
    public int exp;
    public int gold;
    public int abilityPoint;


    [Header("Combat Attributes")]
    public float maxHp;
    public float currentHp;
    public float damage;
    public float defense;
    public float speed;

    [Header("Transform (Position & Rotation)")]
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

