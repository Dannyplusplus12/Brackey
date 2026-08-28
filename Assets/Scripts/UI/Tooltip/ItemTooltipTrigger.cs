using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Thêm vào bất kỳ Unity UI slot nào để hiện tooltip khi hover.
///
/// ── Ví dụ cấu hình ────────────────────────────────────────────────────────
/// OfferSlot  : Direction=Right,  AlignEnd=false  (panel phải, top-aligned)
/// InvSlot    : Direction=Top,    AlignEnd=true   (panel trên, right-aligned)
/// StatBoost  : Direction=Top,    AlignEnd=true
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ItemTooltipTrigger : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Hướng panel hiện ra so với slot")]
    [SerializeField] TooltipDirection direction = TooltipDirection.Right;

    [Tooltip("false = canh cạnh đầu (trái/trên)\ntrue  = canh cạnh cuối (phải/dưới)")]
    [SerializeField] bool alignEnd = false;

    [Tooltip("Khoảng cách giữa 2 cạnh gần nhau nhất (pixel)")]
    [SerializeField] float gap = 8f;

    [Tooltip("Để trống → tự đọc từ IItemSlot trên cùng GO")]
    [SerializeField] ItemData staticItem;

    IItemSlot     _slot;
    RectTransform _rect;

    void Awake()
    {
        _slot = GetComponent<IItemSlot>();
        _rect = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var item = GetItem();
        if (item == null) return;

        // Character item → đọc description từ CharacterStats (1 nơi duy nhất)
        if (item.itemType == ItemType.Character && item.characterPrefab != null)
        {
            var stats = item.characterPrefab.GetComponent<CharacterBase>()?.Stats;
            if (stats != null)
            {
                CharacterStatBar.Instance?.ShowBase(stats);
                TooltipSystem.Show(TooltipData.FromCharacterStats(stats, _rect, direction, alignEnd, gap));
            }
            return;
        }

        // StatBoost / Active → hiện description như cũ
        TooltipSystem.Show(TooltipData.FromItem(item, _rect, direction, alignEnd, gap));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipSystem.Hide();
        CharacterStatBar.Instance?.Hide();
    }

    ItemData GetItem() => _slot != null ? _slot.GetCurrentItem() : staticItem;
}
