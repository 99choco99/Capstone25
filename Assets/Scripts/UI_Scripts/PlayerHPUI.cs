using UnityEngine;
using UnityEngine.UI;

public class PlayerHPUI : MonoBehaviour
{
    [SerializeField] PlayerStats player;
    Slider HPslider;

    private void Awake()
    {
        HPslider = GetComponent<Slider>();
    }

    private void Start()
    {
        HPslider.maxValue = player.maxHp;
        HPslider.value = player.currentHp;
    }
}
