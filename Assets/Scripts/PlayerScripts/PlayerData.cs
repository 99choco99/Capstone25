using UnityEngine;

public class PlayerData
{
    public string user_id;
    public string nickname;

    public float maxHp { get; set; }
    public float currentHp { get; set; }
    public float damage { get; set; }
    public float defense { get; set; }
    public float speed { get; set; }
    public float exp { get; set; }
    public int level { get; set; }
    public bool dead { get; set; }

}
