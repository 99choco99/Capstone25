using UnityEngine;

[System.Serializable]
public class PlayerData
{
    [Header("Stats & Progression")]
    public int level;
    public int exp;
    public int abilityPoint;

    [Header("Survival Attributes")]
    public float maxHp;
    public float currentHp;
    public float maxPosture;

    [Header("Combat Attributes")]
    public float attackPower;
}
