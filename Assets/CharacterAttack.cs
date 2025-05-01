using UnityEngine;
using System.Collections;

public class CharacterAttack : MonoBehaviour
{
    // Character, attack, dash, defense, damage, and debug settings
    [Header("Character Type")]
    public bool enableDashAttack = true;
    public string characterID = "default";
    [Header("Attack Settings")]
    public KeyCode attackKey = KeyCode.J;
    public float attackDuration = 0.2f;
    public float comboDelay = 0.1f;
    public int maxCombo = 3;
    public float comboResetTime = 1.5f;
    [Header("Dash Attack Settings")]
    public float dashAttackDuration = 0.3f;
    public float dashAttackDamage = 30f;
    public float dashAttackWidth = 1.4f;
    public float dashAttackHeight = 1.0f;
    [Header("Defense Settings")]
    public KeyCode defenseKey = KeyCode.S;
    public float defenseStaminaCost = 1f;
    public float maxStamina = 100f;
    public float staminaRecoveryRate = 15f;
    public float damageReductionPercent = 75f;
    public float currentStamina;
    private bool staminaDepleted = false;
    [Header("Damage Settings")]
    public float[] comboDamage = { 10f, 15f, 25f };
    [Header("Hit Detection")]
    public Transform attackPoint;
    public float attackWidth = 1.0f;
    public float attackHeight = 0.5f;
    public LayerMask enemyLayers;
    // Audio reference
    private AudioManager audioManager;
    private Animator animator;
    private Movement movement;
    private bool isAttacking = false;
    private bool isDashAttacking = false;
    private bool isDefending = false;
    private int currentCombo = 0;
    private float lastAttackTime = 0f;
    private float lastDefenseTime = 0f;
    private Coroutine comboResetCoroutine;
    [Header("Debug Options")]
    public bool showDebugInfo = true;

    void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<Movement>();
        currentStamina = maxStamina;
        audioManager = FindObjectOfType<AudioManager>();
        if (!enableDashAttack && movement != null)
            movement.SetDashEnabled(false);
        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>Character [{characterID}] initialized</color> - Dash Attack: {(enableDashAttack ? "Enabled" : "Disabled")}");
            Debug.Log($"<color=cyan>Attack Power:</color> Combo1={comboDamage[0]}, Combo2={comboDamage[1]}, Combo3={comboDamage[2]}, Dash={dashAttackDamage}");
        }
    }

    void Update()
    {
        float timeSinceLastAttack = Time.time - lastAttackTime;
        bool canAttack = !isDefending && !isAttacking && !isDashAttacking && timeSinceLastAttack >= comboDelay;
        if (movement && movement.isGrounded && Input.GetKeyDown(attackKey))
        {
            if (canAttack)
                TryAttack();
            else if (showDebugInfo && timeSinceLastAttack < comboDelay)
                Debug.Log($"<color=orange>Attack denied:</color> Need to wait {(comboDelay - timeSinceLastAttack).ToString("F2")}s more");
        }
        if(Input.GetKey(defenseKey) && !isAttacking && !isDashAttacking && !staminaDepleted)
        {
            if (!isDefending)
                StartDefense();
        }
        else if(isDefending)
        {
            StopDefense();
        }
        if (isDefending)
            DefenseHitDetection();
        ManageStamina();
    }

    void ManageStamina()
    {
        if (isDefending)
        {
            currentStamina -= defenseStaminaCost * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                staminaDepleted = true;
            }
        }
        else
        {
            currentStamina += staminaRecoveryRate * Time.deltaTime;
            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                staminaDepleted = false;
            }
        }
    }

    public float GetStaminaPercentage() => currentStamina / maxStamina;
    public bool IsAttacking() => isAttacking || isDashAttacking;
    public bool IsDefending() => isDefending;

    public void DashAttack(int direction)
    {
        if (!enableDashAttack)
            return;
        float timeSinceLastAttack = Time.time - lastAttackTime;
        if (timeSinceLastAttack < comboDelay || isAttacking || isDashAttacking || isDefending)
        {
            if (showDebugInfo && timeSinceLastAttack < comboDelay)
                Debug.Log($"<color=orange>Dash Attack denied:</color> Need to wait {(comboDelay - timeSinceLastAttack).ToString("F2")}s more");
            return;
        }
        isDashAttacking = true;
        lastAttackTime = Time.time;
        if (audioManager != null)
            audioManager.PlayDashSFX();
        animator.SetTrigger("isAttacking");
        animator.SetTrigger("Dash");
        Debug.Log($"Dash Attack! Damage: {dashAttackDamage}, Direction: {direction}");
        StartCoroutine(DashAttackRoutine());
    }
    public bool IsDashEnabled() => enableDashAttack;
    private IEnumerator DashAttackRoutine()
    {
        yield return new WaitForSeconds(dashAttackDuration * 0.5f);
        DashAttackHitDetection();
        yield return new WaitForSeconds(dashAttackDuration * 0.5f);
        isDashAttacking = false;
    }
    private void DashAttackHitDetection()
    {
        if (attackPoint == null)
            return;
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(
            attackPoint.position,
            new Vector2(dashAttackWidth, dashAttackHeight),
            0f,
            enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            CharacterHealth enemyHealth = enemy.GetComponent<CharacterHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(dashAttackDamage, transform.position, gameObject);
                Debug.Log($"Dash Attack hit enemy: {enemy.name} with {dashAttackDamage} damage!");
            }
        }
    }
    void TryAttack()
    {
        if (Time.time <= lastAttackTime + comboResetTime && currentCombo < maxCombo - 1)
            currentCombo++;
        else
            currentCombo = 0;
        lastAttackTime = Time.time;
        if (comboResetCoroutine != null)
            StopCoroutine(comboResetCoroutine);
        comboResetCoroutine = StartCoroutine(ComboResetTimer());
        Attack();
    }
    void Attack()
    {
        isAttacking = true;
        if (audioManager != null)
            audioManager.PlayAttackSFX();
        animator.SetTrigger("isAttacking");
        animator.SetInteger("Combo", currentCombo);
        Debug.Log($"Attack! Combo: {currentCombo}, Damage: {comboDamage[currentCombo]}");
        StartCoroutine(HitDetectionWithDelay(attackDuration * 0.5f));
        StartCoroutine(ResetAttackState());
    }
    public void HitDetection()
    {
        if (attackPoint == null)
            return;
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(
            attackPoint.position,
            new Vector2(attackWidth, attackHeight),
            0f,
            enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            CharacterHealth enemyHealth = enemy.GetComponent<CharacterHealth>();
            if (enemyHealth != null)
            {
                bool damageApplied = enemyHealth.TakeDamage(comboDamage[currentCombo], transform.position, gameObject);
                Debug.Log($"Hit enemy: {enemy.name} with {comboDamage[currentCombo]} damage!");
                CharacterAttack enemyAttack = enemy.GetComponent<CharacterAttack>();
                if (enemyAttack != null && enemyAttack.IsDefending() && enemy.name.Contains("Knight") && gameObject.name.Contains("Samurai"))
                {
                    int knightFacingDirection = enemy.transform.localScale.x > 0 ? 1 : -1;
                    Vector2 attackerPosition = transform.position;
                    bool isAttackFromFront = enemyAttack.IsDamageFromFront(attackerPosition, knightFacingDirection);
                    if (isAttackFromFront)
                    {
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
    void ResetCombo() => currentCombo = 0;
    public float CalculateDamageTaken(float incomingDamage, Vector2 damageSourcePosition)
    {
        if (isDefending)
        {
            int facingDirection = transform.localScale.x > 0 ? 1 : -1;
            bool isDamageFromFront = IsDamageFromFront(damageSourcePosition, facingDirection);
            if (isDamageFromFront)
            {
                float reducedDamage = incomingDamage * (1 - damageReductionPercent / 100f);
                Debug.Log($"<color=green>FRONT DEFENSE!</color> Reduced damage from {incomingDamage} to {reducedDamage}.");
                return reducedDamage;
            }
            else
            {
                Debug.Log($"<color=yellow>DEFENSE FAILED!</color> Hit from behind, taking full damage: {incomingDamage}.");
                return incomingDamage;
            }
        }
        return incomingDamage;
    }
    private bool IsDamageFromFront(Vector2 damageSourcePosition, int facingDirection)
    {
        float relativeX = damageSourcePosition.x - transform.position.x;
        return (facingDirection > 0 && relativeX > 0) || (facingDirection < 0 && relativeX < 0);
    }
    private void DefenseHitDetection()
    {
        if (attackPoint == null)
            return;
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(
            attackPoint.position,
            new Vector2(attackWidth, attackHeight),
            0f,
            enemyLayers);
        int facingDirection = transform.localScale.x > 0 ? 1 : -1;
        foreach (Collider2D enemy in hitEnemies)
        {
            Vector2 enemyPosition = enemy.transform.position;
            bool isEnemyInFront = IsDamageFromFront(enemyPosition, facingDirection);
            if (showDebugInfo && Time.frameCount % 30 == 0)
            {
                string defenseStatus = isEnemyInFront ? "FRONT (REDUCED DAMAGE)" : "BEHIND (FULL DAMAGE)";
                Debug.Log($"<color=cyan>DEFENSE CHECK:</color> Enemy {enemy.name} is {defenseStatus}");
            }
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, new Vector3(attackWidth, attackHeight, 0.1f));
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(attackPoint.position, new Vector3(attackWidth, attackHeight, 0.1f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(attackPoint.position, new Vector3(dashAttackWidth, dashAttackHeight, 0.1f));
    }
    void StartDefense()
    {
        if (isAttacking || isDashAttacking)
            return;
        if (staminaDepleted || currentStamina <= 0)
        {
            Debug.Log("<color=red>DEFENSE FAILED!</color> Not enough stamina!");
            return;
        }
        isDefending = true;
        if (audioManager != null)
            audioManager.PlayDefendSFX(characterID);
        if (animator)
            animator.SetBool("isDefending", true);
        lastDefenseTime = Time.time;
        Debug.Log("<color=blue>DEFENSE UP!</color> Damage reduction active!");
    }
    void StopDefense()
    {
        if (!isDefending)
            return;
        isDefending = false;
        if (animator)
            animator.SetBool("isDefending", false);
        lastDefenseTime = Time.time;
        Debug.Log("<color=cyan>DEFENSE DOWN!</color>");
    }
    public void DisableAttacks()
    {
        isAttacking = false;
        isDashAttacking = false;
        isDefending = false;
        if (comboResetCoroutine != null)
            StopCoroutine(comboResetCoroutine);
        if (animator)
            animator.SetBool("isDefending", false);
        if (showDebugInfo)
            Debug.Log($"<color=red>ATTACKS DISABLED</color> for {gameObject.name} (character died)");
    }
}
