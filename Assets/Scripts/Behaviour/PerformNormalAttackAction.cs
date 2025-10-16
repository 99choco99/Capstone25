using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Random = UnityEngine.Random;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Perform NormalAttack", story: "Perform NormalAttack", category: "Action", id: "f2a7a86ff31f440231d686a1eda9484b")]
public partial class PerformNormalAttackAction : Action
{
    Enemy enemy;

    protected override Status OnStart()
    {
        if(enemy == null && GameObject != null)
        {
            enemy = GameObject.GetComponent<Enemy>();
        }
        if(enemy == null)
        {
            Debug.LogError("combat 컴포넌트 없음");
            return Status.Failure;
        }

        enemy.Combat.PerformAttack();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!enemy.AnimationManager.IsPerformAction)
        {
            return Status.Success;
        }
        return Status.Running;
    }

    protected override void OnEnd()
    {

    }

}

