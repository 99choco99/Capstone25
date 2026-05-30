using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;


public class StatModifier
{
    public readonly float Value;
    public readonly object Source;

    public StatModifier(float value, object source)
    {
        Value = value;
        Source = source;
    }
}

public class Stat
{
    [SerializeField] private float baseValue;

    private readonly List<StatModifier> modifiers = new List<StatModifier>();

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

    public float GetBaseValue()
    {
        return baseValue;
    }

    public void AddBaseValue(float amount)
    {
        baseValue += amount;
        isDirty = true;
    }


    public void AddModifier(float value ,ItemSpec source)
    {
        if(value == 0) { return; }
        modifiers.Add(new StatModifier(value, source));
        isDirty = true;
    }

    public void RemoveModifier(ItemSpec source)
    {
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            if (modifiers[i].Source.Equals(source))
            {
                modifiers.RemoveAt(i);
                isDirty = true;
            }
        }
    }

    public void ClearModifiers() => modifiers.Clear();


    private float CalcFinalValue()
    {
        float finalValue = baseValue;
        foreach (var mod in modifiers) { finalValue += mod.Value; }
        return finalValue;
    }
}
