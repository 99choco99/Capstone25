using UnityEngine;

public class Stat
{
    [SerializeField] private float baseValue;

    public Stat(float baseValue)
    {
        this.baseValue = baseValue;
    }

    public float GetValue()
    {
        return baseValue;
    }

    public void AddBaseValue(float amount)
    {
        baseValue += amount;
    }
}
