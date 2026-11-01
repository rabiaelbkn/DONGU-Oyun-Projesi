using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Seçenekli diyalog ağacı. Inspector'dan düzenlenebilir.
/// Her düğümün metni, seçenekleri ve (isteğe bağlı) açtığı bayrak vardır.
/// Hangi düğümle başlanacağı "startRules" listesine göre belirlenir (ilk uyan kazanır).
/// </summary>
[CreateAssetMenu(menuName = "Dialogue/Dialogue Data", fileName = "Dialogue")]
public class DialogueData : ScriptableObject
{
    public string speakerName = "NPC";

    [System.Serializable]
    public class Choice
    {
        public string text;
        [Tooltip("Gidilecek düğüm id'si. Boş = diyaloğu bitir.")]
        public string next;
        [Tooltip("Bu bayrak varsa seçenek görünür (boş = her zaman).")]
        public string requireFlag;
        [Tooltip("Bu bayrak varsa seçenek gizlenir.")]
        public string hideIfFlag;
        [Tooltip("Seçilince açılan bayrak.")]
        public string setFlag;
        /// <summary>Kodla eklenen kontrol/etki (hikâye sahneleri için). false dönerse seçim iptal edilir.</summary>
        [System.NonSerialized] public System.Func<bool> onPick;
    }

    [System.Serializable]
    public class Node
    {
        public string id;
        [Tooltip("Boşsa speakerName kullanılır.")]
        public string speaker;
        [TextArea(2, 6)] public string text;
        [Tooltip("Düğüm açılınca set edilen bayrak.")]
        public string setFlag;
        [Tooltip("Düğüm açılınca ekranın üstünde gösterilen bildirim (görev vb.).")]
        public string toast;
        [Tooltip("Seçenek yoksa E ile gidilecek düğüm. Boş = bitir.")]
        public string next;
        public List<Choice> choices = new List<Choice>();
    }

    [System.Serializable]
    public class StartRule
    {
        public string requireFlag;
        public string nodeId;
    }

    public List<StartRule> startRules = new List<StartRule>();
    public List<Node> nodes = new List<Node>();

    public Node Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var n in nodes) if (n.id == id) return n;
        Debug.LogWarning($"[Dialogue] '{name}' içinde '{id}' düğümü bulunamadı.");
        return null;
    }

    public Node GetStartNode()
    {
        foreach (var r in startRules)
            if (string.IsNullOrEmpty(r.requireFlag) || GameFlags.Has(r.requireFlag))
            {
                var n = Find(r.nodeId);
                if (n != null) return n;
            }
        return nodes.Count > 0 ? nodes[0] : null;
    }
}
