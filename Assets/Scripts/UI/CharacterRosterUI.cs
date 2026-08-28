using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Tạo 1 entry cho mỗi nhân vật phe Ally.
// - Lúc Start: quét toàn scene (char có sẵn từ đầu).
// - Sau đó: subscribe CharacterSpawner.OnCharacterSpawned → thêm entry lên đầu roster.
// GetFeedOrder() trả về danh sách character theo thứ tự UI (dùng cho FeedingManager).
public class CharacterRosterUI : MonoBehaviour
{
    public static CharacterRosterUI Instance { get; private set; }

    [SerializeField] CharacterRosterEntry entryPrefab;
    [SerializeField] Transform content;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()  => CharacterSpawner.OnCharacterSpawned += HandleCharacterSpawned;
    void OnDisable() => CharacterSpawner.OnCharacterSpawned -= HandleCharacterSpawned;

    void Start()
    {
        SetupScrollFixes();

        // Char có sẵn trong scene khi game start (đặt tay trong editor)
        foreach (CharacterBase character in FindObjectsByType<CharacterBase>(FindObjectsSortMode.None))
        {
            if (character.Faction != Faction.Ally) continue;
            AddEntry(character, atTop: false);
        }
    }

    // Tự động gắn scroll fixes lên ScrollRect + Viewport — không cần làm tay trong Inspector.
    void SetupScrollFixes()
    {
        var scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (scrollRect == null) return;

        // 1. ViewportScrollInterceptor — intercept scroll events với chiều đúng
        //    + thêm Image trong suốt cho Viewport để catch raycasts vùng trống
        if (scrollRect.viewport != null &&
            scrollRect.viewport.GetComponent<ViewportScrollInterceptor>() == null)
        {
            scrollRect.viewport.gameObject.AddComponent<ViewportScrollInterceptor>();
        }

        // 2. RosterScrollbarSetup — tạo visual scrollbar (nếu chưa có)
        if (scrollRect.GetComponent<RosterScrollbarSetup>() == null)
            scrollRect.gameObject.AddComponent<RosterScrollbarSetup>();
    }

    // Gọi từ OnCharacterSpawned — char mới lên đầu roster
    void HandleCharacterSpawned(CharacterBase character)
    {
        if (character.Faction != Faction.Ally) return;
        AddEntry(character, atTop: true);
    }

    void AddEntry(CharacterBase character, bool atTop)
    {
        var entry = Instantiate(entryPrefab, content);
        entry.Bind(character);
        if (atTop) entry.transform.SetAsFirstSibling();
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
