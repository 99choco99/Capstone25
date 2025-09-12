using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "Scriptable Objects/Attack")]
public class Attack : ScriptableObject
{
    public bool canGuard;
    public float damage;
    public float knockbackPower;
    public float knockbackDuration;
}
