using System;
using Unity.Behavior;
using UnityEngine;
using Composite = Unity.Behavior.Composite;
using Unity.Properties;

[Serializable, GeneratePropertyBag]

[NodeDescription(name: "Selector", story: "Selector", category: "Flow", id: "de85289a3c5c99dd3086fc22689fed5c")]

public partial class SelectorSequence : Composite
{
    [CreateProperty] int m_CurrentChild;

    /// <inheritdoc cref="OnStart" />
    protected override Status OnStart()
    {
        m_CurrentChild = 0;
        if (Children.Count == 0)
            return Status.Success;

        var status = StartNode(Children[0]);
        if (status == Status.Failure)
            return Status.Failure;
        if (status == Status.Success)
            return Status.Running;

        return Status.Waiting;
    }

    /// <inheritdoc cref="OnUpdate" />
    protected override Status OnUpdate()
    {
        var currentChild = Children[m_CurrentChild];
        Status childStatus = currentChild.CurrentStatus;
        if (childStatus == Status.Success)
        {
            if (m_CurrentChild == Children.Count - 1)
                return Status.Success;

            m_CurrentChild++;

            var status = StartNode(Children[m_CurrentChild]);
            if (status == Status.Failure)
                return Status.Running;
            if (status == Status.Success)
                return Status.Success;
        }
        else if (childStatus == Status.Failure)
        {
            m_CurrentChild++;
            return Status.Failure;
        }
        return Status.Waiting;
    }
}