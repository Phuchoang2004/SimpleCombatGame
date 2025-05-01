using UnityEngine;

public class StaminaManager : MonoBehaviour
{
    [Header("Stamina Bar References")]
    public GameObject staminaBar1; // First character's stamina bar container
    public GameObject staminaBar2; // Second character's stamina bar container
    public Transform staminaFill1; // The stamina bar fill for character 1
    public Transform staminaFill2; // The stamina bar fill for character 2
    
    [Header("Character References")]
    public CharacterAttack character1Attack; // First character's attack component
    public CharacterAttack character2Attack; // Second character's attack component
    
    [Header("Stamina Bar Settings")]
    [Tooltip("If true, staminaBar1 will reduce from right to left")]
    public bool reverseStaminaBar1 = false;
    [Tooltip("If true, staminaBar2 will reduce from right to left")]
    public bool reverseStaminaBar2 = true;
    
    private Vector3 staminaFill1OriginalScale;
    private Vector3 staminaFill2OriginalScale;
    private Vector3 staminaFill1OriginalPosition;
    private Vector3 staminaFill2OriginalPosition;

    void Start()
    {
        // Store original scales and positions
        if (staminaFill1 != null)
        {
            staminaFill1OriginalScale = staminaFill1.localScale;
            staminaFill1OriginalPosition = staminaFill1.localPosition;
        }
        
        if (staminaFill2 != null)
        {
            staminaFill2OriginalScale = staminaFill2.localScale;
            staminaFill2OriginalPosition = staminaFill2.localPosition;
        }
        
        // Try to find character attack components if not set
        if (character1Attack == null && staminaBar1 != null)
        {
            Debug.LogWarning("Character1Attack not set in StaminaManager. Please assign it in the inspector.");
        }
        
        if (character2Attack == null && staminaBar2 != null)
        {
            Debug.LogWarning("Character2Attack not set in StaminaManager. Please assign it in the inspector.");
        }
    }

    void Update()
    {
        UpdateStaminaBar(staminaFill1, character1Attack, reverseStaminaBar1);
        UpdateStaminaBar(staminaFill2, character2Attack, reverseStaminaBar2);
    }
    
    void UpdateStaminaBar(Transform staminaFill, CharacterAttack characterAttack, bool reverseDirection)
    {
        if (staminaFill == null || characterAttack == null)
            return;
        
        float staminaPercentage = characterAttack.GetStaminaPercentage();
        Vector3 originalScale = (staminaFill == staminaFill1) ? staminaFill1OriginalScale : staminaFill2OriginalScale;
        Vector3 originalPosition = (staminaFill == staminaFill1) ? staminaFill1OriginalPosition : staminaFill2OriginalPosition;
        
        float reduction = (1 - staminaPercentage) * originalScale.x;
        Vector3 newScale = originalScale;
        newScale.x = originalScale.x * staminaPercentage;
        staminaFill.localScale = newScale;
        
        Vector3 newPosition = originalPosition;
        float shift = reduction * 0.5f;
        if (reverseDirection)
            newPosition.x -= shift;
        else
            newPosition.x += shift;
        staminaFill.localPosition = newPosition;
    }
}