using UnityEngine;

public class Name : MonoBehaviour
{
    private Camera cam;
    
    void Start()
    {
        if(cam == null)
        {
            cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
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
