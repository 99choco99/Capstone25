using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Slider PlayerHp;
    public Slider EnemyHp;
    public TextMeshProUGUI EnemyName;

    public void ShowEnemyInfoUI()
    {
        EnemyHp.gameObject.SetActive(true);
        EnemyName.gameObject.SetActive(true);
        StartCoroutine("HideEnemyInfoUI");
    }
    IEnumerator HideEnemyInfoUI()
    {
        yield return new WaitForSeconds(4f);
        EnemyHp.gameObject.SetActive(false);
        EnemyName.gameObject.SetActive(false);
    }
}
