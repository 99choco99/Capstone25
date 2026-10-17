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
    public int Frame;
    public float ValidUntil;
    public bool IsValid => Command != ActionCommand.None
        && (Time.time - Timestamp <= CombatSettings.Current.PlayerInputBufferTime
            || Command == ActionCommand.Attack && ValidUntil > 0f && Time.time <= ValidUntil);
}

public class PlayerInputBuffer
{
    private InputTicket currentTicket;

    public void AddCommand(ActionCommand cmd)
    {
        if (cmd == ActionCommand.Attack
            && currentTicket.Command == ActionCommand.Guard
            && currentTicket.Frame == Time.frameCount)
            return;

        currentTicket = new InputTicket
        {
            Command = cmd,
            Timestamp = Time.time,
            Frame = Time.frameCount
        };
    }

    public ActionCommand PeekValidCommand()
    {
        if (currentTicket.IsValid)
            return currentTicket.Command;

        Clear();
        return ActionCommand.None;
    }

    public InputTicket ConsumeCurrentCommand()
    {
        InputTicket ticket = currentTicket.IsValid ? currentTicket : default;
        Clear();
        return ticket;
    }

    public void KeepAttackUntil(float recoveryEndTime)
    {
        if (currentTicket.Command != ActionCommand.Attack || !currentTicket.IsValid)
            return;

        currentTicket.ValidUntil = Mathf.Max(
            currentTicket.ValidUntil,
            recoveryEndTime + CombatSettings.Current.PlayerInputBufferTime);
    }

    public void CancelHeldAttack()
    {
        if (currentTicket.Command == ActionCommand.Attack && currentTicket.ValidUntil > 0f)
            Clear();
    }

    public void Clear()
    {
        currentTicket = default;
    }
}
