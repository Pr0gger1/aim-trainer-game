using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UI
{
    public class PauseModeController : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenu;
        private bool _isPaused = false;
        
        void Update()
        {
            bool isEscape = Keyboard.current.escapeKey.wasPressedThisFrame;
            
            if (isEscape)
                TogglePause();
        }

        private void Start()
        {
            ClosePauseMenu();
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            
            pauseMenu.SetActive(_isPaused);
            Time.timeScale = _isPaused ? 0 : 1;
            Cursor.lockState = _isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _isPaused;
        }
        
        public void ExitMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void ClosePauseMenu()
        {
            _isPaused = false;
            pauseMenu.SetActive(false);
        }
    }
}
