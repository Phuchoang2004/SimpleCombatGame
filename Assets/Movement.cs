using UnityEngine;
using System.Collections;

public class Movement : MonoBehaviour
{
    // Movement, dash, controls, ground, defense, stun, and debug settings
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    [Header("Dash Settings")]
    public KeyCode dashKey = KeyCode.L;
    public float dashForce = 8f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;
    private bool dashEnabled = true;
    [Header("Custom Controls")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.W;
    [Header("Ground Detection")]
    public LayerMask groundLayer;
    [Header("Defense Settings")]
    public float defenseMovementMultiplier = 0.5f;
    [Header("Stun Settings")]
    private bool isStunned = false;
    private Rigidbody2D rb;
    private Animator animator;
    private CharacterAttack attackScript;
    private AudioManager audioManager;
    private bool isJumping;
    public bool isGrounded;
    private bool isDashing;
    private float moveInput;
    private float lastDashTime;
    private Vector3 originalScale;
    private int facingDirection = 1;
    private float aiMoveInput = 0f;
    [Header("Debug")]
    public bool showDebugInfo = true;
    public Color groundedColor = Color.green;
    public Color notGroundedColor = Color.red;
    private GameObject currentPlatform;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        attackScript = GetComponent<CharacterAttack>();
        audioManager = FindObjectOfType<AudioManager>();
        originalScale = transform.localScale;
        lastDashTime = -dashCooldown;
        if (showDebugInfo)
        {
            Debug.Log("<color=cyan>Movement script started with direct collision detection.</color>");
            Debug.Log("<color=yellow>Control Settings:</color> Left: " + leftKey + ", Right: " + rightKey + ", Jump: " + jumpKey + ", Dash: " + dashKey);
            Debug.Log("<color=yellow>Physics Settings:</color> Character should have Rigidbody2D with Continuous Collision");
            Debug.Log("<color=yellow>Layer Settings:</color> Using layer " + LayerMask.LayerToName(Mathf.RoundToInt(Mathf.Log(groundLayer.value, 2))) + " for ground detection");
        }
    }

    private void Update()
    {
        CharacterHealth healthScript = GetComponent<CharacterHealth>();
        // Skip all movement if stunned or dead
        if (isStunned || (healthScript != null && healthScript.IsDead()))
        {
            if (animator)
            {
                animator.SetFloat("xVelocity", 0);
                animator.SetFloat("yVelocity", rb.linearVelocity.y);
            }
            return;
        }
        bool isDefending = attackScript && attackScript.IsDefending();
        float horizontalInput = 0f;
        if (!attackScript || (!attackScript.IsAttacking() && !isDashing && !isDefending))
        {
            if (aiMoveInput != 0f) // AI control
            {
                horizontalInput = aiMoveInput;
            }
            else // Player control
            {
                if (Input.GetKey(leftKey)) horizontalInput -= 1f;
                if (Input.GetKey(rightKey)) horizontalInput += 1f;
            }
        }
        moveInput = isDefending ? 0f : horizontalInput;
        if (moveInput > 0)
            facingDirection = 1;
        else if (moveInput < 0)
            facingDirection = -1;
        bool dashInput = Input.GetKeyDown(dashKey);
        if (dashEnabled && dashInput && CanDash() && (!attackScript || (!attackScript.IsAttacking() && !attackScript.IsDefending())))
        {
            if (attackScript && attackScript.IsDashEnabled() && Input.GetKey(attackScript.attackKey))
            {
                StartDashAttack();
            }
            else
            {
                StartDash();
            }
        }
        bool jumpInput = Input.GetKeyDown(jumpKey) && 
                        (!attackScript || !attackScript.IsAttacking()) && 
                        !isDashing &&
                        !isDefending;
        if (jumpInput && isGrounded)
        {
            Debug.Log("<color=green>JUMP SUCCESS!</color> Standing on: " + (currentPlatform != null ? currentPlatform.name : "unknown platform"));
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = true;
        }
        else
        {
            Debug.LogWarning("<color=orange>JUMP FAILED!</color> Character not grounded.");
        }
        transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * facingDirection, originalScale.y, originalScale.z);
        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
        animator.SetBool("isJumping", !isGrounded && (isJumping || rb.linearVelocity.y > 0.1f));
    }

    private void FixedUpdate()
    {
        if (isStunned)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.9f, rb.linearVelocity.y);
            return;
        }
        if (!isDashing)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }
    }

    private bool CanDash()
    {
        return !isDashing && 
               Time.time > lastDashTime + dashCooldown && 
               isGrounded &&
               (!attackScript || !attackScript.IsDefending());
    }

    public void StartDash()
    {
        if (isDashing) return;
        isDashing = true;
        lastDashTime = Time.time;
        if (audioManager != null)
            audioManager.PlayDashSFX();
        animator.SetTrigger("Dash");
        StartCoroutine(DashRoutine(false));
        Debug.Log("<color=blue>DASH!</color> Direction: " + facingDirection);
    }

    public void StartDashAttack()
    {
        if (isDashing) return;
        isDashing = true;
        lastDashTime = Time.time;
        attackScript.DashAttack(facingDirection);
        StartCoroutine(DashRoutine(true));
        Debug.Log("<color=magenta>DASH ATTACK!</color> Direction: " + facingDirection);
    }

    private IEnumerator DashRoutine(bool isAttacking)
    {
        rb.linearVelocity = new Vector2(dashForce * facingDirection, 0);
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
        if (!isAttacking)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsInGroundLayer(collision.gameObject.layer))
        {
            if (showDebugInfo)
                Debug.Log("<color=orange>NOT GROUND LAYER:</color> Collision with " + collision.gameObject.name + " ignored (layer: " + LayerMask.LayerToName(collision.gameObject.layer) + ")");
            return;
        }
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.7f)
            {
                if (!isGrounded && isJumping) 
                {
                    isJumping = false;
                    if (showDebugInfo)
                        Debug.Log("<color=cyan>LANDING!</color> Ending jump animation at Y=" + transform.position.y);
                }
                isGrounded = true;
                currentPlatform = collision.gameObject;
                if (showDebugInfo)
                    Debug.Log("<color=green>GROUNDED!</color> Now standing on: " + currentPlatform.name + " (layer: " + LayerMask.LayerToName(currentPlatform.layer) + ")");
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject == currentPlatform && IsInGroundLayer(collision.gameObject.layer))
        {
            isGrounded = false;
            currentPlatform = null;
            if (showDebugInfo)
                Debug.Log("<color=red>NOT GROUNDED!</color> Left platform: " + collision.gameObject.name);
        }
    }

    private bool IsInGroundLayer(int layer)
    {
        return ((1 << layer) & groundLayer) != 0;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? groundedColor : notGroundedColor;
        Gizmos.DrawSphere(transform.position + Vector3.down * 0.9f, 0.1f);
    }

    // Dash, stun, and state control methods
    public void SetDashEnabled(bool enabled)
    {
        dashEnabled = enabled;
        if (showDebugInfo)
        {
            Debug.Log($"<color={(enabled ? "green" : "red")}>DASH {(enabled ? "ENABLED" : "DISABLED")}</color> for {gameObject.name}");
        }
    }
    public void SetStunned(bool stunned)
    {
        isStunned = stunned;
        if (stunned && isDashing)
        {
            StopAllCoroutines();
            isDashing = false;
        }
        if (showDebugInfo)
        {
            Debug.Log($"<color={(stunned ? "red" : "green")}>{gameObject.name} is {(stunned ? "stunned!" : "no longer stunned.")}</color>");
        }
    }
    public bool IsStunned()
    {
        return isStunned;
    }

    public void SetAIMoveInput(float input)
    {
        aiMoveInput = input;
    }
}