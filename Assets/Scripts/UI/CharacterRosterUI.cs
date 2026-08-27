using System.Collections.Generic;
using UnityEngine;

// Tạo 1 entry cho mỗi nhân vật phe Ally đang có trong scene, gắn vào Content của ScrollView.
// Quét 1 lần lúc Start vì nhân vật sống xuyên suốt Shop/Arena (không despawn giữa 2 state).
// GetFeedOrder() trả về danh sách character theo thứ tự UI hiện tại (dùng cho FeedingManager).
public class CharacterRosterUI : MonoBehaviour
{
    public static CharacterRosterUI Instance { get; private set; }

    [SerializeField] CharacterRosterEntry entryPrefab;
    [SerializeField] Transform content;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        foreach (CharacterBase character in FindObjectsByType<CharacterBase>(FindObjectsSortMode.None))
        {
            if (character.Faction != Faction.Ally) continue;

            CharacterRosterEntry entry = Instantiate(entryPrefab, content);
            entry.Bind(character);
        }
    }

    // Trả về danh sách character theo thứ tự entry trong Content (trên → dưới).
    // Player kéo thả entry để đổi thứ tự → thứ tự feed thay đổi theo.
    public List<CharacterBase> GetFeedOrder()
    {
        var result = new List<CharacterBase>();
        for (int i = 0; i < content.childCount; i++)
        {
            var entry = content.GetChild(i).GetComponent<CharacterRosterEntry>();
            if (entry != null && entry.BoundCharacter != null && !entry.BoundCharacter.IsDead)
                result.Add(entry.BoundCharacter);
        }
        return result;
    }
}
