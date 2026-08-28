using System.Collections.Generic;
using UnityEngine;

// Random 4 item từ pool để bán trong Shop.
// Roll theo rarity: trước tiên chọn rarity tier (dựa trên rarityWeights),
// rồi pick ngẫu nhiên 1 item trong tier đó.
// Nếu tier trống → fallback về tier thấp hơn gần nhất.
//
// Mua: kiểm tra đủ corn (item.buyCost) → trừ corn → nhét vào PlayerInventory.
// Reroll: lần đầu mỗi wave miễn phí. Lần kế tiếp tốn rerollBaseCost, +1 mỗi lần bấm thêm.
public class ShopOfferManager : MonoBehaviour
{
    public static ShopOfferManager Instance { get; private set; }
    public static event System.Action OnOffersChanged;

    public const int OfferCount = 4;

    [SerializeField] List<ItemData> itemPool;

    [Tooltip("Corn cho lần reroll đầu tiên (bấm nút). Mỗi lần bấm thêm +1.")]
    [SerializeField] int rerollBaseCost = 4;

    int _rerollCost; // cost hiện tại, reset mỗi wave

    [Header("Rarity Weights")]
    [Tooltip("Xác suất tương đối (không cần tổng = 100). Thứ tự: Common, Uncommon, Rare, Epic, Legendary")]
    [SerializeField] float[] rarityWeights = { 60f, 25f, 10f, 4f, 1f };

    readonly ItemData[] currentOffers = new ItemData[OfferCount];

    // Cache theo rarity để roll nhanh
    readonly List<ItemData>[] poolByRarity = new List<ItemData>[5];

    public ItemData GetOffer(int index) => currentOffers[index];

    void Awake()
    {
        Instance = this;
        RebuildRarityCache();
    }

    void OnEnable()
    {
        GameManager.OnGameStateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState state)
    {
        if (state == GameState.Shop)
        {
            _rerollCost = rerollBaseCost; // reset mỗi wave
            RerollFree();
        }
    }

    void Start()
    {
        _rerollCost = rerollBaseCost;
        RerollFree(); // lần đầu không tốn corn
    }

    void RebuildRarityCache()
    {
        for (int i = 0; i < poolByRarity.Length; i++)
            poolByRarity[i] = new List<ItemData>();

        foreach (var item in itemPool)
            if (item != null)
                poolByRarity[(int)item.rarity].Add(item);
    }

    // ── Roll ─────────────────────────────────────────────────────────────────

    // Gọi từ Button OnClick trong Inspector
    public void RerollButton() => Reroll();

    // Reroll có tốn corn — gọi từ code
    public bool Reroll()
    {
        if (PlayerWallet.Instance != null && !PlayerWallet.Instance.TrySpend(_rerollCost))
            return false;
        _rerollCost++; // mỗi lần bấm tăng 1
        RerollFree();
        return true;
    }

    // Cost reroll hiện tại (để UI hiện số)
    public int CurrentRerollCost => _rerollCost;

    void RerollFree()
    {
        for (int i = 0; i < OfferCount; i++)
            currentOffers[i] = PickRandomItem();
        OnOffersChanged?.Invoke();
    }

    ItemData PickRandomItem()
    {
        if (itemPool.Count == 0) return null;

        // Bước 1: chọn rarity tier theo weight
        int tier = RollRarityTier();

        // Bước 2: tìm item trong tier đó, fallback xuống thấp hơn nếu trống
        for (int t = tier; t >= 0; t--)
        {
            var bucket = poolByRarity[t];
            if (bucket.Count > 0)
                return bucket[Random.Range(0, bucket.Count)];
        }

        // Fallback toàn bộ pool (đề phòng tier cao hơn cũng trống)
        return itemPool[Random.Range(0, itemPool.Count)];
    }

    int RollRarityTier()
    {
        // Đảm bảo mảng đủ 5 phần tử
        int len = Mathf.Min(rarityWeights.Length, 5);
        float total = 0f;
        for (int i = 0; i < len; i++) total += Mathf.Max(0f, rarityWeights[i]);

        if (total <= 0f) return 0; // tất cả weight = 0 → Common

        float roll = Random.Range(0f, total);
        float acc = 0f;
        for (int i = 0; i < len; i++)
        {
            acc += Mathf.Max(0f, rarityWeights[i]);
            if (roll < acc) return i;
        }
        return len - 1;
    }

    // ── Buy ──────────────────────────────────────────────────────────────────

    // Trả về true nếu mua thành công.
    public bool BuyOffer(int index)
    {
        ItemData item = currentOffers[index];
        if (item == null || PlayerInventory.Instance == null) return false;

        if (PlayerWallet.Instance != null && !PlayerWallet.Instance.TrySpend(item.buyCost))
            return false;

        bool bought;
        if (item.itemType == ItemType.Character)
        {
            var spawned = CharacterSpawner.Spawn(item.characterPrefab);
            bought = spawned != null;
            if (!bought)
                PlayerWallet.Instance?.Earn(item.buyCost);
        }
        else if (item.itemType == ItemType.Active)
        {
            bought = PlayerInventory.Instance.TryAddItem(item);
            if (!bought)
                PlayerWallet.Instance?.Earn(item.buyCost);
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
