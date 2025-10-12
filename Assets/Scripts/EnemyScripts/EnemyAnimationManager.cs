using UnityEngine;

public class EnemyAnimationManager : MonoBehaviour
{
    Enemy enemy;
    bool IsPerformAction;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }


    public void PlayAnimation(string animationName, bool IsPerformAction = true)
    {
        enemy.Anim.CrossFade(animationName, 0.2f);
        this.IsPerformAction = IsPerformAction;
    }

}
