using UnityEngine;

[System.Serializable]
public class PlayerData
{
    [Header("Stats & Progression")]
    public int level;
    public int exp;
    public int gold;
    public int abilityPoint;


    [Header("Survival Attributes")]
    public float maxHp;
    public float currentHp;
    public float maxPosture = 100f;

    [Header("Combat Attributes")]
    // 구버전 JSON에서 이 필드가 없으면 0으로 들어오며 PlayerStats가 기본 공격력으로 보정합니다.
    public float attackPower;

}

