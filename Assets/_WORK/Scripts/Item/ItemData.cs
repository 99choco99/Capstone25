using Newtonsoft.Json;
using System;
using System.Text;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine.UI;

//아이템의 기본 데이터를 정의
public class ItemBase
{
    public int id;
    public string itemName;
    public SlotType type;
    public string description;
}


public enum EquipmentType { Helmet, Top, Bottom, Shoes, Gloves, Accessory }


//장비 아이템 원본 정의
public class EquipmentBaseData : ItemBase
{
    public EquipmentType equipmentType; // Helmet, Top, Weapon 등
    public ItemSpec baseStats;
}

//소비 아이템 원본 정의
public class ConsumptionBaseData : ItemBase
{
    public float amount;    // 회복량
    public float duration;      // 지속시간
    public float coolTime;      // 쿨타임
}

public class OtherBaseData : ItemBase { }

//아이템의 스탯을 정의
[Serializable]
public struct ItemSpec
{
    public float attackPower;
    public float defense;
    public float maxHp;
    public float posture;


    public static ItemSpec operator +(ItemSpec a, ItemSpec b)
    {
        return new ItemSpec
        {
            attackPower = a.attackPower + b.attackPower,
            defense = a.defense + b.defense,
            maxHp = a.maxHp + b.maxHp,
            posture = a.posture + b.posture,
        };
    }
}

//사용자가 볼 아이템의 최종 정보
[Serializable]
public abstract class ItemInstance
{
    public int templateId;    // (원본이 뭔지 기억하기 위함)

    [JsonIgnore]
    public ItemBase BaseData => ItemManager.Instance.GetItem(templateId);
    public ItemInstance(int templateId)
    {
        this.templateId = templateId;
    }

    public abstract string GetToolTipText();

}

[Serializable]
public class EquipmentInstance : ItemInstance
{
    public string instanceId; // (서버가 발급한 고유번호)
    public int enhanceLevel;
    public ItemSpec bonusStat;

    public EquipmentInstance(int templateId) : base(templateId)
    {
        instanceId = Guid.NewGuid().ToString();
        enhanceLevel = 0;
        bonusStat = new ItemSpec();
    }
    public ItemSpec GetFinalStats()
    {
        ItemBase baseData = ItemManager.Instance.GetItem(templateId);

        if (baseData is EquipmentBaseData equipData)
        {
            ItemSpec enhanceStat = new ItemSpec
            {
                attackPower = enhanceLevel * 2f,
                defense = enhanceLevel * 3f,
                maxHp = enhanceLevel * 10f,
                posture = 0f
            };

            return equipData.baseStats + bonusStat + enhanceStat;
        }

        return new ItemSpec();
    }

    public override string GetToolTipText()
    {
        ItemSpec stats = GetFinalStats();
        StringBuilder statsBuilder = new StringBuilder();

        if (stats.attackPower > 0) statsBuilder.AppendLine($"공격력: +{stats.attackPower}");
        if (stats.defense > 0) statsBuilder.AppendLine($"방어력: +{stats.defense}");
        if (stats.maxHp > 0) statsBuilder.AppendLine($"체력: +{stats.maxHp}");

        return statsBuilder.ToString().TrimEnd();
    }
}

[Serializable]
public class ConsumptionInstance : ItemInstance
{
    public ConsumptionInstance(int templateId) : base(templateId) { }

    public override string GetToolTipText()
    {
        if (BaseData is ConsumptionBaseData consData)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (consData.amount > 0) sb.AppendLine($"회복량: {consData.amount}");
            if (consData.duration > 0) sb.AppendLine($"지속시간: {consData.duration}초");
            if (consData.coolTime > 0) sb.AppendLine($"쿨타임: {consData.coolTime}초");
            return sb.ToString().TrimEnd();
        }
        return "";
    }
}

[Serializable]
public class OtherInstance : ItemInstance
{
    public OtherInstance(int templateId) : base(templateId) { }

    public override string GetToolTipText()
    {
        return "";
    }
}

