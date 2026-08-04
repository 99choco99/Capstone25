using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public int id;

    [Header("생존 수치")]
    public float hp;
    public float posture;


    public int exp;
    public int gold;
}
