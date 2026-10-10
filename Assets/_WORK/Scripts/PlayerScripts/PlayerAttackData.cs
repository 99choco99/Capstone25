using UnityEngine;

[CreateAssetMenu(fileName = "PlayerAttackData", menuName = "Scriptable Objects/PlayerAttackData")]
public class PlayerAttackData : AttackData
{
    [Header("다음 공격 데이터")]
    public PlayerAttackData NextComboAttack;

    [Header("플레이어 공격 타이밍")]
    public float commitFrame = 10f;
    public float comboStartFrame = 50f;
}
