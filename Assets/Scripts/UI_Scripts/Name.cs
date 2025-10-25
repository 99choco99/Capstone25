using TMPro;
using UnityEngine;

public class Name : MonoBehaviour
{
    private Camera cam;
    private TextMeshProUGUI nametext;

    void Start()
    {
        if (cam == null)
        {
            cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        }
        nametext = GetComponentInChildren<TextMeshProUGUI>();
        nametext.text = gameObject.transform.root.name;
        if(gameObject.tag == "Player")
        {
            nametext.text = PublicAPIManager.Instance.loginData.nickname;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (cam != null)
        {
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward);
        }
    }
}
