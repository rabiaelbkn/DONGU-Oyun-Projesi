using UnityEngine;

/// <summary>
/// DialogueData ağacı olan NPC (ör. geçmişteki Rabiş ve fabrika görevlisi Emirhan).
/// İpucu ve E tuşu ortak PlayerInteraction üzerinden gelir: "[E] Konuş".
/// Konuşma süresince NPC dolaşmayı bırakır ve oyuncuya döner.
/// </summary>
public class NPCDialogue : MonoBehaviour, IInteractable
{
    public DialogueData dialogue;
    [Tooltip("Diyalog kutusundaki portre. Boşsa NPC'nin kendi sprite'ı kullanılır.")]
    public Sprite portrait;
    [Tooltip("Etkileşim alanı (trigger) yarıçapı.")]
    public float talkRange = 1.8f;
    public string prompt = "Konuş";

    [Header("Görev Kontrolü")]
    [Tooltip("Sadece fabrika görevlisi Emirhan için aç.")]
    public bool requiresQuest = false;

    [Tooltip("Rabiş'in görev verdiğinde oluşturduğu bayrak.")]
    public string requiredQuestFlag = "gorev_alindi";

    [TextArea]
    public string questRequiredMessage =
        "Önce nehirdeki bilim insanıyla konuşmalıyım. " +
        "Burada ne olduğunu öğrenmeden fabrika görevlisine yaklaşmamalıyım.";

    [Tooltip("Görev alındıktan sonraki ilk konuşmada gösterilecek bölüm etiketi (boş = yok).")]
    public string chapterOnTalk = "";

    NPCWander2D wander;
    SpriteRenderer sr;

    public string InteractPrompt => dialogue != null ? prompt : null;

    void Awake()
    {
        wander = GetComponent<NPCWander2D>();
        sr = GetComponent<SpriteRenderer>();
        if (portrait == null && sr != null) portrait = sr.sprite;
    }

    public void Interact() => StartTalk();

    public void StartTalk()
    {
        if (dialogue == null) { Debug.LogWarning($"[NPCDialogue] {name}: diyalog atanmamış."); return; }

        // Rabiş görev vermeden Emirhan'la konuşulamaz.
        if (requiresQuest && !GameFlags.Has(requiredQuestFlag))
        {
            DialogueUI.Toast(questRequiredMessage, 5f);
            return;
        }

        if (!string.IsNullOrEmpty(chapterOnTalk) && GameFlags.Has(requiredQuestFlag)) StoryText.ChapterOnce(chapterOnTalk, 0.1f);
        var player = GameObject.Find("Player");
        if (wander)
        {
            wander.paused = true;
            if (player) wander.Face(player.transform.position.x);
        }
        DialogueUI.Get().Open(dialogue, portrait, () =>
        {
            if (wander) wander.paused = false;
        });
    }
}
