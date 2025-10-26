using System.Collections;
using UnityEngine;

public class EnemyAnimationManager : MonoBehaviour
{
    Enemy enemy;
    public bool IsPerformAction;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        enemy.Stats.OnDeath += DeathProcess;
    }

    private void OnDestroy()
    {
        enemy.Stats.OnDeath -= DeathProcess;
    }


    public void PlayAnimation(string animationName, bool IsLockAction = true)
    {
        if (IsPerformAction) { return; }
        enemy.Anim.CrossFade(animationName, 0.1f);
        this.IsPerformAction = IsLockAction;
    }
    
    public void DeathProcess()
    {
        PlayAnimation("Die");
        StartCoroutine(Disappear());
    }

    //죽은 후 2.5초뒤 시체 없어짐.
    IEnumerator Disappear()
    {
        yield return new WaitForSeconds(2.5f);
        Destroy(gameObject);
    }

    public void AE_PlaySFX(string name)
    {
        SoundManager.Instance.PlaySFX(name);
    }
}
