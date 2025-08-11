using _Scripts.Components;
using _Scripts.Systems;
using _Scripts.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace _Scripts
{
    public class EarthCalculator : MonoBehaviour
    {
        public int3 EarthCenter { get; private set; } = new(0, 0, 0);
        public int EarthRadius { get; private set; }

        // ECS 相关
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeArray<CellConfig> _cellConfigs;
        private World _world;
        private EntityManager _entityManager;

        private void Start()
        {
            // ECS 相关初始化
            _world = World.DefaultGameObjectInjectionWorld;
            _entityManager = _world.EntityManager;

            // 获取 CellMap 和 CellConfigs 引用
            var globalDataSystem = _world.GetExistingSystemManaged<GlobalDataInitSystem>();
            _cellMap = globalDataSystem.CellMap;
            _cellConfigs = globalDataSystem.CellConfigs;
        }

        private void FixedUpdate()
        {
            if (!_cellMap.IsCreated) return;

            UpdateEarthRadius();
        }

        private void UpdateEarthRadius()
        {
            // 检查地球中心是否有方块
            if (!_cellMap.ContainsKey(EarthCenter))
            {
                EarthRadius = 0;
                return;
            }

            var radius = 0;

            // 从半径 1 开始逐圈检查
            while (true)
            {
                radius++;
                var hasBlockInThisRadius = false;

                // 遍历当前半径圈上的所有可能位置
                for (var x = EarthCenter.x - radius; x <= EarthCenter.x + radius; x++)
                {
                    for (var y = EarthCenter.y - radius; y <= EarthCenter.y + radius; y++)
                    {
                        for (var z = EarthCenter.z - radius; z <= EarthCenter.z + radius; z++)
                        {
                            var pos = new int3(x, y, z);
                            // 计算曼哈顿距离，只检查距离等于当前半径的位置
                            var distance = math.abs(pos.x - EarthCenter.x) +
                                           math.abs(pos.y - EarthCenter.y) +
                                           math.abs(pos.z - EarthCenter.z);
                            if (distance != radius) continue;
                            // 检查该位置是否有方块
                            if (!_cellMap.TryGetValue(pos, out var entity)) continue;
                            if (!_entityManager.Exists(entity) ||
                                !_entityManager.HasComponent<CellTag>(entity)) continue;
                            hasBlockInThisRadius = true;
                            break;
                        }

                        if (hasBlockInThisRadius) break;
                    }

                    if (hasBlockInThisRadius) break;
                }

                // 如果这一圈没有方块，停止
                if (hasBlockInThisRadius) continue;
                radius--; // 回到上一个有方块的半径
                break;
            }

            EarthRadius = radius;
        }
    }
}