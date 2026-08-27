using UnityEngine;

// Loại item theo đối tượng áp dụng — dùng cho StatBoost và Active stat items.
// SpecialCondition items không cần field này (logic hoàn toàn custom trong subclass/handler).
public enum ItemTargetType
{
    AllCharacters,  // áp toàn bộ nhân vật
    SpecificType,   // áp riêng 1 loại (gán targetCharacterType)
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string displayName;
    public Sprite icon;
    public ItemType itemType;

    [TextArea]
    [Tooltip("Dùng cho box thông tin khi hover (chưa làm UI hover)")]
    public string description;

    [Tooltip("Corn cần để mua item này trong shop")]
    public int buyCost = 3;
    [Tooltip("Corn nhận lại khi bán item này")]
    public int sellValue = 1;

    // ── Character (itemType == Character) ────────────────────────────────────
    [Header("Character (chỉ dùng khi itemType = Character)")]
    [Tooltip("Prefab nhân vật sẽ được spawn vào SpawnArea khi mua")]
    public GameObject characterPrefab;

    // ── Stat Effect (StatBoost passive / Active stat buff khi kích hoạt) ─────
    // StatBoost : áp ngay khi mua, hoàn lại khi bán.
    // Active    : áp khi kích hoạt (handler tự quyết định tạm thời hay vĩnh viễn).
    // Special   : bỏ trống phần này — dùng custom logic trong ItemEffectHandler.
    [Header("Stat Effect (để trống nếu là Character / Special Condition item)")]
    public ItemTargetType targetType;

    [Tooltip("Chỉ dùng khi targetType = SpecificType")]
    public CharacterStats targetCharacterType;

    public StatDelta statDelta;
}
