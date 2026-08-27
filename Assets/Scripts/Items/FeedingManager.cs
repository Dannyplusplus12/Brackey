using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Feed lần lượt từng ally với stagger delay.
// Sau khi xong fire OnFeedingComplete để GameManager biết.
public class FeedingManager : MonoBehaviour
{
    [Tooltip("Giây delay giữa 2 lần feed")]
    public float feedStagger = 0.4f;

    public static event System.Action OnFeedingComplete;
    public static event System.Action<CharacterBase, int, bool> OnFeedResult;

    readonly List<CharacterBase> feedBuffer = new();

    // Gọi từ GameManager.EnterArena()
    public void StartFeeding()
    {
        StartCoroutine(FeedAllCoroutine());
    }

    IEnumerator FeedAllCoroutine()
    {
        yield return null; // 1 frame để mọi thứ sẵn sàng

        if (PlayerWallet.Instance != null)
        {
            feedBuffer.Clear();

            // Ưu tiên thứ tự roster UI (player kéo thả để sắp xếp).
            // Fallback về CharacterGrid nếu UI chưa sẵn sàng.
            if (CharacterRosterUI.Instance != null)
            {
                feedBuffer.AddRange(CharacterRosterUI.Instance.GetFeedOrder());
            }
            else
            {
                var allies = CharacterGrid.GetAll(Faction.Ally);
                for (int i = 0; i < allies.Count; i++)
                    feedBuffer.Add(allies[i]);
            }

            foreach (CharacterBase ally in feedBuffer)
            {
                if (ally == null || ally.IsDead) continue;

                int  cost = ally.Stats.foodRequiredPerRound;
                bool fed  = PlayerWallet.Instance.TrySpend(cost);

                if (fed) ally.Feed();
                else     ally.SkipFeed();

                OnFeedResult?.Invoke(ally, cost, fed);

                if (feedStagger > 0f)
                    yield return new WaitForSeconds(feedStagger);
            }
        }

        OnFeedingComplete?.Invoke();
    }
}
