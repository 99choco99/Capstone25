using UnityEngine;

public enum ActionCommand
{
    None,
    Attack,
    Guard,
    Dodge,
    Jump
}

public struct InputTicket
{
    public ActionCommand Command;
    public float Timestamp;
    public bool IsValid => Command != ActionCommand.None;
}

public class PlayerInputBuffer
{
    private InputTicket currentTicket;

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
        if (!currentTicket.IsValid) return ActionCommand.None;
        if (Time.unscaledTime - currentTicket.Timestamp <= CombatSettings.Current.PlayerInputBufferTime) return currentTicket.Command;

        Clear();
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
