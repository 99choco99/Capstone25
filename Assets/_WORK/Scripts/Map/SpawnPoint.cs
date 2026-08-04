using UnityEngine;

public class SpawnPoint : MonoBehaviour
{

    private void OnEnable()
    {
        PlayerSpawner.Instance.RegisterSpawnPoint(transform);
    }
    private void OnDisable()
    {
        PlayerSpawner.Instance.UnregisterSpawnPoint(transform);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }

}
