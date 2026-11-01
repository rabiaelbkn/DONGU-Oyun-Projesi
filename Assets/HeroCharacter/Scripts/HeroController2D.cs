using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 2D platformer karakter kontrolcüsü.
/// Klavye: A/D veya ok tuşları = yürü, SHIFT (veya K) basılı tut = koş, Space/W/Yukarı = zıpla.
/// "Run Toggle" açıksa Shift/K bir kez basınca koşma açılır/kapanır.
/// Gamepad: sol çubuk, güney tuşu (A/X) = zıpla, sağ tetik = koş.
/// Mobil: UI butonlarından SetMobileMove / MobileJumpDown / MobileJumpUp / SetMobileRun çağırın.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class HeroController2D : MonoBehaviour
{
    [Header("Hareket")]
    public float walkSpeed = 2f;
    public float runSpeed = 6f;
    [Tooltip("Açıksa koşma tuşu basılı tutulmaz, bir kez basınca açılır/kapanır.")]
    public bool runToggle = false;
    public float acceleration = 60f;
    public float deceleration = 70f;

    [Header("Zıplama")]
    public float jumpVelocity = 12f;
    [Tooltip("Tuş erken bırakılırsa yükselme hızı bu oranla kesilir (değişken zıplama yüksekliği).")]
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
    [Tooltip("Platformdan düştükten sonra hâlâ zıplayabileceği süre.")]
    public float coyoteTime = 0.1f;
    [Tooltip("Yere değmeden hemen önce basılan zıplamayı hatırlama süresi.")]
    public float jumpBufferTime = 0.12f;

    [Tooltip("Diyalog vb. sırasında true yapılır: karakter yürümez, zıplamaz.")]
    public bool inputLocked;

    [Header("Zemin")]
    public LayerMask groundLayers = ~0;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int VelocityYHash = Animator.StringToHash("VelocityY");
    static readonly int AnimSpeedHash = Animator.StringToHash("AnimSpeed");

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    ContactFilter2D groundFilter;

    float moveInput;
    bool runHeld, jumpHeld;
    float lastGroundedTime = -10f, lastJumpPressedTime = -10f;
    bool grounded, isJumping, runToggled;

    // mobil girişler
    float mobileMove; bool mobileRun, mobileJumpHeld, mobileJumpPressed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayers);
        groundFilter.SetNormalAngle(45f, 135f); // sadece yukarı bakan yüzeyler zemin sayılır
    }

    void Update()
    {
        ReadInput();

        if (moveInput > 0.01f) sr.flipX = false;
        else if (moveInput < -0.01f) sr.flipX = true;

        Vector2 v = Velocity;
        anim.SetFloat(SpeedHash, Mathf.Abs(v.x) / runSpeed);
        anim.SetBool(GroundedHash, grounded);
        anim.SetFloat(VelocityYHash, v.y);
        float ax = Mathf.Abs(v.x);
        float refSpeed = ax > (walkSpeed + runSpeed) * 0.5f ? runSpeed : walkSpeed;
        anim.SetFloat(AnimSpeedHash, Mathf.Clamp(ax / refSpeed, 0.5f, 1.3f));
    }

    void FixedUpdate()
    {
        grounded = rb.IsTouching(groundFilter);
        if (grounded) lastGroundedTime = Time.time;

        Vector2 v = Velocity;

        float target = moveInput * (runHeld ? runSpeed : walkSpeed);
        float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
        v.x = Mathf.MoveTowards(v.x, target, rate * Time.fixedDeltaTime);

        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        bool wantsJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
        if (canJump && wantsJump)
        {
            v.y = jumpVelocity;
            lastJumpPressedTime = -10f;
            lastGroundedTime = -10f;
            grounded = false;
            isJumping = true;
        }
        else if (isJumping && !jumpHeld && v.y > 0f)
        {
            v.y *= jumpCutMultiplier; // tek seferlik kesme
            isJumping = false;
        }
        if (v.y <= 0f) isJumping = false;

        Velocity = v;
    }

    void ReadInput()
    {
        float x = 0f; bool run = false, runDown = false, jumpDown = false, jumpNow = false;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
            run |= k.leftShiftKey.isPressed || k.rightShiftKey.isPressed || k.kKey.isPressed;
            runDown |= k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame || k.kKey.wasPressedThisFrame;
            jumpDown |= k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame;
            jumpNow |= k.spaceKey.isPressed || k.wKey.isPressed || k.upArrowKey.isPressed;
        }
        var g = Gamepad.current;
        if (g != null)
        {
            float sx = g.leftStick.x.ReadValue();
            if (Mathf.Abs(sx) > 0.2f) x = sx;
            run |= g.rightTrigger.ReadValue() > 0.5f || g.buttonWest.isPressed;
            runDown |= g.buttonWest.wasPressedThisFrame;
            jumpDown |= g.buttonSouth.wasPressedThisFrame;
            jumpNow |= g.buttonSouth.isPressed;
        }
#else
        x = Input.GetAxisRaw("Horizontal");
        run = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.K);
        runDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.K);
        jumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        jumpNow = Input.GetButton("Jump") || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
#endif
        if (Mathf.Abs(mobileMove) > 0.01f) x = mobileMove;
        run |= mobileRun;
        jumpDown |= mobileJumpPressed;
        jumpNow |= mobileJumpHeld;
        mobileJumpPressed = false;

        if (inputLocked || GameUI.MovementLocked) { x = 0f; run = false; jumpDown = false; jumpNow = false; }
        moveInput = Mathf.Clamp(x, -1f, 1f);
        if (runToggle) { if (runDown) runToggled = !runToggled; runHeld = runToggled || mobileRun; }
        else runHeld = run;
        jumpHeld = jumpNow;
        if (jumpDown) lastJumpPressedTime = Time.time;
    }

    // ---- Mobil UI butonları için ----
    public void SetMobileMove(float x) => mobileMove = x;
    public void SetMobileRun(bool on) => mobileRun = on;
    public void MobileJumpDown() { mobileJumpPressed = true; mobileJumpHeld = true; }
    public void MobileJumpUp() => mobileJumpHeld = false;

    Vector2 Velocity
    {
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
    }
}
