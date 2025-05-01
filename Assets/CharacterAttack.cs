using UnityEngine;
using System.Collections;

public class CharacterAttack : MonoBehaviour
{
    [Header("Character Type")]
    public bool enableDashAttack = true;  // Set to false to disable dash attack for this character
    public string characterID = "default"; // Used to identify character type for settings

    [Header("Attack Settings")]
    public KeyCode attackKey = KeyCode.J;
    public float attackDuration = 0.2f;          // Fixed duration of each attack
    public float comboDelay = 0.1f;              // Delay before next combo can be executed
    public int maxCombo = 3;
    public float comboResetTime = 1.5f;         // Time before combo resets
    
    [Header("Dash Attack Settings")]
    public float dashAttackDuration = 0.3f;       // Duration of dash attack
    public float dashAttackDamage = 30f;          // Damage for dash attack
    public float dashAttackWidth = 1.4f;          // Width of dash attack hitbox
    public float dashAttackHeight = 1.0f;         // Height of dash attack hitbox
    
    [Header("Defense Settings")]
    public KeyCode defenseKey = KeyCode.S;
    public float defenseStaminaCost = 1f;        // Stamina cost per second while defending
    public float maxStamina = 100f;              // Maximum stamina
    public float staminaRecoveryRate = 15f;      // Stamina recovery per second when not defending
    public float damageReductionPercent = 75f;   // Percent of damage reduced when defending from the front
    public float currentStamina;                 // Current stamina level
    private bool staminaDepleted = false;        // Flag to prevent defense when stamina is depleted
    
    [Header("Damage Settings")]
    public float[] comboDamage = { 10f, 15f, 25f }; // Damage for each combo hit
    
    private Animator animator;
    private Movement movement;
    private bool isAttacking = false;
    private bool isDashAttacking = false;
    private bool isDefending = false;
    private int currentCombo = 0;
    private float lastAttackTime = 0f;
    private float lastDefenseTime = 0f;
    private Coroutine comboResetCoroutine;
    
    // Attack hitbox
    [Header("Hit Detection")]
    public Transform attackPoint;
    public float attackWidth = 1.0f;        // Width of the square hitbox
    public float attackHeight = 0.5f;       // Height of the square hitbox
    public LayerMask enemyLayers;

    // Audio reference
    private AudioManager audioManager;

    void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<Movement>();
        currentStamina = maxStamina;
        
        // Get reference to AudioManager
        audioManager = FindObjectOfType<AudioManager>();
        
        // Inform Movement script if dash is disabled
        if (!enableDashAttack && movement != null)
        {
            movement.SetDashEnabled(false);
        }
        
        // Log character configuration
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>Character [{characterID}] initialized</color> - Dash Attack: {(enableDashAttack ? "Enabled" : "Disabled")}");
            Debug.Log($"<color=cyan>Attack Power:</color> Combo1={comboDamage[0]}, Combo2={comboDamage[1]}, Combo3={comboDamage[2]}, Dash={dashAttackDamage}");
        }
    }

    void Update()
    {
        // Calculate time since last attack for delay enforcement
        float timeSinceLastAttack = Time.time - lastAttackTime;
        
        // Only allow attacks when grounded, not defending, not currently attacking,
        // and minimum time since last attack has passed
        bool canAttack = !isDefending && !isAttacking && !isDashAttacking && 
                         timeSinceLastAttack >= comboDelay;
        
        // Check if character is grounded before allowing attack
        if (movement && movement.isGrounded && Input.GetKeyDown(attackKey))
        {
            if (canAttack)
            {
                TryAttack();
            }
            else if (showDebugInfo && timeSinceLastAttack < comboDelay)
            {
                // Show debug message if trying to attack too quickly
                Debug.Log($"<color=orange>Attack denied:</color> Need to wait {(comboDelay - timeSinceLastAttack).ToString("F2")}s more");
            }
        }
        
        // Handle defense input - continuously check for defense key press
        if(Input.GetKey(defenseKey) && !isAttacking && !isDashAttacking && !staminaDepleted)
        {
            // Only call StartDefense once when we first start defending
            if (!isDefending)
            {
                StartDefense();
            }
        }
        else if(isDefending)
        {
            // Stop defense when key is released or stamina depleted
            StopDefense();
        }
        
        // Check for hit detection during defense
        if (isDefending)
        {
            DefenseHitDetection();
        }
        
        // Handle stamina
        ManageStamina();
    }
    
    // Manage stamina costs and recovery
    void ManageStamina()
    {
        if (isDefending)
        {
            // Consume stamina while defending
            currentStamina -= defenseStaminaCost * Time.deltaTime;
            
            // Check if stamina depleted
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                staminaDepleted = true;
                // Don't call StopDefense here - it will be called in Update
            }
        }
        else
        {
            // Recover stamina when not defending
            currentStamina += staminaRecoveryRate * Time.deltaTime;
            
            // Cap stamina at max
            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                staminaDepleted = false;
            }
        }
    }
    
    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }

    // Public method to check if character is currently attacking
    public bool IsAttacking()
    {
        return isAttacking || isDashAttacking;
    }
    
    // Public method to check if character is currently defending
    public bool IsDefending()
    {
        return isDefending;
    }
    
    // Called from Movement script to initiate dash attack
    public void DashAttack(int direction)
    {
        // Don't allow dash attack if it's disabled for this character
        if (!enableDashAttack)
            return;
            
        // Check if we can attack based on delay
        float timeSinceLastAttack = Time.time - lastAttackTime;
        if (timeSinceLastAttack < comboDelay || isAttacking || isDashAttacking || isDefending)
        {
            if (showDebugInfo && timeSinceLastAttack < comboDelay)
            {
                Debug.Log($"<color=orange>Dash Attack denied:</color> Need to wait {(comboDelay - timeSinceLastAttack).ToString("F2")}s more");
            }
            return;
        }
            
        // Start dash attack
        isDashAttacking = true;
        lastAttackTime = Time.time;
        
        // Play dash sound
        if (audioManager != null)
            audioManager.PlayDashSFX();
        
        // Trigger dash attack animations
        animator.SetTrigger("isAttacking");
        animator.SetTrigger("Dash");
        
        Debug.Log($"Dash Attack! Damage: {dashAttackDamage}, Direction: {direction}");
        
        // Call hit detection with delay appropriate for dash attack
        StartCoroutine(DashAttackRoutine());
    }
    
    // Public method to check if dash is enabled for this character
    public bool IsDashEnabled()
    {
        return enableDashAttack;
    }
    
    private IEnumerator DashAttackRoutine()
    {
        // Wait for the middle of the dash duration for main hit detection
        yield return new WaitForSeconds(dashAttackDuration * 0.5f);
        
        // Perform the hit detection with larger radius
        DashAttackHitDetection();
        
        // Wait for the rest of the dash attack to complete
        yield return new WaitForSeconds(dashAttackDuration * 0.5f);
        
        // End dash attack state
        isDashAttacking = false;
    }
    
    private void DashAttackHitDetection()
    {
        if (attackPoint == null)
            return;
            
        // Use box overlap for square hitbox (dash attack uses bigger dimensions)
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(
            attackPoint.position,                  // center of box
            new Vector2(dashAttackWidth, dashAttackHeight), // size of box (width, height)
            0f,                                    // rotation of box (0 = no rotation)
            enemyLayers);
        
        // Apply damage to enemies
        foreach (Collider2D enemy in hitEnemies)
        {
            // Get enemy health component and apply damage
            CharacterHealth enemyHealth = enemy.GetComponent<CharacterHealth>();
            if (enemyHealth != null)
            {
                // Send damage amount, position, and this gameObject as the attacker
                enemyHealth.TakeDamage(dashAttackDamage, transform.position, gameObject);
                Debug.Log($"Dash Attack hit enemy: {enemy.name} with {dashAttackDamage} damage!");
            }
        }
    }
    
    void TryAttack()
    {
        // If we're within combo window, advance combo
        if (Time.time <= lastAttackTime + comboResetTime && currentCombo < maxCombo - 1)
        {
            currentCombo++;
        }
        else
        {
            currentCombo = 0;  // First attack in combo
        }
        
        // Update last attack time
        lastAttackTime = Time.time;
        
        // Reset any existing combo reset coroutine
        if (comboResetCoroutine != null)
            StopCoroutine(comboResetCoroutine);
            
        // Start new combo reset timer
        comboResetCoroutine = StartCoroutine(ComboResetTimer());
        
        // Execute attack
        Attack();
    }

    void Attack()
    {
        // Set attacking flag
        isAttacking = true;
        
        // Play attack sound
        if (audioManager != null)
            audioManager.PlayAttackSFX();
        
        // Trigger animation
        animator.SetTrigger("isAttacking");
        animator.SetInteger("Combo", currentCombo);
        
        Debug.Log($"Attack! Combo: {currentCombo}, Damage: {comboDamage[currentCombo]}");
        
        // Call hit detection in middle of attack
        StartCoroutine(HitDetectionWithDelay(attackDuration * 0.5f));
        
        // Reset attack state after exact duration
        StartCoroutine(ResetAttackState());
    }
    
    // Called from animation event or after delay
    public void HitDetection()
    {
        if (attackPoint == null)
            return;
            
        // Use box overlap for square hitbox
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(
            attackPoint.position,                  // center of box
            new Vector2(attackWidth, attackHeight), // size of box (width, height)
            0f,                                    // rotation of box (0 = no rotation)
            enemyLayers);
        
        // Apply damage to enemies
        foreach (Collider2D enemy in hitEnemies)
        {
            // Get enemy health component and apply damage
            CharacterHealth enemyHealth = enemy.GetComponent<CharacterHealth>();
            if (enemyHealth != null)
            {
                // Send damage amount, position, and this gameObject as the attacker
                bool damageApplied = enemyHealth.TakeDamage(comboDamage[currentCombo], transform.position, gameObject);
                Debug.Log($"Hit enemy: {enemy.name} with {comboDamage[currentCombo]} damage!");
                
                // Check if the enemy is Knight and is defending
                CharacterAttack enemyAttack = enemy.GetComponent<CharacterAttack>();
                if (enemyAttack != null && enemyAttack.IsDefending() && enemy.name.Contains("Knight") && 
                    gameObject.name.Contains("Samurai"))
                {
                    // Get the knight's facing direction
                    int knightFacingDirection = enemy.transform.localScale.x > 0 ? 1 : -1;
                    
                    // Check if the attack is coming from the direction the knight is facing
                    Vector2 attackerPosition = transform.position;
                    bool isAttackFromFront = enemyAttack.IsDamageFromFront(attackerPosition, knightFacingDirection);
                    
                    // Only apply counter damage if the attack is from the front
                    if (isAttackFromFront)
                    {
                        // Make Samurai take 1 damage when hitting a defending Knight from the front
                        CharacterHealth myHealth = GetComponent<CharacterHealth>();
                        if (myHealth != null)
                        {
                            myHealth.TakeDamage(1f, enemy.transform.position, enemy.gameObject);
                            Debug.Log($"<color=orange>COUNTER DAMAGE:</color> {gameObject.name} took 1 damage for hitting defending Knight from the front!");
                        }
                    }
                    else
                    {
                        Debug.Log($"<color=green>NO COUNTER DAMAGE:</color> {gameObject.name} attacked Knight from behind!");
                    }
                }
            }
        }
    }
    
    IEnumerator HitDetectionWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HitDetection();
    }
    
    IEnumerator ResetAttackState()
    {
        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
    }
    
    IEnumerator ComboResetTimer()
    {
        yield return new WaitForSeconds(comboResetTime);
        if (currentCombo > 0)
        {
            ResetCombo();
            Debug.Log("Combo reset due to timeout");
        }
    }
    
    void ResetCombo()
    {
        currentCombo = 0;
    }
    
    // Public method to handle damage reduction when hit
    public float CalculateDamageTaken(float incomingDamage, Vector2 damageSourcePosition)
    {
        if (isDefending)
        {
            // Get character's facing direction (1 = right, -1 = left)
            int facingDirection = transform.localScale.x > 0 ? 1 : -1;
            
            // Calculate if the damage is coming from the direction character is facing
            bool isDamageFromFront = IsDamageFromFront(damageSourcePosition, facingDirection);
            
            // If defending from the front, apply damage reduction
            if (isDamageFromFront)
            {
                // Apply damage reduction based on percentage
                float reducedDamage = incomingDamage * (1 - damageReductionPercent / 100f);
                Debug.Log($"<color=green>FRONT DEFENSE!</color> Reduced damage from {incomingDamage} to {reducedDamage}.");
                return reducedDamage;
            }
            else
            {
                // If hit from behind while defending, take full damage
                Debug.Log($"<color=yellow>DEFENSE FAILED!</color> Hit from behind, taking full damage: {incomingDamage}.");
                return incomingDamage;
            }
        }
        
        // Not defending, take full damage
        return incomingDamage;
    }
    
    // Helper method to determine if damage is coming from the direction character is facing
    private bool IsDamageFromFront(Vector2 damageSourcePosition, int facingDirection)
    {
        // Compare character's x position with damage source x position
        float relativeX = damageSourcePosition.x - transform.position.x;
        
        // If facing right (facingDirection = 1), damage from front means relativeX > 0
        // If facing left (facingDirection = -1), damage from front means relativeX < 0
        return (facingDirection > 0 && relativeX > 0) || (facingDirection < 0 && relativeX < 0);
    }
    
    // Defense hit detection with square hitbox (visual feedback for debugging)
    private void DefenseHitDetection()
    {
        if (attackPoint == null)
            return;
            
        // Use box overlap for square hitbox
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(
            attackPoint.position,                  // center of box
            new Vector2(attackWidth, attackHeight), // size of box (width, height)
            0f,                                    // rotation of box (0 = no rotation)
            enemyLayers);
        
        // Get character's facing direction
        int facingDirection = transform.localScale.x > 0 ? 1 : -1;
        
        // Detect enemies but don't apply damage, just log for debugging
        foreach (Collider2D enemy in hitEnemies)
        {
            // Get enemy position
            Vector2 enemyPosition = enemy.transform.position;
            
            // Check if enemy is in front of character based on facing direction
            bool isEnemyInFront = IsDamageFromFront(enemyPosition, facingDirection);
            
            // For debugging only - remove in production
            if (showDebugInfo && Time.frameCount % 30 == 0) // Only log occasionally to avoid spam
            {
                string defenseStatus = isEnemyInFront ? "FRONT (REDUCED DAMAGE)" : "BEHIND (FULL DAMAGE)";
                Debug.Log($"<color=cyan>DEFENSE CHECK:</color> Enemy {enemy.name} is {defenseStatus}");
            }
        }
    }
    
    // Visualization for attack and defense hitbox in editor
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;
        
        // Draw regular attack hitbox as a wire cube (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, new Vector3(attackWidth, attackHeight, 0.1f));
        
        // Draw defense hitbox as a wire cube (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(attackPoint.position, new Vector3(attackWidth, attackHeight, 0.1f));
        
        // Draw dash attack hitbox as a wire cube (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(attackPoint.position, new Vector3(dashAttackWidth, dashAttackHeight, 0.1f));
    }
    
    // Debug option to see hit ranges
    [Header("Debug Options")]
    public bool showDebugInfo = true;

    void StartDefense()
    {
        // Prevent starting defense if already defending or in another action
        if (isAttacking || isDashAttacking)
            return;
        
        // Check if we have enough stamina
        if (staminaDepleted || currentStamina <= 0)
        {
            Debug.Log("<color=red>DEFENSE FAILED!</color> Not enough stamina!");
            return;
        }
            
        // Set defending state
        isDefending = true;
        
        // Play defend sound based on character type
        if (audioManager != null)
            audioManager.PlayDefendSFX(characterID);
        
        // Set the animation parameter
        if (animator)
        {
            animator.SetBool("isDefending", true);
        }
        
        lastDefenseTime = Time.time;
        Debug.Log("<color=blue>DEFENSE UP!</color> Damage reduction active!");
    }
    
    void StopDefense()
    {
        // Only stop if actually defending
        if (!isDefending)
            return;
            
        // Reset defending state
        isDefending = false;
        
        // Reset animation parameter
        if (animator)
        {
            animator.SetBool("isDefending", false);
        }
        
        lastDefenseTime = Time.time;
        Debug.Log("<color=cyan>DEFENSE DOWN!</color>");
    }
    
    // Public method to disable attacks (called when character dies)
    public void DisableAttacks()
    {
        // Set flags to prevent any new attacks
        isAttacking = false;
        isDashAttacking = false;
        isDefending = false;
        
        // Cancel any ongoing attack routines
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
        }
        
        // Reset animation parameters
        if (animator)
        {
            animator.SetBool("isDefending", false);
        }
        
        // Log disabling attacks
        if (showDebugInfo)
        {
            Debug.Log($"<color=red>ATTACKS DISABLED</color> for {gameObject.name} (character died)");
        }
    }
}
