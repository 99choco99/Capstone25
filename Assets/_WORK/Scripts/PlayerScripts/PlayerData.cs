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


    [Header("Survival Attributes")]
    public float maxHp;
    public float currentHp;
    public float speed;

    [Header("Combat Attributes")]
    // 구버전 JSON에서 이 필드가 없으면 0으로 들어오며 PlayerStats가 기본 공격력으로 보정합니다.
    public float attackPower;

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

