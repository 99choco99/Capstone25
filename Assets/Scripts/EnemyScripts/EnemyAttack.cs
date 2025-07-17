using UnityEngine;

public class EnemyAttack : MonoBehaviour
{

    [SerializeField] public Attack[] attacks;
    public Attack currentPattern;
    public int currentAnimationIndex;

}
