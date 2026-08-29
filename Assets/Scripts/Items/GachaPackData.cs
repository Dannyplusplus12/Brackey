using UnityEngine;

// ScriptableObject cấu hình 1 pack gacha (spin wheel).
// Tạo: chuột phải trong Project → Items/Gacha Pack Data.
[CreateAssetMenu(fileName = "NewGachaPack", menuName = "Items/Gacha Pack Data")]
public class GachaPackData : ScriptableObject
{
    [Header("Display")]
    public string displayName;
    public Sprite icon;

    [Header("Wheel — đúng 6 Character ItemData theo thứ tự slot 0-5")]
    [Tooltip("Slot 0 ở vị trí 12h ban đầu, đi theo chiều kim đồng hồ")]
    public ItemData[] characterPool = new ItemData[6];

    [Header("Economy")]
    [Tooltip("Corn cần trả mỗi lần quay (0 = miễn phí)")]
    public int spinCost = 0;
}
