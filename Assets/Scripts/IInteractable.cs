/// <summary>
/// Oyundaki tüm etkileşimli nesnelerin ortak arayüzü (kapı, portal, dolap, gazete, NPC, şişe...).
/// PlayerInteraction en yakın IInteractable'ı bulur, aynı stilde "[E] ..." ipucunu gösterir
/// ve E tuşu / fare tıklamasıyla Interact() çağırır. Böylece tüm etkileşim butonları aynı görünür ve aynı çalışır.
/// </summary>
public interface IInteractable
{
    /// <summary>İpucu metni, ör. "Kapıdan geç". Boş/null dönerse nesne şu an etkileşime kapalıdır.</summary>
    string InteractPrompt { get; }
    void Interact();
}

/// <summary>Arayüz durumlarının tek yerden kontrolü.</summary>
public static class GameUI
{
    public const string Key = "<color=#FFD36B>[E]</color> ";

    /// <summary>Diyalog veya gazete açık: etkileşim ve hareket kapalı.</summary>
    public static bool InteractionBlocked =>
        DialogueUI.IsOpen || StoryCard.IsOpen || EndingCinematic.IsPlaying ||
        TitleScreen.IsOpen || PortalTransition.Active || (NewspaperUI.Instance != null && NewspaperUI.Instance.IsOpen);

    /// <summary>Karakter yürüyemez/zıplayamaz (dolap portalı açıkken de).</summary>
    public static bool MovementLocked => InteractionBlocked || CabinetPortalInteraction.AnyOpen;
}
