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
        this.IsPerformAction = IsLockAction;
        enemy.Anim.CrossFade(animationName, 0.1f);
    }

    public void UpdateLocomotion(float forward, float right, float speed)
    {
        enemy.Anim.SetFloat("Vertical", forward, 0.1f, Time.deltaTime);
        enemy.Anim.SetFloat("Horizontal", right, 0.1f, Time.deltaTime);
        enemy.Anim.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
    }
    
    public void DeathProcess()
    {
        PlayAnimation("Die");
        StartCoroutine(Disappear());
    }

    //Á×Àº ÈÄ 2.5ÃÊµÚ ½ÃÃ¼ ¾ø¾îÁü.
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
