using TMPro;
using UnityEngine;

public class Name : MonoBehaviour
{
    private UnityEngine.Camera cam;
    private TextMeshProUGUI nametext;

    void Start()
    {
        if (cam == null)
        {
            cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<UnityEngine.Camera>();
        }
        nametext = GetComponentInChildren<TextMeshProUGUI>();
    }


    public void SetNickname(string nickname)
    {
        if (nametext == null)
        {
            nametext = GetComponentInChildren<TextMeshProUGUI>();
        }
        nametext.text = nickname;
    }

    void Update()
    {
        if (cam != null)
        {
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward);
        }
    }
}
