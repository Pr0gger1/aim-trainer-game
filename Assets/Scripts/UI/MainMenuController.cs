using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenuController: MonoBehaviour
    {
        public void Play()
        {
            SceneManager.LoadScene("Game");
        }

        public void Quit()
        {
            Application.Quit();
        }
    }
}