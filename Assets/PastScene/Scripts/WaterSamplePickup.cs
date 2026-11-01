using UnityEngine;

/// <summary>
/// Borunun yanındaki numune şişesi.
/// Rabiş görevi verdiyse VE fabrika görevlisi Emirhan ikna edildiyse ("guvenlik_gecildi") E ile su örneği alınır.
/// İpucu ve E tuşu ortak PlayerInteraction üzerinden gelir.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WaterSamplePickup : MonoBehaviour, IInteractable
{
    public string questFlag = "gorev_alindi";
    public string sampleFlag = "ornek_alindi";
    public string guardFlag = "guvenlik_gecildi";

    [TextArea] public string lookText = "Su kahverengi ve köpüklü... Keskin bir kimyasal kokusu var. Belki nehrin yanındaki bilim insanı bu konuda bir şey biliyordur.";
    [TextArea] public string guardText = "Emirhan vananın başında ve seni görüyor! Şimdi örnek almaya çalışırsan yakalanırsın. Önce onunla konuşup ikna etmelisin.";
    [TextArea] public string takenText = "Su örneği alındı! Şimdi Rabiş'e götür.";

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        if (GameFlags.Has(sampleFlag) && sr) sr.enabled = false;
    }

    public string InteractPrompt
    {
        get
        {
            if (GameFlags.Has(sampleFlag)) return null;
            if (!GameFlags.Has(questFlag)) return "Suya bak";
            if (!GameFlags.Has(guardFlag)) return "Su örneğini almaya çalış";
            return "Su örneği al";
        }
    }

    public void Interact()
    {
        if (GameFlags.Has(sampleFlag)) return;

        // Rabiş henüz görev vermediyse
        if (!GameFlags.Has(questFlag)) { DialogueUI.Toast(lookText, 5f); return; }

        // Emirhan henüz ikna edilmediyse
        if (!GameFlags.Has(guardFlag)) { DialogueUI.Toast(guardText, 5f); return; }

        GameFlags.Set(sampleFlag);
        if (sr) sr.enabled = false;
        DialogueUI.Toast(takenText, 4f);
    }
}
