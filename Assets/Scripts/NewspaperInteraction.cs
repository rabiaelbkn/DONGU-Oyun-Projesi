using UnityEngine;

/// <summary>Masadaki gazete: E ile okunur. Geçmiş değiştiyse değişmiş sayfalar açılır.</summary>
public class NewspaperInteraction : MonoBehaviour, IInteractable
{
    public string prompt = "Gazeteyi oku";

    public string InteractPrompt => prompt;

    public void Interact() => NewspaperUI.Get().OpenNewspaper();
}
