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


    public void PlayAnimation(string animationName, bool IsPerformAction = true)
    {
        if (this.IsPerformAction) { return; }
        enemy.Anim.CrossFade(animationName, 0.2f);
        this.IsPerformAction = IsPerformAction;
    }

    public void DeathProcess()
    {
        PlayAnimation("Die");
        StartCoroutine(Disappear());
    }
    public void DeathBlowProcess()
    {
        enemy.Stats.dead = true;
        enemy.Anim.enabled = false;
        Debug.Log("½ÅÈ£¿È");
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
