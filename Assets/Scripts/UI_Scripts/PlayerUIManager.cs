using System;
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    public Slider PlayerHpUI;
    public Slider EnemyHpUI;
    public Slider ExpUI;
    public Image dialogUI;
    public TextMeshProUGUI EnemyName;
    public TextMeshProUGUI NpcName;
    public TextMeshProUGUI NpcText;
    public GameObject Inventory;
    public GameObject PlayerProfile;

    public bool isPlayerProfileOpen;
    public bool isInventoryOpen;
    public GameObject Setting;
    public bool isSettingOpen;

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

    public void SetMaxExp(float maxExp)
    {
        ExpUI.maxValue = maxExp;
    }
    

    public void OnInventory(InputAction.CallbackContext context)
    {
        if(context.phase == InputActionPhase.Started)
        {
            isInventoryOpen = !isInventoryOpen;
            Inventory.SetActive(isInventoryOpen);
        }
    }

    public void OnPlayerProfile(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            isPlayerProfileOpen = !isPlayerProfileOpen;
            PlayerProfile.SetActive(isPlayerProfileOpen);
        }
    }

    public void OnSetting(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            isSettingOpen = !isSettingOpen;
            Setting.SetActive(isSettingOpen);
        }
    }
}

