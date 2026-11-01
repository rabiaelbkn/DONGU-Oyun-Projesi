using UnityEngine;

public class NewspaperInteractable : MonoBehaviour, IInteractable
{
    public string prompt = "Gazeteyi oku";

    public string InteractPrompt => prompt;

    public void Interact() => NewspaperUI.Get().OpenNewspaper();
}
