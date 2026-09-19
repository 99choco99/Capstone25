using System.Collections.Generic;
using UnityEngine;

public class PlayerRepository
{
    Dictionary<string ,GameObject> players = new Dictionary<string ,GameObject>();

    public void AddPlayer(string id, GameObject player)
    {
        if (!players.ContainsKey(id)) players.Add(id, player);
    }

    public void RemovePlayer(string id)
    {
        if (players.ContainsKey(id))
        {
            GameObject.Destroy(players[id]);
            players.Remove(id);
        }
    }

    public GameObject GetPlayer(string id)
    {
        return players.GetValueOrDefault(id);
    }

    public bool HasPlayer(string id)
    {
        return players.ContainsKey(id);
    }
    public void ClearAllPlayers()
    {
        foreach (var player in players.Values)
        {
            if (player != null) GameObject.Destroy(player);
        }
        players.Clear();
    }
}
