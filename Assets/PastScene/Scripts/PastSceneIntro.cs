using System.Collections;
using UnityEngine;

/// <summary>Geçmiş sahnesine girişte kısa bir bildirim gösterir ve oyuncuyu doğma noktasına koyar.</summary>
public class PastSceneIntro : MonoBehaviour
{
    public Transform spawnPoint;
    [TextArea] public string message = "Geçmişe geldin... Nehrin kenarındaki bilim insanıyla konuş.";
    [TextArea] public string messageAfterProof = "Kanıt teslim edildi. Zaman kapısından geleceğe dönebilirsin.";

    IEnumerator Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p && spawnPoint)
        {
            p.transform.position = spawnPoint.position;
            var rb = p.GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = Vector2.zero;
        }
        yield return new WaitForSeconds(0.6f);
        DialogueUI.Toast(GameFlags.Has("kanit_teslim") ? messageAfterProof : message, 5f);
    }
}
