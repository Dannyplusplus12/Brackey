using UnityEngine;

/// <summary>
/// Bridge nhỏ: gắn lên CardPanel trong GachaResultPopup để
/// ItemTooltipTrigger (cũng trên CardPanel) có thể đọc item qua IItemSlot.
///
/// GachaResultPopup thực hiện IItemSlot nhưng nằm ở GO cha,
/// nên cần component này trên CardPanel để bridge lại.
/// </summary>
public class GachaCardSlot : MonoBehaviour, IItemSlot
{
    [Tooltip("Tự gán bởi Setup Tool")]
    public GachaResultPopup popup;

    public ItemData GetCurrentItem() => popup != null ? popup.GetCurrentItem() : null;
    public bool IsSellable => false;
}
