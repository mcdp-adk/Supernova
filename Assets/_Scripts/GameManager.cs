using System.Collections;
using System.Collections.Generic;
using _Scripts.Utilities;
using Unity.Entities;
using UnityEngine;
using Cursor = UnityEngine.Cursor;

namespace _Scripts
{
    public class GameManager : MonoBehaviour
    {
        #region 变量和属性

        [Header("游戏设置")] [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject projectilePrefab;

        [Header("UI 设置")] [SerializeField] private GameObject fpsCounterUI;
        [SerializeField] private GameObject startUI;
        [SerializeField] private GameObject inGameUI;
        [SerializeField] private GameObject settingUI;
        [SerializeField] private GameObject toolUI;
        [SerializeField] private GameObject gameOverUI;

        public static GameManager Instance { get; private set; }

        public static readonly Dictionary<CellTypeEnum, int> cellTypeIndexMap = new()
        {
            { CellTypeEnum.Grass, 0 },
            { CellTypeEnum.Soil, 1 },
            { CellTypeEnum.Ground, 2 },
            { CellTypeEnum.GroundDry, 3 },
            { CellTypeEnum.Water, 4 },
            { CellTypeEnum.Ice, 5 },
            { CellTypeEnum.Snow, 6 },
            { CellTypeEnum.WoodWet, 7 },
            { CellTypeEnum.Wood, 8 },
            { CellTypeEnum.WoodScorched, 9 },
            { CellTypeEnum.Lava, 10 },
            { CellTypeEnum.RockVolcanic, 11 },
            { CellTypeEnum.Sand, 12 },
            { CellTypeEnum.Concrete, 13 },
            { CellTypeEnum.StoneSlate, 14 },
            { CellTypeEnum.StoneRiver, 15 },
            { CellTypeEnum.StoneGranite, 16 },
            { CellTypeEnum.StoneBasalt, 17 },
            { CellTypeEnum.RockBedrock, 18 }
        };

        public bool IsGameStarted { get; private set; }
        public bool IsMenuOpened { get; private set; }

        [HideInInspector] public GameObject spaceship;
        [HideInInspector] public SpaceFighterController spaceFighterController;
        [HideInInspector] public int[] toolCount = new int[12];
        [HideInInspector] public EarthCalculator earthCalculator;

        #endregion

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

        public void OnGameOver(bool showUI)
        {
            gameOverUI.SetActive(showUI);
        }

        public void ShotProjectile(Vector3 position, Vector3 direction, float speed)
        {
            var projectile = Instantiate(projectilePrefab, position, Quaternion.identity);
            var projectileController = projectile.GetComponent<ProjectileController>();
            projectileController.Initialize(direction, speed, toolCount);

            StartCoroutine(DestroyAfterDelay(projectile, 30f));
        }

        public void AddToolCount(int index)
        {
            toolCount[index]++;
        }

        public void ResetToolCount()
        {
            toolCount = new int[12];
        }

        public void ChangeFPSCounterActive()
        {
            fpsCounterUI.SetActive(!fpsCounterUI.activeSelf);
        }

        public void ChangeToolActive()
        {
            if (!IsGameStarted) return;
            var shouldToolUIOpen = !toolUI.activeSelf;
            toolUI.SetActive(shouldToolUIOpen);

            if (shouldToolUIOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

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

            // 直接在 GameManager 上添加 EarthCalculator 组件
            if (earthCalculator == null)
                earthCalculator = gameObject.AddComponent<EarthCalculator>();
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

        private static IEnumerator DestroyAfterDelay(GameObject obj, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (obj) Destroy(obj);
        }

        private void SpawnPlayer()
        {
            if (playerPrefab != null && spawnPoint != null)
            {
                spaceship = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                spaceFighterController = spaceship.GetComponent<SpaceFighterController>();
            }
            else
            {
                Debug.LogError("Player prefab or spawn point not assigned in GameManager");
            }
        }

        #endregion
    }
}