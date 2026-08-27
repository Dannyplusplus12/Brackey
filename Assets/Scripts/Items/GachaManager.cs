using UnityEngine;

// Stub: chỉ giữ chỗ 2 pack, chưa có popup chọn nhân vật.
public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    [SerializeField] GachaPackData[] packs = new GachaPackData[2];

    public GachaPackData GetPack(int index) => packs[index];

    void Awake()
    {
        Instance = this;
    }

    public void OpenPack(int index)
    {
        Debug.Log($"[GachaManager] TODO: mở popup chọn nhân vật cho pack '{packs[index]?.displayName}'");
    }
}
