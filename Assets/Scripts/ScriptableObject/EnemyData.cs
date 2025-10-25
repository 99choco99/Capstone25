using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public int id;

    public float hp;
    public float defense;

    public int exp;
    public int gold;
}
