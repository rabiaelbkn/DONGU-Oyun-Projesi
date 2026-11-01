using UnityEngine;

/// <summary>Geçmiş: fabrikanın atık borusu. İncelenince ipucu bulunur (bayrak "fabrika_borusu_incelendi").</summary>
public class FactoryPipeClue : MonoBehaviour, IInteractable
{
    public const string Flag = "fabrika_borusu_incelendi";

    public string InteractPrompt => "Fabrika borusunu incele";

    public void Interact()
    {
        if (GameFlags.Has(Flag))
        {
            DialogueUI.Toast("Bu boruyu zaten inceledin.");
            return;
        }

        GameFlags.Set(Flag);
        StoryText.ChapterOnce(StoryText.ChClues, 0.1f);
        DialogueUI.Toast("İPUCU BULUNDU!\nFabrikanın borusundan nehre kirli atık akıyor!", 5f);
        Debug.Log("İPUCU BULUNDU: Fabrika borusu incelendi.");
    }
}
