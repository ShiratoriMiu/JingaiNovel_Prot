using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    // This method will be called by the "Start Game" button
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }
}
