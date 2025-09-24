using UnityEngine;

public class InstructionUI : MonoBehaviour
{
    public static InstructionUI Instance;

    private void Awake()
    {
        // Make globally accessible
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    public void HideInstruction()
    {
        gameObject.SetActive(false);
    }
}
