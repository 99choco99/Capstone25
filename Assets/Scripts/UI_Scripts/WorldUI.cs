using UnityEngine;

public class WorldUI : MonoBehaviour
{
    [SerializeField] private PlayerInteractUI interactUI;
    [SerializeField] private TargetingUI targetingUI;
    [SerializeField] private EnemyUI enemyUI;

    public void Init(Player localPlayer)
    {
        if(localPlayer == null) { return; }
        if (interactUI != null && localPlayer.Interaction != null)
        {
            interactUI.Init(localPlayer.Interaction);
        }


        if (targetingUI != null && localPlayer.TargetingSystem != null)
            targetingUI.Init(localPlayer.TargetingSystem);
    }
}
