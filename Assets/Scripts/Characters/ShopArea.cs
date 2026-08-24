using UnityEngine;

// Điểm tham chiếu tối giản cho khu vực shop: nhân vật dùng Center + ClusterRadius
// để tự chọn vị trí đứng ngẫu nhiên lúc spawn. Không xử lý kinh tế/mua bán.
public class ShopArea : MonoBehaviour
{
    public static ShopArea Instance { get; private set; }

    [Tooltip("Bán kính cụm quanh tâm mà nhân vật sẽ tự chọn vị trí đứng khi spawn")]
    public float clusterRadius = 3f;

    public Vector2 Center => transform.position;

    void Awake()
    {
        Instance = this;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, clusterRadius);
    }
}
