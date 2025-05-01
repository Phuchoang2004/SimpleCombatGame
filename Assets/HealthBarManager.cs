using UnityEngine;

public class HealthBarManager : MonoBehaviour
{
    [Header("Health Bar References")]
    public GameObject healthBar1; 
    public GameObject healthBar2; 
    public Transform healthFill1; 
    public Transform healthFill2; 
    
    [Header("Character References")]
    public CharacterHealth character1Health; 
    public CharacterHealth character2Health; 
    
    [Header("Health Bar Settings")]
    [Tooltip("If true, healthBar1 will reduce from right to left")]
    public bool reverseHealthBar1 = true; 
    [Tooltip("If true, healthBar2 will reduce from right to left")]
    public bool reverseHealthBar2 = false;
    
    private Vector3 healthFill1OriginalScale;
    private Vector3 healthFill2OriginalScale;
    private Vector3 healthFill1OriginalPosition;
    private Vector3 healthFill2OriginalPosition;

    void Start()
    {
        if (healthFill1 != null)
        {
            healthFill1OriginalScale = healthFill1.localScale;
            healthFill1OriginalPosition = healthFill1.localPosition;
        }
        
        if (healthFill2 != null)
        {
            healthFill2OriginalScale = healthFill2.localScale;
            healthFill2OriginalPosition = healthFill2.localPosition;
        }
        
        if (character1Health == null && healthBar1 != null)
        {
            Debug.LogWarning("Character1Health not set in HealthBarManager. Please assign it in the inspector.");
        }
        
        if (character2Health == null && healthBar2 != null)
        {
            Debug.LogWarning("Character2Health not set in HealthBarManager. Please assign it in the inspector.");
        }
    }

    void Update()
    {
        UpdateHealthBar(healthFill1, character1Health, reverseHealthBar1);
        UpdateHealthBar(healthFill2, character2Health, reverseHealthBar2);
    }
    
    void UpdateHealthBar(Transform healthFill, CharacterHealth characterHealth, bool reverseDirection)
    {
        if (healthFill == null || characterHealth == null)
            return;
        
        float healthPercentage = characterHealth.GetHealthPercentage();
        Vector3 originalScale = (healthFill == healthFill1) ? healthFill1OriginalScale : healthFill2OriginalScale;
        Vector3 originalPosition = (healthFill == healthFill1) ? healthFill1OriginalPosition : healthFill2OriginalPosition;
        
        float reduction = (1 - healthPercentage) * originalScale.x;
        Vector3 newScale = originalScale;
        newScale.x = originalScale.x * healthPercentage;
        healthFill.localScale = newScale;
        
        Vector3 newPosition = originalPosition;
        float shift = reduction * 0.5f;
        if (reverseDirection)
            newPosition.x -= shift;
        else
            newPosition.x += shift;
        healthFill.localPosition = newPosition;
    }
}
