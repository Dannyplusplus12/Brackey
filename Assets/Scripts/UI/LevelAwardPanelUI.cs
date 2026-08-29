using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị "Award: +X 🌽" cho wave sắp tới.
/// Đọc từ LevelData của level TIẾP THEO (chưa spawn).
/// Ẩn khi không còn level nào.
///
/// Gắn lên AwardPanel trong ShopRoot.
/// </summary>
public class LevelAwardPanelUI : MonoBehaviour
{
    [SerializeField] TMP_Text awardText;
    [SerializeField] Image    cornIcon;   // optional — sprite icon corn

    void OnEnable()
    {
        GameManager.OnGameStateChanged += OnStateChanged;
        Refresh();
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState state)
    {
        if (state == GameState.Shop) Refresh();
    }

    void Refresh()
    {
        // Level tiếp theo = level sẽ đánh sau khi bấm "Bắt đầu Wave"
        // currentLevelIndex đã tăng khi vào Shop (AdvanceAndSpawn chạy rồi)
        // → GetCurrentLevelData() chính là level sắp đánh
        var data   = LevelManager.Instance?.GetCurrentLevelData();
        int reward = data?.waveWinReward
                     ?? GameManager.Instance?.waveWinReward
                     ?? 5;

        if (awardText != null)
            awardText.text = $"Award: +{reward}";

        // Ẩn panel nếu không còn level
        bool hasLevel = LevelManager.Instance == null || LevelManager.Instance.GetCurrentLevelData() != null;
        gameObject.SetActive(hasLevel);
    }
}
