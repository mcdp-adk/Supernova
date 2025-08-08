using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts
{
    public class GameManager : MonoBehaviour
    {
        [Header("游戏设置")] [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject playerPrefab;

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
    }
}