using UnityEngine;

public class DebugOverlay : MonoBehaviour
{
    [Header("Debug Spawn")]
    [Tooltip("Nhấn E: spawn tại vị trí chuột (force Enemy)")]
    [SerializeField] CharacterBase enemyPrefab;
    [Tooltip("Nhấn R: spawn tại vị trí chuột (force Ally)")]
    [SerializeField] CharacterBase allyPrefab;

    static readonly float[] timeScalePresets = { 0.25f, 0.5f, 1f, 2f, 4f };
    int timeScaleIndex = 2;

    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        Time.timeScale = timeScalePresets[timeScaleIndex];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            SpawnDebugChar(enemyPrefab, Faction.Enemy);

        if (Input.GetKeyDown(KeyCode.R))
            SpawnDebugChar(allyPrefab, Faction.Ally);

        if (Input.GetKeyDown(KeyCode.I))
            CharacterBase.DebugInvincible = !CharacterBase.DebugInvincible;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            SetTimeScaleIndex(timeScaleIndex - 1);

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            SetTimeScaleIndex(timeScaleIndex + 1);

        // Debug trên char đang được kéo
        CharacterBase dragged = CharacterDragHandler.CurrentlyDragged;
        if (dragged != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
                dragged.Heal(10f);
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
                dragged.TakeDamage(10f);
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
                dragged.DebugAddAngry(1f);
            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
                dragged.DebugAddAngry(-1f);
        }
    }

    void SetTimeScaleIndex(int index)
    {
        timeScaleIndex = Mathf.Clamp(index, 0, timeScalePresets.Length - 1);
        Time.timeScale = timeScalePresets[timeScaleIndex];
    }

    void SpawnDebugChar(CharacterBase prefab, Faction forceFaction)
    {
        if (prefab == null) { Debug.LogWarning($"DebugOverlay: chưa gán prefab cho {forceFaction}."); return; }
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        CharacterBase spawned = Instantiate(prefab, mouseWorld, Quaternion.identity);
        spawned.ForceSetFaction(forceFaction);
        spawned.SetSpawnPosition(mouseWorld);
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 14;
        GUILayout.BeginArea(new Rect(10, 10, 340, 500), GUI.skin.box);

        GUILayout.Label(WaveManager.IsWaveActive ? "Wave: ACTIVE" : "Wave: INACTIVE");

        // Hiển thị tốc độ game từ GameSpeedController nếu có
        if (GameSpeedController.Instance != null)
        {
            var gsc = GameSpeedController.Instance;
            string speedLabel = WaveManager.IsWaveActive
                ? $"Speed: {gsc.CurrentSpeed:0.##}x  (Space cycle)"
                : $"Speed: {gsc.CurrentSpeed:0.##}x  (chỉ active trong wave)";
            GUILayout.Label(speedLabel);
        }
        else
        {
            GUILayout.Label($"TimeScale: {Time.timeScale:0.##}x  (1: slow, 2: fast)");
        }

        int corn = PlayerWallet.Instance != null ? PlayerWallet.Instance.Corn : -1;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Corn: {corn}");
        if (GUILayout.Button("+10", GUILayout.Width(40)))
            PlayerWallet.Instance?.Earn(10);
        GUILayout.EndHorizontal();

        // --- Char diagnostics ---
        GUILayout.Space(6);
        int ally  = CharacterGrid.CountAlive(Faction.Ally);
        int enemy = CharacterGrid.CountAlive(Faction.Enemy);
        GUILayout.Label($"Ally alive: {ally}  |  Enemy alive: {enemy}");

        // Liệt kê từng char: tên, faction, state, stats null?
        GUILayout.Space(4);
        GUILayout.Label("── Characters ──");
        foreach (Faction f in new[] { Faction.Ally, Faction.Enemy })
        {
            var list = CharacterGrid.GetAll(f);
            foreach (var c in list)
            {
                if (c == null) continue;
                string statsOk = c.Stats != null ? "" : " [STATS NULL!]";
                string targetInfo = "no target";
                if (c.CurrentTarget != null)
                {
                    float dist = Vector2.Distance(c.transform.position, c.CurrentTarget.transform.position);
                    targetInfo = $"→{c.CurrentTarget.name} d={dist:F2}";
                }
                GUILayout.Label($"  [{f}] {c.name}  {c.State}  spd={c.DebugEffectiveMoveSpeed:F1}  {targetInfo}{statsOk}");
            }
        }

        // --- Sumo diagnostics ---
        bool hasSumo = false;
        foreach (Faction f in new[] { Faction.Ally, Faction.Enemy })
        {
            foreach (var c in CharacterGrid.GetAll(f))
            {
                if (c is not Sumo sumo) continue;
                if (!hasSumo) { GUILayout.Space(4); GUILayout.Label("── Sumo ──"); hasSumo = true; }
                GUILayout.Label($"  [{f}] {sumo.name}");
                GUILayout.Label($"    HP   base={sumo.Stats.maxHP:F0}  eff={sumo.MaxHP:F0}  |  Times fed: {sumo.FeedCount}");
                GUILayout.Label($"    Ally deaths: {sumo.AllyDeathCount}  |  DMG eff={sumo.LiveDamage:F1}  APS eff={sumo.EffectiveAPS:F2}");
                GUILayout.Label($"    Angry: {sumo.CurrentAngry:F0}/{sumo.Stats?.maxAngry:F0}");
            }
        }

        // --- NguoiSoi diagnostics ---
        bool hasWolf = false;
        foreach (Faction f in new[] { Faction.Ally, Faction.Enemy })
        {
            foreach (var c in CharacterGrid.GetAll(f))
            {
                if (c is not NguoiSoi wolf) continue;
                if (!hasWolf) { GUILayout.Space(4); GUILayout.Label("── Người Sói ──"); hasWolf = true; }
                GUILayout.Label($"  [{f}] {wolf.name}");
                GUILayout.Label($"    DMG  base={wolf.Stats.damage:F0}  temp+{wolf.FrenzyDmgPct*100:F0}%  → {wolf.LiveDamage:F1}");
                GUILayout.Label($"    APS  base={wolf.Stats.attackSpeed:F2}  temp+{wolf.FrenzyAPS:F2}spd  → {wolf.EffectiveAPS:F2}");
                GUILayout.Label($"    MoveSpd temp+{wolf.FrenzySpd:F0}  |  Frenzy hits: {wolf.FrenzyHits}");
                GUILayout.Label($"    Angry: {wolf.CurrentAngry:F0}/{wolf.Stats?.maxAngry:F0}");
            }
        }

        GUILayout.Space(6);
        if (CharacterBase.DebugInvincible)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("⚠ INVINCIBLE ON (I to toggle)");
            GUI.color = Color.white;
        }
        GUILayout.Label("E: spawn enemy  |  R: spawn ally  |  I: invincible toggle");
        GUILayout.Label("1: slow timescale  |  2: fast timescale");

        CharacterBase dragged = CharacterDragHandler.CurrentlyDragged;
        if (dragged != null)
        {
            GUILayout.Space(4);
            GUILayout.Label($"── Dragging: {dragged.name} ──");
            float hpPct  = dragged.MaxHP  > 0 ? dragged.CurrentHP    / dragged.MaxHP              * 100f : 0f;
            float angPct = dragged.Stats != null && dragged.Stats.maxAngry > 0
                           ? dragged.CurrentAngry / dragged.Stats.maxAngry * 100f : 0f;
            GUILayout.Label($"  HP:    {dragged.CurrentHP:F0}/{dragged.MaxHP:F0}  ({hpPct:F0}%)   [4] +10  [5] -10");
            GUILayout.Label($"  Angry: {dragged.CurrentAngry:F1}/{dragged.Stats?.maxAngry:F0}  ({angPct:F0}%)  [7] +1  [8] -1");
        }

        GUILayout.EndArea();
    }
}
