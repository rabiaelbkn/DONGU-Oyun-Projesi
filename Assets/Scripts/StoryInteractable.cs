using UnityEngine;

public enum StoryCharacter
{
    Elif,
    AnneVeDeniz,
    Rabis,
    Zeynep,
    Emirhan
}

/// <summary>Hikâye NPC'si: konuşunca seçenekli sahne açılır (DialogueUI ile, Rabiş'in diyaloğuyla aynı kutu).</summary>
public class StoryInteractable : MonoBehaviour, IInteractable
{
    public StoryCharacter character;
    [Tooltip("Diyalog kutusundaki portre. Boşsa NPC'nin kendi sprite'ı kullanılır.")]
    public Sprite portrait;
    public string prompt = "Konuş";

    public string InteractPrompt => prompt;

    public void Interact()
    {
        if (DialogueManager.Instance == null) return;
        Sprite face = portrait;
        if (face == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) face = sr.sprite;
        }

        var wander = GetComponent<NPCWander2D>();
        var player = GameObject.Find("Player");
        if (wander != null)
        {
            wander.paused = true;
            if (player != null) wander.Face(player.transform.position.x);
        }
        DialogueManager.Instance.Open(StoryDialogueCatalog.Get(character), face, () =>
        {
            if (wander != null) wander.paused = false;
        });
    }
}
