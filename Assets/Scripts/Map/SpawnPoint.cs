using UnityEngine;

public class SpawnPoint : MonoBehaviour
{

    private void OnEnable()
    {
        GameManager.instance.PlayerSpawner.RegisterSpawnPoint(transform);
    }
    private void OnDisable()
    {
        GameManager.instance.PlayerSpawner.UnregisterSpawnPoint(transform);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }

}
