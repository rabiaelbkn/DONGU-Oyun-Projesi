using UnityEngine;

/// <summary>Geçmiş: kıyıdaki sarı uyarı tabelası. İncelenince ipucu bulunur (bayrak "uyari_tabelasi_incelendi").</summary>
public class WarningSignClue : MonoBehaviour, IInteractable
{
    public const string Flag = "uyari_tabelasi_incelendi";

    public string InteractPrompt => "Uyarı tabelasını incele";

    public void Interact()
    {
        GameFlags.Set(Flag);
        StoryText.ChapterOnce(StoryText.ChClues, 0.1f);
        DialogueUI.Toast("İPUCU BULUNDU!\nBu bölgede su kirliliği tehlikesi var!", 5f);
    }
}
