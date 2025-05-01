using UnityEngine;

public class OpponentAI : MonoBehaviour
{
    public Transform target; // Assign the player character in Inspector or via script
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public float dashRange = 4f;
    public float defenseHealthThreshold = 0.3f; // Defend if health below 30%
    public float defenseReactRange = 2.5f; // Defend if player is attacking and close
    public float dashCooldown = 2f;
    public bool useAI = true; // Toggle AI control in Inspector
    private float lastDashTime = -999f;
    private CharacterAttack attackScript;
    private Movement movementScript;
    private CharacterHealth healthScript;
    private Animator animator;
    private Transform playerAttackPoint;
    private CharacterAttack playerAttackScript;

    public enum Difficulty { Easy, Normal, Hard }
    public Difficulty difficulty = Difficulty.Normal;

    void Start()
    {
        attackScript = GetComponent<CharacterAttack>();
        movementScript = GetComponent<Movement>();
        healthScript = GetComponent<CharacterHealth>();
        animator = GetComponent<Animator>();
        if (target != null)
        {
            playerAttackScript = target.GetComponent<CharacterAttack>();
            if (playerAttackScript != null)
                playerAttackPoint = playerAttackScript.attackPoint;
        }
        // Use script values for stats
        if (attackScript != null)
        {
            attackRange = Mathf.Max(attackScript.attackWidth, attackScript.dashAttackWidth) + 0.2f;
        }
        if (movementScript != null)
        {
            moveSpeed = movementScript.moveSpeed * 0.8f;
            dashRange = movementScript.moveSpeed * 2.5f;
        }
        if (healthScript != null)
        {
            defenseHealthThreshold = Mathf.Clamp01(0.3f * (healthScript.maxHealth / 100f));
        }
    }

    void Update()
    {
        if (!useAI) return;
        if (target == null || healthScript == null || healthScript.IsDead() || (movementScript != null && movementScript.IsStunned()))
            return;

        float distance = Vector2.Distance(transform.position, target.position);
        bool playerIsAttacking = playerAttackScript != null && playerAttackScript.IsAttacking();
        bool shouldDefend = false;
        float defendChance = 0f;
        float attackChance = 0f;
        float dashChance = 0f;
        float reactDelay = 0f;
        // Difficulty-based randomness
        switch (difficulty)
        {
            case Difficulty.Easy:
                defendChance = 0.2f;
                attackChance = 0.4f;
                dashChance = 0.1f;
                reactDelay = Random.Range(0.3f, 0.7f);
                break;
            case Difficulty.Normal:
                defendChance = 0.5f;
                attackChance = 0.7f;
                dashChance = 0.3f;
                reactDelay = Random.Range(0.1f, 0.3f);
                break;
            case Difficulty.Hard:
                defendChance = 0.85f;
                attackChance = 0.95f;
                dashChance = 0.6f;
                reactDelay = 0f;
                break;
        }
        // Defense logic
        if ((healthScript.GetHealthPercentage() < defenseHealthThreshold || (playerIsAttacking && distance < attackRange + 0.5f)) && Random.value < defendChance)
        {
            shouldDefend = true;
        }
        // Face the player
        if (target.position.x > transform.position.x)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        // Defense
        if (attackScript != null && shouldDefend && !attackScript.IsDefending())
        {
            attackScript.SendMessage("StartDefense");
            if (difficulty == Difficulty.Easy) return;
        }
        else if (attackScript != null && !shouldDefend && attackScript.IsDefending())
        {
            attackScript.SendMessage("StopDefense");
        }
        // Dash logic (only if dash is enabled for this character)
        if (attackScript != null && attackScript.enableDashAttack && movementScript != null && Time.time > lastDashTime + dashCooldown && Random.value < dashChance)
        {
            if (distance > attackRange && distance < dashRange)
            {
                movementScript.StartDash();
                lastDashTime = Time.time;
                return;
            }
            else if (playerIsAttacking && distance < dashRange)
            {
                movementScript.StartDash();
                lastDashTime = Time.time;
                return;
            }
        }
        // Move toward player if not in range
        if (distance > attackRange)
        {
            if (movementScript != null && !movementScript.IsStunned())
            {
                float direction = target.position.x > transform.position.x ? 1f : -1f;
                movementScript.SetAIMoveInput(direction);
                if (animator != null)
                {
                    animator.SetFloat("xVelocity", Mathf.Abs(moveSpeed));
                }
            }
        }
        else
        {
            // Stop moving
            if (movementScript != null)
            {
                movementScript.SetAIMoveInput(0f);
                if (animator != null)
                {
                    animator.SetFloat("xVelocity", 0);
                }
            }
            // Attack if not defending, with randomness
            if (attackScript != null && !attackScript.IsDefending() && !attackScript.IsAttacking() && Random.value < attackChance)
            {
                if (reactDelay > 0f)
                {
                    StartCoroutine(DelayedAttack(reactDelay));
                }
                else
                {
                    attackScript.SendMessage("TryAttack");
                }
            }
        }
    }

    private System.Collections.IEnumerator DelayedAttack(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (attackScript != null && !attackScript.IsDefending() && !attackScript.IsAttacking())
        {
            attackScript.SendMessage("TryAttack");
        }
    }
}
