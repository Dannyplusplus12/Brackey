using UnityEngine;
using UnityEngine.UI;

// 1 trong 2 ô Gacha. packIndex khớp vị trí trong GachaManager - gán tay trong Inspector (0-1).
public class GachaPackSlotUI : MonoBehaviour
{
    [SerializeField] int packIndex;
    [SerializeField] Image icon;

    void Start()
    {
        GachaPackData pack = GachaManager.Instance != null ? GachaManager.Instance.GetPack(packIndex) : null;
        icon.sprite = pack != null ? pack.icon : null;
        icon.enabled = pack != null;
    }

    // Gán vào OnClick() của Button trên cùng GameObject trong Inspector.
    public void OnClickOpen()
    {
        GachaManager.Instance?.OpenPack(packIndex);
    }
}
