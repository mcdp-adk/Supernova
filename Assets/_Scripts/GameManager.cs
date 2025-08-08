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

        public void OnGameStart()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            startUI.SetActive(false);
            inGameUI.SetActive(true);

            EnableSystemsUpdate();
            SpawnPlayer();
        }

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

        private static void EnableSystemsUpdate()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var caSlowSystemGroup = world.GetExistingSystemManaged<CaSlowSystemGroup>();
            var caFastSystemGroup = world.GetExistingSystemManaged<CaFastSystemGroup>();
            if (caSlowSystemGroup != null && caFastSystemGroup != null)
            {
                caSlowSystemGroup.Enabled = true;
                caFastSystemGroup.Enabled = true;
            }

            Debug.Log("[GameManager] Cellular Automata 系统更新已启用。");
        }
    }
}