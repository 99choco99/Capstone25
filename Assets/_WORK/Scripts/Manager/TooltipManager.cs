using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance; // ½Ì±ÛÅæ

    [SerializeField] private GameObject itemDescriptionObject;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // ÅøÆÁÀº Ã³À½¿¡ ¼û°ÜÁ® ÀÖ¾î¾ß ÇÔ
        itemDescriptionObject.SetActive(false);
    }

    public void ShowTooltip(string text, Vector3 position)
    {
        if (itemDescriptionObject == null) return;
        itemDescriptionText.text = text;
        itemDescriptionObject.transform.position = position + Vector3.down * 10;
        itemDescriptionObject.SetActive(true);
    }

    public void HideTooltip()
    {
        if (itemDescriptionObject == null) return;
        itemDescriptionObject.SetActive(false);
    }
}
