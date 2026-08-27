using UnityEngine;

// Tạo 1 entry cho mỗi nhân vật phe Ally đang có trong scene, gắn vào Content của ScrollView.
// Quét 1 lần lúc Start vì nhân vật sống xuyên suốt Shop/Arena (không despawn giữa 2 state).
public class CharacterRosterUI : MonoBehaviour
{
    [SerializeField] CharacterRosterEntry entryPrefab;
    [SerializeField] Transform content;

    void Start()
    {
        foreach (CharacterBase character in FindObjectsByType<CharacterBase>(FindObjectsSortMode.None))
        {
            if (character.Faction != Faction.Ally) continue;

            CharacterRosterEntry entry = Instantiate(entryPrefab, content);
            entry.Bind(character);
        }
    }
}
