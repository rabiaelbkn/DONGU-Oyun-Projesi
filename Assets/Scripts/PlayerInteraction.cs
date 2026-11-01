using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ana karakterin (HeroCharacter) tek etkileşim noktası.
/// En yakın IInteractable'ı bulur, ekranın altında "[E] ..." ipucunu gösterir (DialogueUI stili),
/// E tuşu veya fareyle tıklama ile etkileşir. Tüm sahnelerde aynı davranır.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Min(0.1f)] public float interactionRange = 1.5f;
    [Tooltip("Etkileşim aranacak katmanlar. Varsayılan: hepsi (IInteractable olmayanlar zaten yok sayılır).")]
    public LayerMask interactableLayer = ~0;

    private IInteractable current;
    private int lastBlockedFrame = -10;
    private readonly List<Collider2D> hits = new List<Collider2D>();

    private void Update()
    {
        if (GameUI.InteractionBlocked)
        {
            lastBlockedFrame = Time.frameCount;
            SetCurrent(null);
            return;
        }

        SetCurrent(FindNearest());

        // Diyaloğu/gazeteyi kapatan aynı E basışı yeni etkileşim açmasın.
        int sinceBlocked = Time.frameCount - lastBlockedFrame;
        if ((sinceBlocked >= 0 && sinceBlocked <= 1) || DialogueUI.JustClosed) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Camera.main != null)
        {
            Vector2 world = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Physics2D.OverlapPoint(world, Filter(), hits);
            foreach (var col in hits)
            {
                var target = col.GetComponentInParent<IInteractable>();
                if (target == null || string.IsNullOrEmpty(target.InteractPrompt)) continue;
                if (Vector2.Distance(col.ClosestPoint(transform.position), transform.position) > interactionRange * 2f) continue;
                target.Interact();
                return;
            }
        }

        if (current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            current.Interact();
    }

    private void LateUpdate()
    {
        // Metin duruma göre değişebilir (ör. dolap açıldıktan sonra "Geçide gir").
        if (current != null && !GameUI.InteractionBlocked) ShowHint(current);
    }

    private IInteractable FindNearest()
    {
        int count = Physics2D.OverlapCircle(transform.position, interactionRange, Filter(), hits);
        IInteractable best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var target = hits[i].GetComponentInParent<IInteractable>();
            if (target == null || string.IsNullOrEmpty(target.InteractPrompt)) continue;
            float d = ((Vector2)hits[i].ClosestPoint(transform.position) - (Vector2)transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = target; }
        }
        return best;
    }

    private ContactFilter2D Filter()
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(interactableLayer);
        filter.useTriggers = true;
        return filter;
    }

    private void SetCurrent(IInteractable target)
    {
        if (ReferenceEquals(target, current)) return;
        current = target;
        if (current == null) DialogueUI.HideHint(this);
        else ShowHint(current);
    }

    private void ShowHint(IInteractable target)
    {
        string prompt = target.InteractPrompt;
        if (string.IsNullOrEmpty(prompt)) { DialogueUI.HideHint(this); return; }
        DialogueUI.ShowHint(this, GameUI.Key + prompt);
    }

    private void OnDisable() => DialogueUI.HideHint(this);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
