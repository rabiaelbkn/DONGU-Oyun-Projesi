using UnityEngine;

/// <summary>Evin dışındaki eski su deposu. Zeynep'le "birlikte su arayalım" seçildiyse burada su bulunur.</summary>
[RequireComponent(typeof(Collider2D))]
public class WaterTank : MonoBehaviour, IInteractable
{
    public HomeStoryDirector director;

    public string InteractPrompt =>
        GameFlags.Has(HomeStoryFlags.ZeynepTank) && !GameFlags.Has(HomeStoryFlags.TankFound)
            ? "Su deposunu kontrol et"
            : "Su deposuna bak";

    void Awake() { GetComponent<Collider2D>().isTrigger = true; }

    public void Interact()
    {
        if (director == null) director = FindFirstObjectByType<HomeStoryDirector>();
        if (director != null) director.OnTankChecked();
    }
}
