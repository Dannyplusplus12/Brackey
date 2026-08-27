using UnityEngine;
using UnityEngine.UI;

// Danh sách item StatBoost đang sở hữu - chỉ để xem (đọc info khi hover sẽ làm sau).
// Dùng chung 1 script cho cả khung "All of static item" lúc Shop và thanh chạy ngang trên
// cùng lúc Arena - khác nhau chỉ ở Layout Group của "content" (Horizontal/Grid), gán trong Editor.
public class StaticItemListUI : MonoBehaviour
{
    [SerializeField] Transform content;
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

        foreach (ItemData item in PlayerInventory.Instance.StaticItems)
        {
            Image entry = Instantiate(iconEntryPrefab, content);
            entry.sprite = item.icon;
        }
    }
}
