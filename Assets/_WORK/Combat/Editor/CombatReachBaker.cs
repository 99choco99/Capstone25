using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>캐릭터의 실제 Override, Avatar, 무기 판정점으로 사전 측정합니다. 원본 에셋은 수정하지 않습니다.</summary>
[InitializeOnLoad]
public static class CombatReachBaker
{
    public const string AssetPath = "Assets/_WORK/Combat/Resources/CombatReachDatabase.asset";
    private const string Root = "Assets/_WORK";
    private static readonly string[] ActorRoots =
    {
        Root + "/Player/Prefabs",
        Root + "/Camera/Prefabs",
        Root + "/Enemies/Prefabs",
        Root + "/GameFlow/Prefabs",
        Root + "/DialogueQuest/Prefabs",
        Root + "/UI/Prefabs",
        Root + "/World/Prefabs/Map",
        Root + "/Combat/Prefabs/Weapons",
        Root + "/World/Scenes",
        Root + "/GameFlow/Scenes",
    };
    private const float SampleStep = 1f / 120f;
    private const float DistanceStep = 0.025f;
    private const string InputStampVersion = "reach-inputs-1|";
    private const string PendingKey = "CombatReachBaker.Pending";
    private static readonly string[] MeasurementScripts =
    {
        Root + "/Combat/Editor/CombatReachBaker.cs", Root + "/Combat/Scripts/Reach/CombatReachDatabase.cs",
        Root + "/Combat/Scripts/Attacks/AttackData.cs", Root + "/Combat/Scripts/Attacks/AttackHitWindow.cs",
        Root + "/Combat/Scripts/Weapons/Weapon.cs", Root + "/Enemies/Scripts/EnemyAttackData.cs",
        Root + "/Enemies/Scripts/EnemyData.cs", Root + "/Enemies/Scripts/EnemyStats.cs",
        Root + "/Player/Scripts/PlayerAttackData.cs",
        Root + "/Player/Scripts/Combat/PlayerCombat.cs",
        Root + "/Combat/Scripts/Animation/AnimationController.cs"
    };
    private static bool queued;
    private static bool baking;
    private static double nextBakeTime;

    private sealed class Source
    {
        public Transform root;
        public EnemyData data;
        public Animator animator;
        public PlayerAttackData first;
        public string path;
        public CombatReachDatabase.Actor profile;
    }
    private struct Sweep { public Vector3 from, to; public float radius; }

    static CombatReachBaker()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                Bake(false);
        };
        EditorApplication.delayCall += Initialize;
    }

    private static void Initialize()
    {
        if (SessionState.GetBool(PendingKey, false) || InputsChanged(LoadDatabase()))
            Queue();
    }

    public static void Queue()
    {
        SessionState.SetBool(PendingKey, true);
        nextBakeTime = EditorApplication.timeSinceStartup + 1d;
        if (queued)
            return;
        queued = true;
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode
            || baking || EditorApplication.timeSinceStartup < nextBakeTime)
            return;
        EditorApplication.update -= Update;
        queued = false;
        Bake(false);
    }

    [MenuItem("Tools/Combat/공격 거리 다시 측정")]
    public static void Rebuild() => Bake(true);

    public static bool Bake(bool force)
    {
        if (baking || EditorApplication.isPlaying)
            return false;
        baking = true;
        var scenes = new List<Scene>();
        try
        {
            CombatReachDatabase previous = LoadDatabase();
            if (!force && !SessionState.GetBool(PendingKey, false) && !InputsChanged(previous))
                return true;
            string[] paths = AssetDatabase.FindAssets("t:Prefab", ActorRoots)
                .Concat(AssetDatabase.FindAssets("t:Scene", ActorRoots))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsActorSource)
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();

            var sources = new List<Source>();
            // 프리팹을 먼저 등록하고 같은 설정의 씬 인스턴스는 중복 측정하지 않습니다.
            foreach (string path in paths.Where(path => path.EndsWith(".prefab", StringComparison.Ordinal)))
                Collect(AssetDatabase.LoadAssetAtPath<GameObject>(path), path, sources);
            foreach (string path in paths.Where(path => path.EndsWith(".unity", StringComparison.Ordinal)))
            {
                Scene scene = EditorSceneManager.OpenPreviewScene(path);
                scenes.Add(scene);
                foreach (GameObject root in scene.GetRootGameObjects())
                    Collect(root, path, sources);
            }
            var players = sources.Where(source => source.first != null).ToArray();
            if (players.Length == 0)
                throw new InvalidOperationException("플레이어의 첫 공격과 몸 캡슐을 찾을 수 없습니다.");
            var result = new List<CombatReachDatabase.Actor>();
            int measured = 0;
            foreach (Source source in sources)
            {
                var targets = sources.Where(other => (source.data == null) != (other.data == null))
                    .Select(other => other.profile.body).ToList();
                var distinctTargets = new List<CombatReachDatabase.Body>();
                foreach (var body in targets)
                    if (!distinctTargets.Exists(other => other.Matches(body)))
                        distinctTargets.Add(body);
                string targetHash = JsonUtility.ToJson(new BodyList { bodies = distinctTargets });
                source.profile.sourceHash += "|" + Hash128.Compute(targetHash)
                    + "|" + AssetDatabase.GetAssetDependencyHash(Root + "/Combat/Editor/CombatReachBaker.cs")
                    + "|" + AssetDatabase.GetAssetDependencyHash(Root + "/Combat/Scripts/Reach/CombatReachDatabase.cs");
                var old = previous != null ? previous.actors.Find(actor => SameActor(actor, source.profile)
                    && actor.sourceHash == source.profile.sourceHash) : null;
                if (!force && previous != null && previous.version == CombatReachDatabase.FormatVersion && old != null)
                {
                    result.Add(old);
                    continue;
                }

                IEnumerable<AttackData> attacks = source.data != null
                    ? source.data.pattern.Where(attack => attack != null).Cast<AttackData>().Distinct()
                    : new AttackData[] { source.first };
                foreach (AttackData attack in attacks)
                {
                    AnimationClip clip = ResolveClip(
                        source.animator.runtimeAnimatorController,
                        attack.animationStateName);
                    var sweeps = Sample(source, attack, out string note);
                    foreach (var target in distinctTargets)
                        source.profile.attacks.Add(new CombatReachDatabase.Attack
                        { data = attack, clip = clip, target = target, bands = Measure(sweeps, target), note = note });
                    measured++;
                }
                result.Add(source.profile);
            }
            CombatReachDatabase database = previous != null
                ? previous
                : ScriptableObject.CreateInstance<CombatReachDatabase>();
            database.version = CombatReachDatabase.FormatVersion;
            database.actors = result;
            database.sourcePaths = paths.SelectMany(path => AssetDatabase.GetDependencies(path, true))
                .Where(IsMeasurementInput).Concat(MeasurementScripts).Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal).ToList();
            database.sourceHashes = database.sourcePaths.Select(InputStamp).ToList();
            if (previous == null)
                AssetDatabase.CreateAsset(database, AssetPath);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssetIfDirty(database);
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= Update;
            queued = false;
            if (force || measured > 0)
                Debug.Log(
                    $"공격 거리 측정 완료: 캐릭터 {result.Count}개, 재측정 패턴 {measured}개. 원본 모션·무기 판정 기준이며 지형/이동 대상의 명중을 보장하지 않습니다.",
                    database);
            return true;
        }
        catch (Exception error)
        {
            // 실패한 갱신 뒤 이전 거리 자료를 최신 값으로 사용하지 않습니다.
            var stale = AssetDatabase.LoadAssetAtPath<CombatReachDatabase>(AssetPath);
            if (stale != null)
            {
                stale.version = 0;
                EditorUtility.SetDirty(stale);
                AssetDatabase.SaveAssetIfDirty(stale);
            }
            Debug.LogException(error);
            return false;
        }
        finally
        {
            foreach (Scene scene in scenes)
                if (scene.IsValid())
                    EditorSceneManager.ClosePreviewScene(scene);
            baking = false;
        }
    }

    [Serializable] private sealed class BodyList { public List<CombatReachDatabase.Body> bodies; }

    private static CombatReachDatabase LoadDatabase() => AssetDatabase.LoadAssetAtPath<CombatReachDatabase>(AssetPath);

    private static bool InputsChanged(CombatReachDatabase database)
    {
        if (database == null || database.version != CombatReachDatabase.FormatVersion
            || database.sourcePaths.Count == 0 || database.sourcePaths.Count != database.sourceHashes.Count)
            return true;
        for (int i = 0; i < database.sourcePaths.Count; i++)
            if (!database.sourceHashes[i].StartsWith(InputStampVersion, StringComparison.Ordinal)
                || database.sourceHashes[i] != InputStamp(database.sourcePaths[i]))
                return true;
        return false;
    }

    private static string InputStamp(string path)
    {
        // 재임포트 알림과 실제 입력 변경을 구분하고, 컴파일 후에도 저장된 입력과 비교합니다.
        var text = new StringBuilder(InputStampVersion);
        foreach (string filePath in new[] { path, path + ".meta" })
            text.Append(FileInputHash(filePath)).Append('|');
        if (path.EndsWith(".asset", StringComparison.Ordinal))
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset is AttackData || asset is EnemyData)
                text.Append(SerializedInput(asset));
        }
        return InputStampVersion + Hash128.Compute(text.ToString());
    }

    private static string FileInputHash(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists)
            return "missing";
        string key = "CombatReachBaker.File." + Hash128.Compute(path);
        string stamp = $"{file.Length}:{file.LastWriteTimeUtc.Ticks}";
        if (SessionState.GetString(key + ".stamp", "") != stamp)
        {
            using var stream = File.OpenRead(path);
            using var digest = SHA256.Create();
            SessionState.SetString(key + ".hash", Convert.ToBase64String(digest.ComputeHash(stream)));
            SessionState.SetString(key + ".stamp", stamp);
        }
        return SessionState.GetString(key + ".hash", "");
    }

    private static string SerializedInput(Object asset)
    {
        // 메모리의 객체 번호 대신 영구 GUID/fileID로 비교해 에디터 재시작에도 같은 입력을 유지합니다.
        var references = new Dictionary<int, string>();
        using var serialized = new SerializedObject(asset);
        SerializedProperty field = serialized.GetIterator();
        while (field.Next(true))
        {
            Object reference = field.propertyType == SerializedPropertyType.ObjectReference
                ? field.objectReferenceValue
                : null;
            if (reference != null
                && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                reference,
                out string guid,
                out long fileId))
                references[reference.GetInstanceID()] = $"\"guid\":\"{guid}\",\"fileID\":{fileId}";
        }
        return Regex.Replace(EditorJsonUtility.ToJson(asset), "\"instanceID\"\\s*:\\s*(-?\\d+)", match =>
            references.TryGetValue(int.Parse(match.Groups[1].Value), out string value) ? value : match.Value);
    }

    private static bool IsActorSource(string path)
    {
        if (!ActorRoots.Any(root => path.StartsWith(root + "/", StringComparison.Ordinal)))
            return false;
        return AssetDatabase.GetDependencies(path, true).Any(dependency =>
            dependency == Root + "/Enemies/Scripts/EnemyStats.cs"
            || dependency == Root + "/Player/Scripts/Combat/PlayerCombat.cs");
    }

    private static bool IsMeasurementInput(string path)
    {
        if (path == AssetPath)
            return false;
        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is ".prefab" or ".unity" or ".anim" or ".controller" or ".overridecontroller"
            or ".fbx" or ".blend" or ".dae" or ".obj")
            return true;
        if (extension == ".cs")
            return MeasurementScripts.Contains(path, StringComparer.Ordinal);
        if (extension != ".asset")
            return false;
        Object asset = AssetDatabase.LoadMainAssetAtPath(path);
        return asset is AttackData || asset is EnemyData || asset is Avatar;
    }

    public static void NotifyChangedAssets(IEnumerable<string> paths)
    {
        CombatReachDatabase database = LoadDatabase();
        foreach (string changed in paths)
        {
            string path = changed.EndsWith(".meta", StringComparison.Ordinal) ? changed[..^5] : changed;
            if (path == AssetPath)
            {
                if (database == null)
                {
                    Queue();
                    return;
                }
                continue;
            }
            int index = database != null ? database.sourcePaths.IndexOf(path) : -1;
            if (index >= 0 && (index >= database.sourceHashes.Count || database.sourceHashes[index] != InputStamp(path)))
            {
                Queue();
                return;
            }
            if (index >= 0)
                continue;
            // 새 캐릭터가 포함된 프리팹/씬도 측정 대상에 추가합니다.
            if ((path.EndsWith(".prefab", StringComparison.Ordinal) || path.EndsWith(".unity", StringComparison.Ordinal))
                && File.Exists(path) && IsActorSource(path))
            {
                Queue();
                return;
            }
        }
    }

    private static void Collect(GameObject root, string path, List<Source> sources)
    {
        if (root == null)
            return;
        foreach (EnemyStats stats in root.GetComponentsInChildren<EnemyStats>(true))
            Add(stats.transform, stats.Data, null, path, sources);
        foreach (PlayerCombat player in root.GetComponentsInChildren<PlayerCombat>(true))
            Add(player.transform, null, player.FirstAttackData, path, sources);
    }

    private static void Add(Transform root, EnemyData data, PlayerAttackData first, string path, List<Source> sources)
    {
        Animator animator = root.GetComponentInChildren<Animator>(true);
        if ((data == null && first == null) || animator == null || animator.runtimeAnimatorController == null
            || !CombatReachDatabase.Body.TryRead(root, out var body))
            return;
        var profile = new CombatReachDatabase.Actor
        {
            source = path,
            sourceHash = DependencyHash(path),
            enemyData = data,
            openingAttack = first,
            controller = animator.runtimeAnimatorController,
            avatar = animator.avatar,
            scale = root.lossyScale,
            weapons = CombatReachDatabase.WeaponSignature(root),
            body = body
        };
        if (sources.Exists(source => SameActor(source.profile, profile)))
            return;
        sources.Add(new Source
        {
            root = root,
            data = data,
            animator = animator,
            first = first,
            path = path,
            profile = profile
        });
    }

    private static bool SameActor(CombatReachDatabase.Actor a, CombatReachDatabase.Actor b)
        => a.enemyData == b.enemyData && a.openingAttack == b.openingAttack && a.controller == b.controller && a.avatar == b.avatar
        && (a.scale - b.scale).sqrMagnitude < 0.000001f && a.weapons == b.weapons && a.body.Matches(b.body);

    private static string DependencyHash(string path)
    {
        // 저장 직후에는 Unity의 import hash 갱신보다 빨리 Play/Build 검사가 실행될 수 있습니다.
        var text = new StringBuilder(AssetDatabase.GetAssetDependencyHash(path).ToString());
        foreach (string dependency in AssetDatabase.GetDependencies(path, true).OrderBy(
            value => value,
            StringComparer.Ordinal))
        {
            var file = new FileInfo(dependency);
            if (file.Exists)
                text.Append('|').Append(dependency).Append(':').Append(file.Length).Append(':').Append(file.LastWriteTimeUtc.Ticks);
            if (dependency.StartsWith(Root + "/Combat/Data/Attacks/", StringComparison.Ordinal)
                || dependency.StartsWith(Root + "/Combat/Data/ImpactProfiles/", StringComparison.Ordinal)
                || dependency.StartsWith(Root + "/Enemies/Data/Attacks/", StringComparison.Ordinal)
                || dependency.StartsWith(Root + "/Player/Data/Attacks/", StringComparison.Ordinal)
                || dependency.StartsWith(Root + "/Enemies/Data/Profiles/", StringComparison.Ordinal))
            {
                Object asset = AssetDatabase.LoadMainAssetAtPath(dependency);
                if (asset != null)
                    text.Append(SerializedInput(asset));
            }
        }
        return Hash128.Compute(text.ToString()).ToString();
    }

    public static AnimationClip ResolveClip(RuntimeAnimatorController runtime, string stateName)
    {
        var overrides = new List<AnimatorOverrideController>();
        while (runtime is AnimatorOverrideController value)
        {
            overrides.Add(value);
            runtime = value.runtimeAnimatorController;
        }
        if (runtime is not AnimatorController controller)
            throw new InvalidOperationException("Animator Controller가 없습니다.");
        var matches = new List<AnimatorState>();
        foreach (var layer in controller.layers)
            FindState(layer.stateMachine, layer.name, stateName, matches);
        if (matches.Count != 1 || matches[0].motion is not AnimationClip clip)
            throw new InvalidOperationException($"{controller.name}/{stateName}: 단일 클립 상태를 유일하게 찾을 수 없습니다.");
        for (int i = overrides.Count - 1; i >= 0; i--)
        {
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrides[i].GetOverrides(pairs);
            var pair = pairs.Find(pair => pair.Key == clip);
            if (pair.Value != null)
                clip = pair.Value;
        }
        return clip;
    }

    private static void FindState(AnimatorStateMachine machine, string path, string name, List<AnimatorState> matches)
    {
        foreach (var state in machine.states)
            if (state.state.name == name || path + "." + state.state.name == name)
                matches.Add(state.state);
        foreach (var child in machine.stateMachines)
            FindState(child.stateMachine, path + "." + child.stateMachine.name, name, matches);
    }

    private static List<Sweep> Sample(Source source, AttackData entry, out string note)
    {
        var sweeps = new List<Sweep>();
        note = "120Hz 원본 모션 / 보조 전진·가드 여유 제외";
        if (entry is EnemyAttackData retreat && retreat.PatternRole == EnemyAttackRole.Retreat)
        {
            note = "후퇴 후 재진입은 별도 준비 정책 사용";
            return sweeps;
        }
        var map = new Dictionary<Transform, Transform>();
        Scene samplingScene = EditorSceneManager.NewPreviewScene();
        Transform rig = CopyHierarchy(source.root, null, map, samplingScene);
        rig.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        rig.localScale = source.root.lossyScale;
        Animator animator = map[source.animator.transform].gameObject.AddComponent<Animator>();
        animator.avatar = source.animator.avatar;
        animator.applyRootMotion = true;
        animator.fireEvents = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        Vector3 animatorPosition = animator.transform.localPosition;
        Quaternion animatorRotation = animator.transform.localRotation;
        var visited = new HashSet<AttackData>();
        try
        {
            AttackData attack = entry;
            while (attack != null && visited.Add(attack))
            {
                AnimationClip clip = ResolveClip(source.animator.runtimeAnimatorController, attack.animationStateName);
                if (attack.ActiveStartFrame < 0f || attack.RecoveryStartFrame <= attack.ActiveStartFrame
                    || attack.RecoveryStartFrame / AttackData.FrameRate > clip.length + SampleStep)
                    throw new InvalidOperationException($"{entry.name}: 활성 프레임과 원본 클립 길이를 확인하세요.");
                var enemyAttack = attack as EnemyAttackData;
                var weapons = source.root.GetComponentsInChildren<Weapon>(true)
                    .Where(weapon => enemyAttack == null || weapon.HitGroup == enemyAttack.HitGroup).ToArray();
                var points = new List<(Transform point, float radius, WeaponSlot slot)>();
                foreach (Weapon weapon in weapons)
                    if (weapon.HitPoints != null)
                        foreach (Transform point in weapon.HitPoints)
                            if (point != null)
                                points.Add((map[point], weapon.HitCheckRadius, weapon.Slot));
                if (attack.HasHitWindow && points.Count == 0)
                    throw new InvalidOperationException($"{source.path}/{attack.name}: 무기 판정점이 없습니다.");
                float endFrame = attack.RecoveryStartFrame;
                if (!attack.HasHitWindow && enemyAttack != null)
                {
                    float speed = enemyAttack.AnimationSpeed * enemyAttack.GetPlaybackSpeed(1f);
                    endFrame = Mathf.Max(
                        enemyAttack.RecoveryStartFrame,
                        enemyAttack.NextComboStartFrame > 0f
                        ? enemyAttack.NextComboStartFrame
                        : enemyAttack.totalFrames - AttackData.FrameRate * speed * AnimationController.AttackTransitionDuration);
                    note = "준비→첫 타격 모션 연결 측정 / 전환 블렌딩 오차 여유 적용 필요";
                }
                var graph = PlayableGraph.Create("CombatReachBake");
                try
                {
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var playable = AnimationClipPlayable.Create(graph, clip);
                    playable.SetApplyFootIK(true);
                    var output = AnimationPlayableOutput.Create(graph, "Reach", animator);
                    output.SetSourcePlayable(playable);
                    graph.Play();
                    Vector3 position = rig.position;
                    Quaternion rotation = rig.rotation;
                    graph.Evaluate(0f);
                    RestoreRoot();
                    Vector3[] previous = points.Select(point => point.point.position).ToArray();
                    int[] previousWindow = Enumerable.Repeat(-1, points.Count).ToArray();
                    int steps = Mathf.CeilToInt(endFrame / AttackData.FrameRate / SampleStep);
                    for (int step = 1; step <= steps; step++)
                    {
                        float frame = step * SampleStep * AttackData.FrameRate;
                        graph.Evaluate(SampleStep);
                        Vector3 delta = animator.deltaPosition;
                        Vector3 forward = rotation * Vector3.forward;
                        float advance = Mathf.Max(0f, Vector3.Dot(delta, forward));
                        if (enemyAttack != null)
                            delta += forward * (advance * (enemyAttack.AdvanceMotionMultiplier - 1f));
                        position += delta;
                        rotation *= animator.deltaRotation;
                        RestoreRoot();
                        bool active = attack.HasHitWindow
                            && frame >= attack.ActiveStartFrame
                            && frame < attack.RecoveryStartFrame;
                        for (int i = 0; i < points.Count; i++)
                        {
                            Vector3 current = points[i].point.position;
                            int window = enemyAttack != null
                                ? enemyAttack.GetHitWindowIndex(frame, points[i].slot)
                                : (active
                                ? 0
                                : -1);
                            if (window >= 0)
                                sweeps.Add(new Sweep
                                {
                                    from = previousWindow[i] == window
                                    ? previous[i]
                                    : current,
                                    to = current,
                                    radius = points[i].radius
                                });
                            previousWindow[i] = window;
                            previous[i] = current;
                        }
                    }
                    void RestoreRoot()
                    {
                        if (animator.transform != rig)
                            animator.transform.SetLocalPositionAndRotation(animatorPosition, animatorRotation);
                        rig.SetPositionAndRotation(position, rotation);
                    }
                }
                finally { if (graph.IsValid()) graph.Destroy(); }
                if (attack.HasHitWindow)
                    return sweeps;
                if (enemyAttack == null || enemyAttack.RequireContactForCombo)
                    break;
                attack = enemyAttack.NextComboAttack;
            }
            note += " / 첫 타격 없음";
            return sweeps;
        }
        finally
        {
            Object.DestroyImmediate(rig.gameObject);
            EditorSceneManager.ClosePreviewScene(samplingScene);
        }
    }

    private static Transform CopyHierarchy(
        Transform original,
        Transform parent,
        Dictionary<Transform, Transform> map,
        Scene scene)
    {
        Transform copy = new GameObject(original.name) { hideFlags = HideFlags.HideAndDontSave }.transform;
        if (parent == null)
            SceneManager.MoveGameObjectToScene(copy.gameObject, scene);
        copy.SetParent(parent, false);
        copy.SetLocalPositionAndRotation(original.localPosition, original.localRotation);
        copy.localScale = original.localScale;
        map.Add(original, copy);
        foreach (Transform child in original)
            CopyHierarchy(child, copy, map, scene);
        return copy;
    }

    private static List<CombatReachDatabase.Band> Measure(List<Sweep> sweeps, CombatReachDatabase.Body body)
    {
        var bands = new List<CombatReachDatabase.Band>();
        if (sweeps.Count == 0)
            return bands;
        float max = sweeps.Max(sweep => Mathf.Max(sweep.from.z, sweep.to.z) + sweep.radius + body.radius + Mathf.Abs(body.center.z));
        float halfAxis = Mathf.Max(0f, body.height * 0.5f - body.radius);
        bool inBand = false;
        float start = 0f;
        int count = Mathf.CeilToInt(max / DistanceStep) + 1;
        for (int i = 0; i <= count; i++)
        {
            float distance = i * DistanceStep;
            Vector3 center = new Vector3(0f, 0f, -body.center.z) + new Vector3(-body.center.x, body.center.y, distance);
            bool hit = sweeps.Exists(sweep => SegmentDistanceSquared(sweep.from, sweep.to,
                center - Vector3.up * halfAxis, center + Vector3.up * halfAxis) <= Mathf.Pow(
                    sweep.radius + body.radius,
                    2f));
            if (hit && !inBand)
            {
                start = distance;
                inBand = true;
            }
            else if (!hit && inBand)
            {
                bands.Add(new CombatReachDatabase.Band(start, distance - DistanceStep));
                inBand = false;
            }
        }
        return bands;
    }

    // 선분-선분 최단 거리. 무기 SphereCast와 수직 몸 캡슐을 같은 공간에서 비교합니다.
    public static float SegmentDistanceSquared(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
    {
        Vector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
        float a = Vector3.Dot(d1, d1), e = Vector3.Dot(d2, d2), f = Vector3.Dot(d2, r);
        float s, t;
        if (a <= 0.0000001f && e <= 0.0000001f)
            return r.sqrMagnitude;
        if (a <= 0.0000001f)
        {
            s = 0f;
            t = Mathf.Clamp01(f / e);
        }
        else
        {
            float c = Vector3.Dot(d1, r);
            if (e <= 0.0000001f)
            {
                t = 0f;
                s = Mathf.Clamp01(-c / a);
            }
            else
            {
                float b = Vector3.Dot(d1, d2), denominator = a * e - b * b;
                s = denominator != 0f ? Mathf.Clamp01((b * f - c * e) / denominator) : 0f;
                t = (b * s + f) / e;
                if (t < 0f)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else if (t > 1f)
                {
                    t = 1f;
                    s = Mathf.Clamp01((b - c) / a);
                }
            }
        }
        return (p1 + d1 * s - p2 - d2 * t).sqrMagnitude;
    }
}

public sealed class CombatReachImportWatcher : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] oldPaths)
    {
        CombatReachBaker.NotifyChangedAssets(imported.Concat(deleted).Concat(moved).Concat(oldPaths));
    }
}

