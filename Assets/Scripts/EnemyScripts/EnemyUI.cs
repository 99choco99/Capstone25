using UnityEngine;

public class EnemyUI : MonoBehaviour
{
    Enemy enemy;
    Collider col;
    Camera mainCamera;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
        col = GetComponentInParent<Collider>();
        mainCamera = Camera.main;
    }

    void Start()
    {
        transform.position = col.bounds.center + new Vector3(0, col.bounds.extents.y, 0);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.LookAt(mainCamera.transform);
    }
}
