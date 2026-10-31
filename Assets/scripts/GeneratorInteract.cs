using UnityEngine;

public class GeneratorInteract : MonoBehaviour
{
    public GeneratorMiniGame generatorMiniGame;

    public void Activate()
    {
        if (generatorMiniGame != null)
        {
            generatorMiniGame.ActivateGenerator();
        }
        else
        {
            Debug.LogWarning("Generator Mini Game is not assigned!");
        }
    }
}