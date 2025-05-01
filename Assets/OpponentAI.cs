using UnityEngine;

public class OpponentAI : MonoBehaviour
{
    public Transform target; // Assign the player character in Inspector or via script
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    private CharacterAttack attackScript;

    void Start()
    {
        attackScript = GetComponent<CharacterAttack>();
    }

    void Update()
    {
        if (target == null) return;
        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > attackRange)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;
        }
        else
        {
            if (attackScript != null)
            {
                attackScript.SendMessage("TryAttack");
            }
        }
    }
}
