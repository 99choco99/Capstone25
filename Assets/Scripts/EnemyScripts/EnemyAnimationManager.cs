using UnityEngine;

public class EnemyAnimationManager : MonoBehaviour
{
    Enemy enemy;
    public bool IsPerformAction;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }


    public void PlayAnimation(string animationName, bool IsPerformAction = true)
    {
        if (this.IsPerformAction) { return; }
        enemy.Anim.CrossFade(animationName, 0.2f);
        this.IsPerformAction = IsPerformAction;
    }

    public void AE_PlaySFX(string name)
    {
        SoundManager.Instance.PlaySFX(name);
    }
}
