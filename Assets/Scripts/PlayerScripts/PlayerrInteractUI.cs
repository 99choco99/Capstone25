using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;


public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private GameObject InteractInfo; //주변 interactable 의 정보
    [SerializeField] private GameObject containerGameObject;  //InteractInfo 를 담기 위함
    [SerializeField] private PlayerInteraction playerInteraction; // 플레이어의 interact 참조

    private Collider[] InteractObject;//플레이어 주변의 InteractObject  저장
    private List<GameObject> InteractUIContainer;  //InteractObject를 보여주기 위한 컨테이너
    private TMP_Text InteractObjectName;  // InteractObject의 이름


    private void Start()
    {
        InteractObject = new Collider[10];
        InteractUIContainer = new List<GameObject>();
    }

    private void Update()
    {
        InteractObject = playerInteraction.GetInteractObject();  //주변 InteractObject 가져오기
        InteractUI();  // UI
    }

    void InteractUI()
    {
        foreach (GameObject obj in InteractUIContainer)
        {
            obj.SetActive(false);
        }

        // 여유 Container가 없으면 생성, 있으면 Active해서 활용
        for (int i = 0; i < InteractObject.Length; i++)
        {
            if (InteractObject[i] == null) { break; }
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

            // 주변 NPC 이름 가져오기
            InteractObjectName = select.transform.GetChild(1).GetComponentInChildren<TMP_Text>();
            InteractObjectName.text = InteractObject[i].name;
        }
        if(InteractUIContainer.Count <= 0) { return; }
        //선택된 UI의 색 바꾸기
        InteractUIContainer[playerInteraction.preselectIndex].transform.GetChild(1).GetComponent<Image>().color = new Color(1, 0, 0);
        InteractUIContainer[playerInteraction.selectIndex].transform.GetChild(1).GetComponent<Image>().color = new Color(0, 1, 0);
    }
}
