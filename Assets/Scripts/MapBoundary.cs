using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    // 씬의 스폰 지점을 저장할 변수
    [SerializeField] private Transform spawnPoint;


    // 맵뚫 방지
    private void OnTriggerExit(Collider other)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("스폰 지점이 설정되지 않아 리스폰할 수 없습니다.");
            return;
        }

        if (other.CompareTag("Player"))
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                other.transform.position = spawnPoint.position;
                cc.enabled = true;
            }
            else
            {
                Debug.LogError("CharacterController가 없습니다.");
            }
        }
    }
}
