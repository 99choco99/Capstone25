using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;


public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private GameObject containerGameObject;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private GameObject InteractInfo;

    private List<GameObject> InteractUIContainer;
    private Collider[] InteractObject;
    private TMP_Text InteractObjectName;


    private void Start()
    {
        InteractUIContainer = new List<GameObject>();
    }

    private void Update()
    {
        InteractObject = playerInteraction.GetInteractObject();
        
        foreach(GameObject obj in InteractUIContainer)
        {
            obj.SetActive(false);
        }

        for(int i = 0; i< InteractObject.Length; i++)
        {
            GameObject select;
            if (i < InteractUIContainer.Count)
            {

                select = InteractUIContainer[i];
                select.SetActive(true);
            }
            else
            {
                select = Instantiate(containerGameObject, InteractInfo.transform);
                InteractUIContainer.Add(select);
            }

            // 이름을 설정합니다.
            InteractObjectName = select.transform.GetChild(1).GetComponentInChildren<TMP_Text>();
            InteractObjectName.text = InteractObject[i].name;

    }
        // InteractObject가 존재하면 Show, 없으면 Hide
        if (InteractObject.Length > 0)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
    void Show()
    {
        InteractInfo.SetActive(true);
    }

    void Hide()
    {
        InteractInfo.SetActive(false);
    }


}
