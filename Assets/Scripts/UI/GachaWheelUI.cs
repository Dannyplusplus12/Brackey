using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Spin Wheel Gacha — wheel nằm cố định trong GachaPanel (ShopRoot lo ẩn/hiện).
///
/// ── Editor workflow ────────────────────────────────────────────────────────
/// 1. Tools > Gacha > Setup Gacha Wheel In Scene  → tạo hierarchy + 6 slots
/// 2. Kéo Slice Sprite vào field → 6 ô tự cập nhật ngay trong editor (OnValidate)
/// 3. Kéo Center Sprite vào CenterImage
///
/// ── Spin math ─────────────────────────────────────────────────────────────
/// 6 slots, local Z của slot i = −i×60°.
/// CW spin = giảm WheelContainer.eulerZ = −_totalDegreesCW.
/// Để slot i lên 12h: _totalDegreesCW ≡ (360 − i×60) mod 360.
/// </summary>
public class GachaWheelUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Sprites (kéo từ Project vào)")]
    [Tooltip("Sprite 1 ô rẻ quạt — đỉnh nhọn hướng vào tâm (xuống dưới)")]
    public Sprite sliceSprite;

    [Header("Nhân vật trong bánh xe (kéo ItemData vào đây)")]
    [Tooltip("Đúng 6 ItemData loại Character — theo thứ tự slot 0→5 (12h, 2h, 4h, 6h, 8h, 10h)")]
    public ItemData[] characterPool = new ItemData[6];

    [Header("Wheel Layout")]
    public float wheelRadius    = 220f;
    public float sliceWidth     = 220f;
    public float charImageSize  = 100f;
    [Tooltip("Khoảng từ tâm bánh xe tới giữa hình nhân vật (px)")]
    public float charOffset     = 130f;

    [Header("Spin Config")]
    public int   extraSpins        = 6;
    public float spinDuration      = 3.5f;
    [Tooltip("Giá Corn lần đầu tiên")]
    public int   baseCost          = 5;
    [Tooltip("Tăng thêm mỗi lần roll")]
    public int   costIncreasePerRoll = 5;

    [Header("References — tạo bởi Setup Tool, không gán tay")]
    public RectTransform  wheelContainer;
    public Image          centerImage;    // center sprite + là Button + cha của CostText
    public TMP_Text       spinCostText;   // con của centerImage
    public GachaResultPopup resultPopup;

    // ── Private ───────────────────────────────────────────────────────────────

    const int SlotCount = 6;
    GachaSlotUI[] _slots       = new GachaSlotUI[SlotCount];
    float         _totalCW     = 0f;
    bool          _isSpinning  = false;
    GachaPackData _currentPack;
    Button        _centerBtn;
    int           _rollCount = 0;   // số lần đã roll, reset khi vào Shop mới

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _centerBtn = centerImage != null ? centerImage.GetComponent<Button>() : null;
    }

    void OnEnable()
    {
        RefreshCharacterSprites();
        UpdateCostText();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Cập nhật slice sprite lên tất cả slot đã build sẵn — hiện ngay trong Editor
        if (wheelContainer == null) return;
        foreach (var slot in wheelContainer.GetComponentsInChildren<GachaSlotUI>(true))
            slot.UpdateSliceSprite(sliceSprite);
    }
#endif

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Đổi pack đang hiển thị (gọi từ GachaManager.OpenPack).</summary>
    public void OpenWheel(GachaPackData pack) => LoadPack(pack);

    /// <summary>Gán vào Button.onClick của centerImage qua Setup Tool.</summary>
    public void OnClickSpin()
    {
        if (_isSpinning) return;

        int spinCost = CurrentCost();
        if (spinCost > 0 && !PlayerWallet.Instance.TrySpend(spinCost))
        {
            Debug.Log("[GachaWheel] Không đủ Corn.");
            return;
        }

        StartCoroutine(SpinCoroutine(Random.Range(0, SlotCount)));
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    void LoadPack(GachaPackData pack)
    {
        _currentPack = pack;
        if (spinCostText != null)
            spinCostText.text = pack.spinCost > 0 ? $"{pack.spinCost} Corn" : "Free";
        RefreshCharacterSprites();
        UpdateCostText();
        SetCenterInteractable(true);
    }

    /// <summary>
    /// Cập nhật hình nhân vật lên 6 slot đã build sẵn.
    /// Đọc từ characterPool gán trực tiếp trong Inspector.
    /// </summary>
    void RefreshCharacterSprites()
    {
        if (wheelContainer == null) return;
        var found = wheelContainer.GetComponentsInChildren<GachaSlotUI>(true);
        for (int i = 0; i < SlotCount; i++)
        {
            if (i >= found.Length) break;
            _slots[i] = found[i];
            ItemData item = (characterPool != null && i < characterPool.Length) ? characterPool[i] : null;
            found[i].UpdateCharacter(item);
            found[i].UpdateSliceSprite(sliceSprite);
        }
    }

    // ── Spin coroutine ────────────────────────────────────────────────────────

    IEnumerator SpinCoroutine(int winnerIndex)
    {
        _isSpinning = true;
        SetCenterInteractable(false);

        float currentMod = _totalCW % 360f;
        float targetMod  = (360f - winnerIndex * (360f / SlotCount)) % 360f;
        float delta      = targetMod - currentMod;
        if (delta <= 0f) delta += 360f;

        float start = _totalCW;
        float end   = _totalCW + extraSpins * 360f + delta;
        float t     = 0f;

        while (t < spinDuration)
        {
            t += Time.deltaTime;
            float ease = EaseInOutCubic(Mathf.Clamp01(t / spinDuration));
            _totalCW = Mathf.Lerp(start, end, ease);
            if (wheelContainer != null)
                wheelContainer.localEulerAngles = new Vector3(0f, 0f, -_totalCW);
            yield return null;
        }

        _totalCW = end;
        if (wheelContainer != null)
            wheelContainer.localEulerAngles = new Vector3(0f, 0f, -_totalCW);

        // Highlight
        for (int i = 0; i < SlotCount; i++) _slots[i]?.SetHighlight(i == winnerIndex);
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < SlotCount; i++) _slots[i]?.SetHighlight(false);

        // Result popup
        ItemData won = (characterPool != null && winnerIndex < characterPool.Length)
                       ? characterPool[winnerIndex] : null;
        if (resultPopup != null)
            resultPopup.Show(won, OnReceive);
        else
            OnReceive(won);

        _isSpinning = false;
    }

    void OnReceive(ItemData item)
    {
        if (item?.characterPrefab != null) CharacterSpawner.Spawn(item.characterPrefab);
        _rollCount++;
        UpdateCostText();
        SetCenterInteractable(true);
    }

    int CurrentCost() => baseCost + _rollCount * costIncreasePerRoll;

    void UpdateCostText()
    {
        if (spinCostText != null)
            spinCostText.text = CurrentCost().ToString();
    }

    void SetCenterInteractable(bool on)
    {
        if (_centerBtn != null) _centerBtn.interactable = on;
    }

    static float EaseInOutCubic(float t) =>
        t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
}
