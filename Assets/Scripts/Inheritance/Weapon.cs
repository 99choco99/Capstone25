using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    protected bool canTrigger = true;

    protected IEnumerator ResetTrigger()
    {
        yield return new WaitForSeconds(0.5f);
        canTrigger = true;
    }

}
