using _Scripts.Utilities;
using Unity.Entities;
using UnityEngine;

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
        
        private bool _isGameStarted = false;

        private static GameManager Instance { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        #region 公共方法

        public void OnGameStart()
        {
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
            SetWorldUpdateEnabled(false);
            inGameUI.SetActive(false);
            startUI.SetActive(true);
            settingUI.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SetWorldUpdateEnabled(false);
        }

        public void OnGameResume()
        {
            SetWorldUpdateEnabled(true);
            inGameUI.SetActive(true);
            startUI.SetActive(false);
            settingUI.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SetWorldUpdateEnabled(true);
        }

        #endregion

        #region 辅助方法

        private void SpawnPlayer()
        {
            if (playerPrefab != null && spawnPoint != null)
            {
                Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            }
            else
            {
                Debug.LogError("Player prefab or spawn point not assigned in GameManager");
            }
        }

        private static void SetWorldUpdateEnabled(bool shouldEnable)
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
    }
}