using UnityEngine;

public class SceneSetupManager : MonoBehaviour
{
    public GameObject player1Object; 
    public GameObject player2Object; 

    void Start()
    {
        var wm = WholeManager.Instance;
        if (wm == null)
        {
            Debug.LogError("WholeManager not found!");
            return;
        }

        GameObject knightObj = GameObject.Find("Knight");
        GameObject samuraiObj = GameObject.Find("Samurai");

        if (wm.gameMode == WholeManager.GameMode.PvP)
        {
            SetupCharacter(knightObj, "Knight", false);
            SetupCharacter(samuraiObj, "Samurai", false);
        }
        else if (wm.gameMode == WholeManager.GameMode.PvE)
        {
            if (wm.player1Character == "Knight")
            {
                SetupCharacter(knightObj, "Knight", false);
                SetupCharacter(samuraiObj, "Samurai", true, wm.aiDifficulty);
            }
            else
            {
                SetupCharacter(samuraiObj, "Samurai", false);
                SetupCharacter(knightObj, "Knight", true, wm.aiDifficulty);
            }
        }
    }

    void SetupCharacter(GameObject obj, string charID, bool useAI, string aiDifficulty = "Normal")
    {
        if (obj == null) return;


        var attack = obj.GetComponent<CharacterAttack>();
        if (attack != null)
            attack.characterID = charID;


        var ai = obj.GetComponent<OpponentAI>();
        if (ai != null)
        {
            ai.useAI = useAI;
            if (useAI)
            {
                if (aiDifficulty == "Easy") ai.difficulty = OpponentAI.Difficulty.Easy;
                else if (aiDifficulty == "Normal") ai.difficulty = OpponentAI.Difficulty.Normal;
                else if (aiDifficulty == "Hard") ai.difficulty = OpponentAI.Difficulty.Hard;
            }
        }
    }
}