using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private float moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (IsStoryUIOpen())
        {
            moveInput = 0f;
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsWalkingLeft", false);
            return;
        }

        moveInput = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput = 1f;
        }

        animator.SetBool("IsWalking", moveInput > 0f);
        animator.SetBool("IsWalkingLeft", moveInput < 0f);
    }

    void FixedUpdate()
    {
        if (IsStoryUIOpen())
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private bool IsStoryUIOpen()
    {
        return (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) ||
               (NewspaperUI.Instance != null && NewspaperUI.Instance.IsOpen) ||
               CabinetPortalInteraction.AnyOpen;
    }
}
