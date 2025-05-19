using UnityEngine;

public class Menu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Play()
    {
        // Load the game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("Level1");
    }
    public void Quit()
    {
        // Quit the application
        Application.Quit();
    }


    
}
