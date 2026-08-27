using UnityEngine;

// Stub tối thiểu cho nút gacha - chưa có logic roll nhân vật/popup chọn.
[CreateAssetMenu(fileName = "NewGachaPack", menuName = "Items/Gacha Pack Data")]
public class GachaPackData : ScriptableObject
{
    public string displayName;
    public Sprite icon;
}
