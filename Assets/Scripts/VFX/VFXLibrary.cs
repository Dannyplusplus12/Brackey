using UnityEngine;

// ScriptableObject chứa refs đến tất cả particle prefabs trong game.
// Tạo 1 asset duy nhất, gán vào VFXManager trong scene.
// Chạy Tools > VFX > Create All Particle Prefabs để tạo prefab tự động.
[CreateAssetMenu(fileName = "VFXLibrary", menuName = "VFX/VFX Library")]
public class VFXLibrary : ScriptableObject
{
    [Header("Combat")]
    [Tooltip("Máu văng khi bị đánh. Burst count scale theo damage/maxHP.")]
    public GameObject bloodHit;

    [Tooltip("Máu + khói khi chết.")]
    public GameObject deathBurst;

    [Header("Buff / Heal")]
    [Tooltip("Mũi tên + line bay lên nhanh khi được buff/tăng chỉ số (maxHP, damage...).")]
    public GameObject buffArrow;

    [Tooltip("Particle hiện khi nhân vật được hồi máu (Heal).")]
    public GameObject healParticle;

    [Header("Feed")]
    [Tooltip("Icon vui bay lên chậm khi được feed đủ corn.")]
    public GameObject feedHappy;

    [Tooltip("Icon tức bay lên chậm khi bị skip feed.")]
    public GameObject feedAngry;

    [Header("Stun (loop — spawn làm child của nhân vật)")]
    [Tooltip("Ngôi sao xoay trên đầu khi bị choáng.")]
    public GameObject stunStars;

    [Header("Spawn")]
    [Tooltip("Sparkle nhỏ khi nhân vật được mua vào trận.")]
    public GameObject spawnPop;
}
