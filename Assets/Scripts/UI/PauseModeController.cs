using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UI
{
    public class PauseModeController : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject firstSelectedButton;

        private bool _isPaused = false;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                TogglePause();
        }

        private void Start()
        {
            ClosePauseMenu();
        }

        public void TogglePause()
        {
            if (_isPaused)
                Continue();
            else
                Pause();
        }

        private void Pause()
        {
            _isPaused = true;

            if (pauseMenu != null)
                pauseMenu.SetActive(true);

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (firstSelectedButton != null)
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(firstSelectedButton);
        }

        public void Continue()
        {
            _isPaused = false;

            if (pauseMenu != null)
                pauseMenu.SetActive(false);

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ClosePauseMenu()
        {
            Continue();
        }

        public void ExitMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("MainMenu");
        }
    }
}