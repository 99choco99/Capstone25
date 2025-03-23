using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Slider PlayerHpUI;
    public Slider EnemyHpUI;
    public TextMeshProUGUI EnemyName;

    public void ShowEnemyInfoUI()
    {
        EnemyHpUI.gameObject.SetActive(true);
        EnemyName.gameObject.SetActive(true);
        StartCoroutine(HideEnemyInfoUI());
    }
    IEnumerator HideEnemyInfoUI()
    {
        yield return new WaitForSeconds(4f);
        EnemyHpUI.gameObject.SetActive(false);
        EnemyName.gameObject.SetActive(false);
    }
}
