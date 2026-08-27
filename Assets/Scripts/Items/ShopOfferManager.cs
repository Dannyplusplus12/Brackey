using System.Collections.Generic;
using UnityEngine;

// Random 4 item từ pool để bán trong Shop.
// Mua: kiểm tra đủ corn (item.buyCost) → trừ corn → nhét vào PlayerInventory.
// Reroll: trả rerollCost corn, chạy miễn phí lần đầu (Start).
public class ShopOfferManager : MonoBehaviour
{
    public static ShopOfferManager Instance { get; private set; }
    public static event System.Action OnOffersChanged;

    public const int OfferCount = 4;

    [SerializeField] List<ItemData> itemPool;

    [Tooltip("Corn cần để reroll (lần reroll đầu tiên lúc Start miễn phí)")]
    [SerializeField] int rerollCost = 1;

    readonly ItemData[] currentOffers = new ItemData[OfferCount];

    public ItemData GetOffer(int index) => currentOffers[index];

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        RerollFree(); // lần đầu không tốn corn
    }

    // Reroll có tốn corn — gọi từ nút UI
    public bool Reroll()
    {
        if (PlayerWallet.Instance != null && !PlayerWallet.Instance.TrySpend(rerollCost))
            return false;
        RerollFree();
        return true;
    }

    void RerollFree()
    {
        for (int i = 0; i < OfferCount; i++)
            currentOffers[i] = itemPool.Count > 0 ? itemPool[Random.Range(0, itemPool.Count)] : null;
        OnOffersChanged?.Invoke();
    }

    // Trả về true nếu mua thành công.
    // Kiểm tra: đủ corn + còn slot (nếu Active).
    public bool BuyOffer(int index)
    {
        ItemData item = currentOffers[index];
        if (item == null || PlayerInventory.Instance == null) return false;

        // Kiểm tra corn trước khi làm gì
        if (PlayerWallet.Instance != null && !PlayerWallet.Instance.TrySpend(item.buyCost))
            return false;

        bool bought;
        if (item.itemType == ItemType.Character)
        {
            var spawned = CharacterSpawner.Spawn(item.characterPrefab);
            bought = spawned != null;
            if (!bought)
                PlayerWallet.Instance?.Earn(item.buyCost); // hoàn corn nếu spawn lỗi
        }
        else if (item.itemType == ItemType.Active)
        {
            bought = PlayerInventory.Instance.TryAddItem(item);
            if (!bought)
            {
                // Hoàn lại corn nếu slot đầy
                PlayerWallet.Instance?.Earn(item.buyCost);
            }
        }
        else
        {
            PlayerInventory.Instance.AddStaticItem(item);
            bought = true;
        }

        if (bought)
        {
            currentOffers[index] = null;
            OnOffersChanged?.Invoke();
        }
        return bought;
    }
}
