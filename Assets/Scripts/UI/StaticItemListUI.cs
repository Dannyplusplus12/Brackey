using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Danh sách item StatBoost đang sở hữu.
/// - Các item giống nhau được GOM lại thành 1 ô, góc dưới phải hiện "xN".
/// - Layout (Horizontal, Grid, v.v.) gán tuỳ ý trên Content GO trong Inspector.
/// - Dùng chung cho cả Shop (grid) và thanh ngang Arena.
///
/// Entry prefab cần có:
///   Root (+ StaticItemEntry + ItemTooltipTrigger)
///   ├── CardBg    (Image — nền thẻ, tuỳ chọn)
///   ├── Icon      (Image — icon item)           ← bắt buộc, đặt tên "Icon"
///   └── CountText (TMP_Text — hiện "x2", v.v.)  ← bắt buộc, đặt tên "CountText"
/// </summary>
public class StaticItemListUI : MonoBehaviour
{
    [SerializeField] Transform  content;

    [Tooltip("Prefab entry mới: Root có StaticItemEntry + ItemTooltipTrigger, chứa Icon + CountText")]
    [SerializeField] GameObject entryPrefab;

    [Tooltip("Fallback: prefab cũ chỉ là Image đơn (không có count text)")]
    [SerializeField] Image iconEntryPrefab;

    void OnEnable()
    {
        PlayerInventory.OnStaticItemsChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        PlayerInventory.OnStaticItemsChanged -= Refresh;
    }

    void Refresh()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        if (PlayerInventory.Instance == null) return;

        // Gom item giống nhau (ScriptableObject reference equality)
        var groups = new Dictionary<ItemData, int>();
        foreach (ItemData item in PlayerInventory.Instance.StaticItems)
        {
            groups.TryGetValue(item, out int c);
            groups[item] = c + 1;
        }

        foreach (var (item, count) in groups)
        {
            if (entryPrefab != null)
                SpawnEntry(item, count);
            else if (iconEntryPrefab != null)
                SpawnLegacyEntry(item, count);
        }
    }

    // ── Entry đầy đủ (prefab mới) ─────────────────────────────────────────────

    void SpawnEntry(ItemData item, int count)
    {
        GameObject go = Instantiate(entryPrefab, content);

        // Gán item vào StaticItemEntry → ItemTooltipTrigger tự đọc qua IItemSlot
        var entry = go.GetComponent<StaticItemEntry>()
                 ?? go.AddComponent<StaticItemEntry>();
        entry.SetItem(item);

        // Đảm bảo trigger tồn tại
        var trigger = go.GetComponent<ItemTooltipTrigger>()
                   ?? go.AddComponent<ItemTooltipTrigger>();
        trigger.Setup(TooltipDirection.Left, end: false, g: 8f);

        // Icon — raycastTarget=true để event bubble lên root's ItemTooltipTrigger
        Image iconImg = FindChild<Image>(go, "Icon") ?? go.GetComponentInChildren<Image>();
        if (iconImg != null)
        {
            iconImg.sprite         = item.icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget  = true;
        }

        // Count text — ẩn khi count = 1
        TMP_Text countTmp = FindChild<TMP_Text>(go, "CountText")
                         ?? go.GetComponentInChildren<TMP_Text>();
        if (countTmp != null)
        {
            countTmp.text = count > 1 ? $"x{count}" : "";
            countTmp.gameObject.SetActive(count > 1);
        }
    }

    // ── Fallback: prefab cũ chỉ là Image — tự thêm trigger + count badge động

    void SpawnLegacyEntry(ItemData item, int count)
    {
        Image entry          = Instantiate(iconEntryPrefab, content);
        entry.sprite         = item.icon;
        entry.preserveAspect = true;
        entry.raycastTarget  = true; // bắt buộc để nhận pointer event

        // StaticItemEntry (IItemSlot) — để ItemTooltipTrigger đọc item đúng
        var staticEntry = entry.GetComponent<StaticItemEntry>()
                       ?? entry.gameObject.AddComponent<StaticItemEntry>();
        staticEntry.SetItem(item);

        // ItemTooltipTrigger — thêm nếu prefab chưa có
        var trigger = entry.GetComponent<ItemTooltipTrigger>()
                   ?? entry.gameObject.AddComponent<ItemTooltipTrigger>();
        trigger.Setup(TooltipDirection.Left, end: false, g: 8f);

        if (count <= 1) return;

        // Tạo count text góc dưới phải
        var countGO = new GameObject("CountText", typeof(RectTransform));
        countGO.transform.SetParent(entry.transform, false);

        var rt              = countGO.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-2f, 2f);
        rt.sizeDelta        = new Vector2(52f, 26f);

        var tmp             = countGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text            = $"x{count}";
        tmp.fontSize        = 18f;
        tmp.fontStyle       = TMPro.FontStyles.Bold;
        tmp.color           = Color.white;
        tmp.alignment       = TMPro.TextAlignmentOptions.BottomRight;
        tmp.raycastTarget   = false;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    static T FindChild<T>(GameObject root, string childName) where T : Component
    {
        Transform t = root.transform.Find(childName);
        return t != null ? t.GetComponent<T>() : null;
    }
}
