using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Stat
{
    [SerializeField] private float baseValue;

    private readonly List<float> modifiers = new List<float>();

    public float Value {get;private set;}
    private bool isDirty = true;

    public Stat(float baseValue)
    {
        this.baseValue = baseValue;
        Value = baseValue;
    }

    public float GetValue()
    {
        if (isDirty) {
            Value = CalcFinalValue();
            isDirty = false;
        }
        return Value;
    }

    public void AddBaseValue(float amount)
    {
        baseValue += amount;
        isDirty = true;
    }


    public void AddModifier(float mod)
    {
        if(mod == 0) { return; }
        modifiers.Add(mod);
        isDirty = true;
    }

    public void RemoveModifier(float mod)
    {
        if (mod != 0)
        {
            if (modifiers.Remove(mod)) { isDirty = true; }
        }
    }



    private float CalcFinalValue()
    {
        float finalValue = baseValue;
        foreach (var mod in modifiers) { finalValue += mod; }
        return finalValue;
    }
}
