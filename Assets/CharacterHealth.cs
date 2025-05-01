using UnityEngine;
using System.Collections;

public class CharacterHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isInvulnerable = false;
    public float invulnerabilityTime = 0.5f;  // Time after damage when can't be hit again
    
    private float lastDamageTime = -999f;
    private CharacterAttack attackScript;
    private Animator animator;
    private Movement movementScript;
    private bool isStunned = false;
    private bool isDead = false;  // New property to track death state
    private AudioManager audioManager;  // Reference to audio manager

    private void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        attackScript = GetComponent<CharacterAttack>();
        movementScript = GetComponent<Movement>();
        audioManager = FindObjectOfType<AudioManager>();  // Get audio manager reference
        
        Debug.Log($"<color=green>{gameObject.name} health initialized:</color> {currentHealth}/{maxHealth}");
    }

    public bool TakeDamage(float amount, Vector2 damageSourcePosition, GameObject attacker = null)
    {
        // If dead, invulnerable, or within invulnerability time window, ignore damage
        if (isDead || isInvulnerable || Time.time < lastDamageTime + invulnerabilityTime)
            return false;
        
        // Prevent self-damage (don't take damage from own attacks)
        if (attacker != null && attacker == gameObject)
        {
            return false;
        }
        
        // Calculate final damage based on defense if present
        float finalDamage = amount;
        if (attackScript != null)
        {
            finalDamage = attackScript.CalculateDamageTaken(amount, damageSourcePosition);
        }
        
        // If no damage to take (perfect block), return
        if (finalDamage <= 0)
            return false;
            
        // Apply damage
        currentHealth -= finalDamage;
        lastDamageTime = Time.time;
        
        // Play hit sound effect
        if (audioManager != null)
            audioManager.PlayHitSFX();
        
        // Trigger animation system
        if (animator != null)
        {
            // Set isHit trigger
            animator.SetTrigger("isHit");
            
            // Set the ReceiveDmg parameter with damage amount (as integer)
            animator.SetInteger("ReceiveDmg", Mathf.RoundToInt(finalDamage));
            
            // Start coroutine to reset the ReceiveDmg parameter after a short delay
            StartCoroutine(ResetReceiveDmgAfterDelay(0.5f));
            
            // Stun the character's movement if needed
            if (!isStunned && movementScript != null)
            {
                isStunned = true;
                movementScript.SetStunned(true);
                StartCoroutine(RecoverFromStun(0.5f)); // Short stun recovery time
            }
        }
        
        Debug.Log($"<color=red>{gameObject.name} took {finalDamage} damage!</color> Health: {currentHealth}/{maxHealth} from {(attacker ? attacker.name : "unknown")}");
        
        // Check for death
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        
        return true;
    }
    
    private IEnumerator RecoverFromStun(float duration)
    {
        // Don't recover from stun if dead
        if (isDead)
            yield break;
            
        yield return new WaitForSeconds(duration);
        
        if (movementScript != null)
        {
            movementScript.SetStunned(false);
        }
        
        isStunned = false;
    }
    
    // Returns the current health as a percentage (0-1)
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    private void Die()
    {
        // Set death state
        isDead = true;
        
        // Set Death trigger
        if (animator != null)
        {
            animator.SetTrigger("Death");
            // Make sure other animation states are turned off
            animator.SetBool("isJumping", false);
            animator.SetBool("isDefending", false);
        }
        
        // Disable movement permanently
        if (movementScript != null)
        {
            movementScript.SetStunned(true);
        }
        
        // Disable attacks
        if (attackScript != null)
        {
            attackScript.DisableAttacks();
        }
        
        // Log death
        Debug.Log($"<color=purple>{gameObject.name} has died!</color>");
    }
    
    // Public method to check if character is dead
    public bool IsDead()
    {
        return isDead;
    }
    
    // Public method to reset ReceiveDmg parameter - can be called from animation events
    public void ResetReceiveDmgParameter()
    {
        if (animator != null)
        {
            animator.SetInteger("ReceiveDmg", 0);
        }
    }
    
    // Coroutine to reset the ReceiveDmg parameter after a delay
    private IEnumerator ResetReceiveDmgAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetReceiveDmgParameter();
    }
}