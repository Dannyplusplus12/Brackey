using UnityEngine;

// Overlay debug đơn giản bằng IMGUI (không cần Canvas/Text). Gắn vào 1 GameObject
// bất kỳ trong scene (VD: WaveManager). Muốn thêm thông tin debug mới, chỉ cần
// thêm 1 dòng GUILayout.Label(...) trong OnGUI().
public class DebugOverlay : MonoBehaviour
{
    [Header("Debug Spawn (nhấn E để spawn Enemy tại vị trí chuột)")]
    [SerializeField] CharacterBase enemyPrefab;

    // Phím 1/2 (cả hàng số lẫn numpad) để giảm/tăng Time.timeScale theo các mốc cố định.
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
            SpawnDebugEnemy();

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            SetTimeScaleIndex(timeScaleIndex - 1);

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            SetTimeScaleIndex(timeScaleIndex + 1);
    }

    void SetTimeScaleIndex(int index)
    {
        timeScaleIndex = Mathf.Clamp(index, 0, timeScalePresets.Length - 1);
        Time.timeScale = timeScalePresets[timeScaleIndex];
    }

    void SpawnDebugEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("DebugOverlay: chưa gán Enemy Prefab để spawn.");
            return;
        }

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        CharacterBase spawned = Instantiate(enemyPrefab, mouseWorld, Quaternion.identity);
        spawned.SetSpawnPosition(mouseWorld);
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 16;
        GUILayout.BeginArea(new Rect(10, 10, 320, 300), GUI.skin.box);

        GUILayout.Label(WaveManager.IsWaveActive ? "Wave: ACTIVE (combat)" : "Wave: SHOP (idle)");
        GUILayout.Label($"Time Scale: {Time.timeScale:0.##}x  (phím 1: chậm lại, 2: tăng tốc)");
        GUILayout.Label("Nhấn E: spawn Enemy tại vị trí chuột");

        // Thêm dòng debug mới ở đây, ví dụ:
        // GUILayout.Label($"FPS: {(1f / Time.unscaledDeltaTime):F0}");

        GUILayout.EndArea();
    }
}
