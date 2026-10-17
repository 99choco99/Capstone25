using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>원본 모션의 활성 판정과 몸 캡슐을 에디터에서 측정한 자료. 보조 전진/가드 판정은 포함하지 않습니다.</summary>
public sealed class CombatReachDatabase : ScriptableObject
{
    public const int FormatVersion = 1;
    public int version;
    public List<Actor> actors = new();
    public List<string> sourcePaths = new();
    public List<string> sourceHashes = new();

    [Serializable]
    public struct Band
    {
        public float min;
        public float max;
        public Band(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    [Serializable]
    public struct Body
    {
        public Vector3 center;
        public float radius;
        public float height;
        public bool Matches(Body other) => (center - other.center).sqrMagnitude < 0.000001f
            && Mathf.Abs(radius - other.radius) < 0.001f && Mathf.Abs(height - other.height) < 0.001f;

        public static bool TryRead(Transform root, out Body body)
        {
            body = default;
            Vector3 scale = root.lossyScale;
            Vector3 center;
            float radius, height;
            if (root.TryGetComponent<CharacterController>(out var controller))
            {
                center = controller.center;
                radius = controller.radius;
                height = controller.height;
            }
            else if (root.TryGetComponent<CapsuleCollider>(out var capsule) && capsule.direction == 1)
            {
                center = capsule.center;
                radius = capsule.radius;
                height = capsule.height;
            }
            else
                return false;
            body.center = Vector3.Scale(center, scale);
            body.radius = radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            body.height = Mathf.Max(height * Mathf.Abs(scale.y), 2f * body.radius);
            return body.radius > 0f;
        }
    }

    [Serializable]
    public sealed class Attack
    {
        public AttackData data;
        public AnimationClip clip;
        public Body target;
        public List<Band> bands = new();
        public string note;
    }

    [Serializable]
    public sealed class Actor
    {
        public string source;
        public string sourceHash;
        public EnemyData enemyData;
        public PlayerAttackData openingAttack;
        public RuntimeAnimatorController controller;
        public Avatar avatar;
        public Vector3 scale;
        public string weapons;
        public Body body;
        public List<Attack> attacks = new();

        public Attack Find(AttackData data, Body target)
        {
            for (int i = 0, count = attacks.Count; i < count; i++)
            {
                Attack entry = attacks[i];
                if (entry.data == data && entry.target.Matches(target))
                    return entry;
            }
            return null;
        }
    }

    public Actor Find(EnemyData data, Transform root, PlayerAttackData opening = null)
    {
        Animator animator = root.GetComponentInChildren<Animator>();
        if (animator == null || !Body.TryRead(root, out Body body))
            return null;
        string weapons = WeaponSignature(root);
        for (int i = 0, count = actors.Count; i < count; i++)
        {
            Actor actor = actors[i];
            if (actor.enemyData == data && actor.controller == animator.runtimeAnimatorController
                && (data != null || actor.openingAttack == opening)
                && actor.avatar == animator.avatar && (actor.scale - root.lossyScale).sqrMagnitude < 0.000001f
                && actor.weapons == weapons && actor.body.Matches(body))
                return actor;
        }
        return null;
    }

    public static string WeaponSignature(Transform root)
    {
        var text = new StringBuilder();
        foreach (Weapon weapon in root.GetComponentsInChildren<Weapon>(true))
        {
            text.Append(weapon.HitGroup).Append(':').Append(weapon.HitCheckRadius.ToString(
                "R",
                CultureInfo.InvariantCulture));
            if (weapon.Slot != WeaponSlot.Any)
                text.Append("@slot=").Append((int)weapon.Slot);
            if (weapon.HitPoints == null)
                continue;
            foreach (Transform point in weapon.HitPoints)
            {
                if (point == null)
                {
                    text.Append("|missing");
                    continue;
                }
                var names = new Stack<string>();
                for (Transform t = point; t != null && t != root; t = t.parent)
                    names.Push(t.name);
                Vector3 p = point.localPosition;
                text.Append('|').Append(string.Join("/", names)).Append(':')
                    .Append(p.x.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(p.y.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(p.z.ToString("R", CultureInfo.InvariantCulture));
            }
            text.Append(';');
        }
        return text.ToString();
    }

}
