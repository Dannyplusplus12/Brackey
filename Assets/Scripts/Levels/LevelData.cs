using UnityEngine;

[System.Serializable]
public class EnemyGroup
{
    [Tooltip("Prefab enemy (có thể là bất kỳ prefab CharacterBase nào — LevelManager sẽ force faction Enemy)")]
    public GameObject prefab;

    [Tooltip("Số lượng enemy trong group này")]
    public int count = 1;

    [Tooltip("Delay giữa mỗi enemy khi spawn (giây). 0 = spawn tất cả cùng lúc")]
    public float spawnInterval = 0.2f;
}

// Dữ liệu 1 level: danh sách các nhóm enemy sẽ spawn khi vào Shop sau wave đó.
// Tạo asset: chuột phải trong Project → Create → Game → Level Data
[CreateAssetMenu(fileName = "Level_01", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Tooltip("Tên hiển thị (debug)")]
    public string levelName = "Level 1";

    [Tooltip("Các nhóm enemy spawn lần lượt từ trên xuống")]
    public EnemyGroup[] groups;

    [Header("Economy")]
    [Tooltip("Corn nhận được khi thắng wave này")]
    public int waveWinReward = 5;
}
