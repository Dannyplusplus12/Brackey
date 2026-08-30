using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị "Award: +X 🌽" cho wave sắp tới.
/// Đọc từ LevelData của level hiện tại (đã spawn vào scene khi vào Shop).
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
        LevelManager.OnHarderSpawned   += Refresh;
        StartCoroutine(RefreshDelayed());
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnStateChanged;
        LevelManager.OnHarderSpawned   -= Refresh;
    }

    void OnStateChanged(GameState state)
    {
        if (state == GameState.Shop) StartCoroutine(RefreshDelayed());
    }

    IEnumerator RefreshDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        Refresh();
    }

    void Refresh()
    {
        var data    = LevelManager.Instance?.GetCurrentLevelData();
        int base_   = data?.waveWinReward ?? GameManager.Instance?.waveWinReward ?? 5;
        bool harder = LevelManager.Instance != null && LevelManager.Instance.HarderWasUsed;
        int reward  = harder ? base_ * 3 : base_;

        if (awardText != null)
            awardText.text = $"Award: +{reward}";

        bool hasLevel = LevelManager.Instance == null || LevelManager.Instance.GetCurrentLevelData() != null;
        gameObject.SetActive(hasLevel);
    }
}
