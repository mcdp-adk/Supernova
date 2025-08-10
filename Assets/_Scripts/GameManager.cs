using System;
using _Scripts.Utilities;
using DG.Tweening;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using Cursor = UnityEngine.Cursor;

namespace _Scripts
{
    public class GameManager : MonoBehaviour
    {
        [Header("游戏设置")] [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject playerPrefab;

        [Header("UI 设置")] [SerializeField] private GameObject fpsCounterUI;
        [SerializeField] private GameObject startUI;
        [SerializeField] private GameObject inGameUI;
        [SerializeField] private GameObject settingUI;

        [Header("Oxygen Bar 设置")] [SerializeField]
        private Gradient currentOxygenBarGradient;

        [SerializeField] private Image currentOxygenBar;
        [SerializeField] private Image maxOxygenBar;
        [SerializeField] private float fillSpeed = 0.25f;

        public static GameManager Instance { get; private set; }
        public bool IsGameStarted { get; private set; } = false;
        public bool IsMenuOpened { get; private set; } = false;

        private GameObject _spaceship;
        private SpaceFighterController _spaceFighterController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            UpdateUI();
        }

        #region 公共方法

        public void OnGameStart()
        {
            IsGameStarted = true;

            startUI.SetActive(false);
            inGameUI.SetActive(true);
            settingUI.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SetWorldUpdateEnabled(true);
            SpawnPlayer();
        }

        public void OnGameExit()
        {
            Application.Quit();
            Debug.Log("[GameManager] 游戏已退出");
        }

        public void OnGamePause()
        {
            IsMenuOpened = true;

            if (IsGameStarted)
            {
                inGameUI.SetActive(false);
                startUI.SetActive(false);
                settingUI.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                SetWorldUpdateEnabled(false);
            }
            else
            {
                inGameUI.SetActive(false);
                startUI.SetActive(false);
                settingUI.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void OnGameResume()
        {
            IsMenuOpened = false;

            if (IsGameStarted)
            {
                inGameUI.SetActive(true);
                startUI.SetActive(false);
                settingUI.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                SetWorldUpdateEnabled(true);
            }
            else
            {
                inGameUI.SetActive(false);
                startUI.SetActive(true);
                settingUI.SetActive(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public static void SetWorldUpdateEnabled(bool shouldEnable)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var caSlowSystemGroup = world.GetExistingSystemManaged<CaSlowSystemGroup>();
            var caFastSystemGroup = world.GetExistingSystemManaged<CaFastSystemGroup>();
            if (caSlowSystemGroup == null || caFastSystemGroup == null)
            {
                Debug.LogError("[GameManager] Cellular Automata 系统组未找到，请确保它们已正确添加到世界中。");
                return;
            }

            caSlowSystemGroup.Enabled = shouldEnable;
            caFastSystemGroup.Enabled = shouldEnable;

            Time.timeScale = shouldEnable ? 1f : 0f;

            Debug.Log("[GameManager] Cellular Automata 系统组已 " + (shouldEnable ? "启用" : "禁用") +
                      "，游戏时间已 " + (shouldEnable ? "继续" : "暂停"));
        }

        #endregion

        #region 辅助方法

        private void SpawnPlayer()
        {
            if (playerPrefab != null && spawnPoint != null)
            {
                _spaceship = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                _spaceFighterController = _spaceship.GetComponent<SpaceFighterController>();
            }
            else
            {
                Debug.LogError("Player prefab or spawn point not assigned in GameManager");
            }
        }

        private void UpdateUI()
        {
            if (!_spaceFighterController) return;

            var oxygenByMax = _spaceFighterController.CurrentOxygen / _spaceFighterController.MaxOxygen;
            var oxygenByUltimate = _spaceFighterController.CurrentOxygen / _spaceFighterController.UltimateOxygen;
            var oxygenMaxByUltimate = _spaceFighterController.MaxOxygen / _spaceFighterController.UltimateOxygen;
            currentOxygenBar.DOColor(currentOxygenBarGradient.Evaluate(oxygenByMax), fillSpeed);
            currentOxygenBar.DOFillAmount(oxygenByUltimate, fillSpeed);
            maxOxygenBar.DOFillAmount(oxygenMaxByUltimate, fillSpeed);
        }

        #endregion
    }
}