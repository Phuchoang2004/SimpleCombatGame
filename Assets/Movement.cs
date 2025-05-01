using UnityEngine;
using System.Collections;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    
    [Header("Dash Settings")]
    public KeyCode dashKey = KeyCode.L;
    public float dashForce = 8f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;
    private bool dashEnabled = true;  // Can be disabled by CharacterAttack script
    
    [Header("Custom Controls")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.W;
    
    [Header("Ground Detection")]
    public LayerMask groundLayer;  // Assign the Ground layer in the inspector
    
    // Movement speed modifier during defense
    [Header("Defense Settings")]
    public float defenseMovementMultiplier = 0.5f; // Move at half speed while defending
    
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
    private Vector3 originalScale; // Store original scale
    private int facingDirection = 1; // 1 for right, -1 for left
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    public Color groundedColor = Color.green;
    public Color notGroundedColor = Color.red;
    
    // Reference to track the platform we're standing on
    private GameObject currentPlatform;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        attackScript = GetComponent<CharacterAttack>();
        audioManager = FindObjectOfType<AudioManager>();
        originalScale = transform.localScale; // Store the original scale
        lastDashTime = -dashCooldown; // Allow dashing immediately
        
        // // Set up physics collision matrix in code
        // SetupCollisionMatrix();
        
        if (showDebugInfo)
        {
            Debug.Log("<color=cyan>Movement script started with direct collision detection.</color>");
            Debug.Log("<color=yellow>Control Settings:</color> Left: " + leftKey + ", Right: " + rightKey + 
                      ", Jump: " + jumpKey + ", Dash: " + dashKey);
            Debug.Log("<color=yellow>Physics Settings:</color> Character should have Rigidbody2D with Continuous Collision");
            Debug.Log("<color=yellow>Layer Settings:</color> Using layer " + LayerMask.LayerToName(Mathf.RoundToInt(Mathf.Log(groundLayer.value, 2))) + " for ground detection");
        }
    }

    // // Setup the physics collision matrix so player only collides with ground layer
    // private void SetupCollisionMatrix()
    // {
    //     // Get the player's layer
    //     int playerLayer = gameObject.layer;
        
    //     // Iterate through all layers
    //     for (int i = 0; i < 32; i++)
    //     {
    //         // Skip if the layer name is empty
    //         if (string.IsNullOrEmpty(LayerMask.LayerToName(i)))
    //             continue;
                
    //         // If this is the ground layer, enable collision with player
    //         if (((1 << i) & groundLayer) != 0)
    //         {
    //             Physics2D.IgnoreLayerCollision(playerLayer, i, false);
    //             if (showDebugInfo)
    //                 Debug.Log($"<color=green>Enabling collision</color> between {LayerMask.LayerToName(playerLayer)} and {LayerMask.LayerToName(i)}");
    //         }
    //         // Otherwise, disable collision with player
    //         else
    //         {
    //             Physics2D.IgnoreLayerCollision(playerLayer, i, true);
    //             if (showDebugInfo)
    //                 Debug.Log($"<color=red>Ignoring collision</color> between {LayerMask.LayerToName(playerLayer)} and {LayerMask.LayerToName(i)}");
    //         }
    //     }
    // }
    
    private void Update()
    {
        // Get health component to check if character is dead
        CharacterHealth healthScript = GetComponent<CharacterHealth>();
        
        // Skip all movement if character is stunned or dead
        if (isStunned || (healthScript != null && healthScript.IsDead()))
        {
            // Just update animator parameters while stunned or dead
            if (animator)
            {
                animator.SetFloat("xVelocity", 0);
                animator.SetFloat("yVelocity", rb.linearVelocity.y);
            }
            return;
        }
        
        // Check if defending
        bool isDefending = attackScript && attackScript.IsDefending();
        
        // Custom key input handling (only if not attacking, dashing, or defending)
        float horizontalInput = 0f;
        if (!attackScript || (!attackScript.IsAttacking() && !isDashing && !isDefending))
        {
            if (Input.GetKey(leftKey)) horizontalInput -= 1f;
            if (Input.GetKey(rightKey)) horizontalInput += 1f;
        }
        
        // Set movement input (zero if defending)
        moveInput = isDefending ? 0f : horizontalInput;
        
        // Update facing direction based on movement input or existing direction
        if (moveInput > 0)
            facingDirection = 1;
        else if (moveInput < 0)
            facingDirection = -1;
        
        // Handle dash input - only if dash is enabled and not already attacking or defending
        bool dashInput = Input.GetKeyDown(dashKey);
        if (dashEnabled && dashInput && CanDash() && (!attackScript || (!attackScript.IsAttacking() && !attackScript.IsDefending())))
        {
            // Special case for dash attack (only if dash attack is enabled for this character)
            if (attackScript && attackScript.IsDashEnabled() && Input.GetKey(attackScript.attackKey))
            {
                StartDashAttack();
            }
            else
            {
                StartDash();
            }
        }
        
        // Only allow jumping if not attacking, dashing, or defending
        bool jumpInput = Input.GetKeyDown(jumpKey) && 
                        (!attackScript || !attackScript.IsAttacking()) && 
                        !isDashing &&
                        !isDefending;
        
        if (jumpInput && isGrounded)
        {
            Debug.Log("<color=green>JUMP SUCCESS!</color> Standing on: " + 
                (currentPlatform != null ? currentPlatform.name : "unknown platform"));
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = true;
        }
        else
        {
            Debug.LogWarning("<color=orange>JUMP FAILED!</color> Character not grounded.");
        }
        
        // Flip character while preserving original scale
        transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * facingDirection, originalScale.y, originalScale.z);
            
        // Update animator parameters - always using realized velocity values
        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("yVelocity", rb.linearVelocity.y);
        
        // End jump animation immediately when grounded, regardless of small y-position changes
        animator.SetBool("isJumping", !isGrounded && (isJumping || rb.linearVelocity.y > 0.1f));
    }
    
    private void FixedUpdate()
    {
        // Skip movement physics if stunned
        if (isStunned)
        {
            // Apply dampening to movement during stun
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.9f, rb.linearVelocity.y);
            return;
        }
        
        // Regular movement (if not dashing)
        if (!isDashing)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }
    }
    
    private bool CanDash()
    {
        // Prevent dashing while defending or after attacking
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
        
        // Play dash sound effect
        if (audioManager != null)
            audioManager.PlayDashSFX();
        
        // Use correct dash trigger parameter name
        animator.SetTrigger("Dash");
        
        // Execute dash
        StartCoroutine(DashRoutine(false));
        
        Debug.Log("<color=blue>DASH!</color> Direction: " + facingDirection);
    }
    
    public void StartDashAttack()
    {
        if (isDashing) return;
        
        isDashing = true;
        lastDashTime = Time.time;
        
        // Notify attack script about dash attack
        attackScript.DashAttack(facingDirection);
        
        // Execute dash with attack
        StartCoroutine(DashRoutine(true));
        
        Debug.Log("<color=magenta>DASH ATTACK!</color> Direction: " + facingDirection);
    }
    
    private IEnumerator DashRoutine(bool isAttacking)
    {
        // Apply dash force
        rb.linearVelocity = new Vector2(dashForce * facingDirection, 0);
        
        // Wait for dash duration
        yield return new WaitForSeconds(dashDuration);
        
        // End dash state
        isDashing = false;
        
        // Slow down after dash if not attacking
        if (!isAttacking)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);
        }
    }
    
    // Called when the character collider touches another collider
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // First check if the colliding object is on the ground layer
        if (!IsInGroundLayer(collision.gameObject.layer))
        {
            if (showDebugInfo)
                Debug.Log("<color=orange>NOT GROUND LAYER:</color> Collision with " + collision.gameObject.name + " ignored (layer: " + LayerMask.LayerToName(collision.gameObject.layer) + ")");
            return;
        }
        
        // Check if we collided with something below us (potential ground)
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // If the contact normal points roughly upward, we're standing on something
            if (contact.normal.y > 0.7f) // Threshold to detect "ground-like" collisions
            {
                // First ground contact - immediately end jumping state
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
                
                return; // Exit once we've found a ground-like collision
            }
        }
    }
    
    // Called when the character collider stops touching another collider
    private void OnCollisionExit2D(Collision2D collision)
    {
        // Only process if we're leaving our current platform and it's a ground object
        if (collision.gameObject == currentPlatform && IsInGroundLayer(collision.gameObject.layer))
        {
            isGrounded = false;
            currentPlatform = null;
            
            if (showDebugInfo)
                Debug.Log("<color=red>NOT GROUNDED!</color> Left platform: " + collision.gameObject.name);
        }
    }
    
    // Helper function to check if a layer is in the ground layer mask
    private bool IsInGroundLayer(int layer)
    {
        return ((1 << layer) & groundLayer) != 0;
    }
    
    // Visualize grounded state in the editor
    private void OnDrawGizmos()
    {
        // Draw a small indicator sphere at the character's feet
        Gizmos.color = isGrounded ? groundedColor : notGroundedColor;
        Gizmos.DrawSphere(transform.position + Vector3.down * 0.9f, 0.1f);
    }

    // Method to disable/enable dash feature (called from CharacterAttack script)
    public void SetDashEnabled(bool enabled)
    {
        dashEnabled = enabled;
        
        if (showDebugInfo)
        {
            Debug.Log($"<color={(enabled ? "green" : "red")}>DASH {(enabled ? "ENABLED" : "DISABLED")}</color> for {gameObject.name}");
        }
    }
    
    // Public method to set stun state (called from CharacterHealth)
    public void SetStunned(bool stunned)
    {
        isStunned = stunned;
        
        // Cancel dash if it was in progress
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
    
    // Public method to check if character is stunned
    public bool IsStunned()
    {
        return isStunned;
    }
}