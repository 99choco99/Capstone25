using System;
using TMPro;
using UnityEngine;

public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance;

    public GameObject MarketUI;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    public void BuyItem()
    {

    }

    public void SellItem()
    {

    }
}
