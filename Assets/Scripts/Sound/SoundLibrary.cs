using UnityEngine;

// ScriptableObject chứa mapping SoundId → AudioClip(s) + thông số phát.
// Tạo asset: chuột phải trong Project → Create → Sound → Sound Library
// Gán asset này vào SoundManager trong Inspector.
[CreateAssetMenu(menuName = "Sound/Sound Library", fileName = "SoundLibrary")]
public class SoundLibrary : ScriptableObject
{
    [System.Serializable]
    public class SoundEntry
    {
        public SoundId id;

        [Tooltip("Nhiều clip → random mỗi lần phát (tránh lặp đơn điệu).")]
        public AudioClip[] clips;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Pitch ngẫu nhiên trong khoảng [pitchMin, pitchMax].")]
        public float pitchMin = 0.95f;
        public float pitchMax = 1.05f;
    }

    [SerializeField] SoundEntry[] entries;

    // Tìm entry theo id. Trả null nếu chưa gán.
    public SoundEntry Get(SoundId id)
    {
        if (entries == null) return null;
        foreach (var e in entries)
            if (e.id == id) return e;
        return null;
    }
}
