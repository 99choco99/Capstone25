using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    public Slider PlayerHpUI;
    public Slider EnemyHpUI;
    public TextMeshProUGUI EnemyName;
    public Image dialogUI;
    public TextMeshProUGUI NpcName;
    public TextMeshProUGUI NpcText;
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

    public void ShowDialogUI()
    {
        dialogUI.gameObject.SetActive(true);
    }
    public void HideDialogUI()
    {
        dialogUI.gameObject.SetActive(false);
    }

    public void SetNpcText(string text)
    {
        NpcText.text = text;
    }

    public void SetNpcName(string name)
    {
        NpcName.text = name;
    }
}

