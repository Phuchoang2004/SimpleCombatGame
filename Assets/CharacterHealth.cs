using UnityEngine;
using System.Collections;

public class CharacterHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isInvulnerable = false;
    public float invulnerabilityTime = 0.5f; 
    
    private float lastDamageTime = -999f;
    private CharacterAttack attackScript;
    private Animator animator;
    private Movement movementScript;
    private bool isStunned = false;
    private bool isDead = false;  
    private AudioManager audioManager;  

    private void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        attackScript = GetComponent<CharacterAttack>();
        movementScript = GetComponent<Movement>();
        audioManager = FindObjectOfType<AudioManager>();  
        
        Debug.Log($"<color=green>{gameObject.name} health initialized:</color> {currentHealth}/{maxHealth}");
    }

    public bool TakeDamage(float amount, Vector2 damageSourcePosition, GameObject attacker = null)
    {
        if (isDead || isInvulnerable || Time.time < lastDamageTime + invulnerabilityTime)
            return false;
        
        if (attacker != null && attacker == gameObject)
        {
            return false;
        }
        
        float finalDamage = amount;
        if (attackScript != null)
        {
            finalDamage = attackScript.CalculateDamageTaken(amount, damageSourcePosition);
        }
        
        if (finalDamage <= 0)
            return false;
            
        currentHealth -= finalDamage;
        lastDamageTime = Time.time;
        
        if (audioManager != null)
            audioManager.PlayHitSFX();
        
        if (animator != null)
        {

            animator.SetTrigger("isHit");
            
            animator.SetInteger("ReceiveDmg", Mathf.RoundToInt(finalDamage));
            
            StartCoroutine(ResetReceiveDmgAfterDelay(0.5f));
            
            if (!isStunned && movementScript != null)
            {
                isStunned = true;
                movementScript.SetStunned(true);
                StartCoroutine(RecoverFromStun(0.5f)); 
            }
        }
        
        Debug.Log($"<color=red>{gameObject.name} took {finalDamage} damage!</color> Health: {currentHealth}/{maxHealth} from {(attacker ? attacker.name : "unknown")}");
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        
        return true;
    }
    
    private IEnumerator RecoverFromStun(float duration)
    {
        if (isDead)
            yield break;
            
        yield return new WaitForSeconds(duration);
        
        if (movementScript != null)
        {
            movementScript.SetStunned(false);
        }
        
        isStunned = false;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    private void Die()
    {
        isDead = true;
        
        if (animator != null)
        {
            animator.SetTrigger("Death");
            animator.SetBool("isJumping", false);
            animator.SetBool("isDefending", false);
        }
        
        if (movementScript != null)
        {
            movementScript.SetStunned(true);
        }
        
        if (attackScript != null)
        {
            attackScript.DisableAttacks();
        }
        
        // Stop sliding after death
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true; // Optional: disables further physics
        }

        Debug.Log($"<color=purple>{gameObject.name} has died!</color>");
    }
    
    public bool IsDead()
    {
        return isDead;
    }
    
    public void ResetReceiveDmgParameter()
    {
        if (animator != null)
        {
            animator.SetInteger("ReceiveDmg", 0);
        }
    }
    
    private IEnumerator ResetReceiveDmgAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetReceiveDmgParameter();
    }
}