using System.Collections.Generic;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public enum ActionCommand { None, Attack, Dodge, Jump, Interact }

public struct InputTicket
{
    public ActionCommand Command;
    public float Timestamp;
    public bool IsValid => Command != ActionCommand.None;
}

public class PlayerInputBuffer
{
    private InputTicket currentTicket;
    [SerializeField] private float bufferTime = 0.25f;

    public void AddCommand(ActionCommand cmd)
    {
        currentTicket = new InputTicket
        {
            Command = cmd,
            Timestamp = Time.unscaledTime
        };
    }
    public ActionCommand PeekValidCommand()
    {
        if (currentTicket.IsValid)
        {
            if (Time.unscaledTime - currentTicket.Timestamp <= bufferTime)
            {
                return currentTicket.Command;
            }
            else
            {
                Clear();
            }
        }
        return ActionCommand.None;
    }

    public void ConsumeCurrentCommand()
    {
        Clear();
    }

    public void Clear()
    {
        currentTicket = default;
    }
}