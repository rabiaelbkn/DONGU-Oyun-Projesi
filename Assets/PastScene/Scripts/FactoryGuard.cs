using UnityEngine;

/// <summary>
/// Emirhan'ın nöbet davranışı: ikna edilince ("guvenlik_gecildi") konuşma bittikten sonra
/// fabrikaya doğru yürüyüp gözden kaybolur. Sahneye tekrar gelindiğinde ikna edilmişse hiç görünmez.
/// </summary>
public class FactoryGuard : MonoBehaviour
{
    public string passFlag = "guvenlik_gecildi";
    [Tooltip("Emirhan'ın girdiği fabrika kapısının x konumu.")]
    public float factoryDoorX = 10.5f;
    [TextArea] public string leftText = "Emirhan vardiyasına dönüp fabrikaya girdi. Borunun başı artık boş: su örneğini alabilirsin!";

    bool leaving;

    void Start()
    {
        if (GameFlags.Has(passFlag)) Destroy(gameObject);
    }

    void Update()
    {
        if (leaving || !GameFlags.Has(passFlag) || DialogueUI.IsOpen) return;
        leaving = true;

        // Artık konuşulamaz; fabrikaya yürüyüp kaybolur.
        var talk = GetComponent<NPCDialogue>();
        if (talk) Destroy(talk);
        var walker = gameObject.AddComponent<StoryWalker>();
        walker.speed = 1.4f;
        walker.WalkTo(factoryDoorX, true, () => DialogueUI.Toast(leftText, 5f));
    }
}
