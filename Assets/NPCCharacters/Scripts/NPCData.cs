using UnityEngine;

/// <summary>Bir NPC'nin kimlik ve davranış ayarları. Sağ tık > Create > NPC > NPC Data ile yenisi oluşturulabilir.</summary>
[CreateAssetMenu(menuName = "NPC/NPC Data", fileName = "NPCData")]
public class NPCData : ScriptableObject
{
    [Header("Kimlik")]
    public string displayName = "NPC";
    public string role = "";
    [TextArea] public string line = "";

    [Header("Dolaşma")]
    public float walkSpeed = 0.8f;
    [Tooltip("Başlangıç noktasının sağına/soluna en fazla ne kadar uzaklaşabilir.")]
    public float wanderRadius = 2.5f;
    public Vector2 idleTimeRange = new Vector2(1.5f, 4f);
}
