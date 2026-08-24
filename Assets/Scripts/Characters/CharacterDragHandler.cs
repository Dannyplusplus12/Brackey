using System.Collections.Generic;
using UnityEngine;

// Kéo-thả nhân vật bằng chuột, hoạt động ở MỌI state (kể cả đang Attacking) — trong
// lúc kéo, CharacterBase tự tạm dừng AI/di chuyển (xem CharacterBase.IsDragging).
// Thả ra: nếu KHÔNG trong wave thì vị trí đó thành home mới; nếu đang trong wave thì
// chỉ là dịch chuyển tạm thời, không đổi home (xử lý trong CharacterBase.EndDrag()).
//
// Dùng Physics2D.OverlapPoint chủ động mỗi frame thay vì OnMouseDown/Drag/Up của Unity
// để tránh phụ thuộc Camera.main bị null lúc Awake hoặc các ca lệch collider khó debug.
[RequireComponent(typeof(CharacterBase))]
[RequireComponent(typeof(Collider2D))]
public class CharacterDragHandler : MonoBehaviour
{
    // useTriggers = true để luôn bắt được collider Is Trigger, bất kể setting
    // "Queries Hit Triggers" toàn Project (Edit > Project Settings > Physics 2D) là gì.
    static readonly ContactFilter2D overlapFilter = new ContactFilter2D { useTriggers = true };
    static readonly List<Collider2D> overlapResults = new();

    CharacterBase character;
    Collider2D col;
    Camera cam;
    bool dragging;

    void Awake()
    {
        character = GetComponent<CharacterBase>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (character.IsDead) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;

        if (!dragging && Input.GetMouseButtonDown(0))
        {
            overlapResults.Clear();
            Physics2D.OverlapPoint(mouseWorld, overlapFilter, overlapResults);
            if (overlapResults.Contains(col))
            {
                dragging = true;
                character.BeginDrag();
            }
            return;
        }

        if (dragging && Input.GetMouseButton(0))
        {
            character.UpdateDrag(mouseWorld);
            return;
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            dragging = false;
            character.EndDrag();
        }
    }
}
