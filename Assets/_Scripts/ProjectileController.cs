using _Scripts.Components;
using _Scripts.Systems;
using _Scripts.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace _Scripts
{
    public class ProjectileController : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;
        
        // 射弹数据
        private float _projectileTemperature;
        private float _projectileMoisture;
        private float _projectileEnergy;

        private NativeHashMap<int3, Entity> _cellMap;
        private NativeArray<CellConfig> _cellConfigs;
        private EntityManager _entityManager;

        public void Initialize(Vector3 direction, float speed, int[] projectileCount)
        {
            _direction = direction.normalized;
            _speed = speed;
            
            InitializeEcsData();
            CalculateProjectileData(projectileCount);
        }

        private void InitializeEcsData()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            _entityManager = world.EntityManager;
            var globalDataSystem = world.GetExistingSystemManaged<GlobalDataInitSystem>();
            _cellMap = globalDataSystem.CellMap;
            _cellConfigs = globalDataSystem.CellConfigs;
        }

        private void Update()
        {
            transform.position += Time.deltaTime * _speed * _direction;

            if (CheckCoordinateHasCell()) 
            {
                ApplyProjectileEffect();
                Destroy(gameObject);
            }
        }

        private void CalculateProjectileData(int[] projectileCount)
        {
            var spaceFighter = GameManager.Instance.spaceFighterController;
            if (spaceFighter == null) 
            {
                Destroy(gameObject);
                return;
            }

            float totalTemperature = 0f;
            float totalMoisture = 0f;
            float totalEnergy = 0f;
            int totalCount = 0;
            
            for (int i = 0; i < projectileCount.Length; i++)
            {
                var useCount = projectileCount[i];
                if (useCount <= 0) continue;

                var cellType = GetCellTypeByToolIndex(i);
                if (cellType == CellTypeEnum.None) continue;

                if (!GameManager.cellTypeIndexMap.TryGetValue(cellType, out var inventoryIndex)) continue;
                if (inventoryIndex >= spaceFighter.CellInventory.Length) continue;

                var inventory = spaceFighter.CellInventory[inventoryIndex];
                var actualUseCount = Mathf.Min(inventory.Count, useCount);
                
                if (actualUseCount > 0)
                {
                    totalTemperature += inventory.AvgTemperature * actualUseCount;
                    totalMoisture += inventory.AvgMoisture * actualUseCount;
                    
                    var config = _cellConfigs.GetCellConfig(cellType);
                    totalEnergy += config.EnergyDefault * actualUseCount;
                    totalCount += actualUseCount;
                    
                    // 消耗背包中的方块数量
                    inventory.Count -= actualUseCount;
                    spaceFighter.CellInventory[inventoryIndex] = inventory;
                }
            }

            if (totalCount > 0)
            {
                _projectileTemperature = totalTemperature / totalCount;
                _projectileMoisture = totalMoisture / totalCount;
                _projectileEnergy = totalEnergy / totalCount;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private CellTypeEnum GetCellTypeByToolIndex(int toolIndex)
        {
            return toolIndex switch
            {
                0 => CellTypeEnum.Grass,
                1 => CellTypeEnum.Soil,
                2 => CellTypeEnum.Ground,
                3 => CellTypeEnum.GroundDry,
                4 => CellTypeEnum.Water,
                5 => CellTypeEnum.Ice,
                6 => CellTypeEnum.Snow,
                7 => CellTypeEnum.WoodWet,
                8 => CellTypeEnum.Wood,
                9 => CellTypeEnum.WoodScorched,
                10 => CellTypeEnum.Lava,
                11 => CellTypeEnum.RockVolcanic,
                _ => CellTypeEnum.None
            };
        }

        private bool CheckCoordinateHasCell()
        {
            var currentPos = new int3(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y),
                Mathf.RoundToInt(transform.position.z)
            );

            if (!_cellMap.TryGetValue(currentPos, out var cellEntity)) return false;
            return _entityManager.Exists(cellEntity) && _entityManager.HasComponent<CellTag>(cellEntity);
        }

        private void ApplyProjectileEffect()
        {
            var impactPos = new int3(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y),
                Mathf.RoundToInt(transform.position.z)
            );

            var baseRadius = Mathf.Sqrt(_projectileEnergy * 0.01f);
            var maxRadius = Mathf.CeilToInt(baseRadius);

            for (int x = -maxRadius; x <= maxRadius; x++)
            {
                for (int y = -maxRadius; y <= maxRadius; y++)
                {
                    for (int z = -maxRadius; z <= maxRadius; z++)
                    {
                        var offset = new int3(x, y, z);
                        var targetPos = impactPos + offset;
                        var distance = math.length(offset);
                        
                        if (distance > baseRadius) continue;
                        
                        if (_cellMap.TryGetValue(targetPos, out var cellEntity) && 
                            _entityManager.Exists(cellEntity) &&
                            _entityManager.HasComponent<CellTag>(cellEntity))
                        {
                            var effectStrength = 1f - (distance / baseRadius);
                            effectStrength = math.max(0f, effectStrength);
                            ApplyEffectToCell(cellEntity, effectStrength);
                        }
                    }
                }
            }
        }

        private void ApplyEffectToCell(Entity cellEntity, float effectStrength)
        {
            if (_projectileTemperature > 0 && _entityManager.HasBuffer<HeatBuffer>(cellEntity))
            {
                var heatBuffer = _entityManager.GetBuffer<HeatBuffer>(cellEntity);
                var heatAmount = _projectileTemperature * effectStrength;
                heatBuffer.Add(new HeatBuffer { Value = heatAmount });
            }

            if (_projectileMoisture > 0 && _entityManager.HasBuffer<MoistureBuffer>(cellEntity))
            {
                var moistureBuffer = _entityManager.GetBuffer<MoistureBuffer>(cellEntity);
                var moistureAmount = _projectileMoisture * effectStrength;
                moistureBuffer.Add(new MoistureBuffer { Value = moistureAmount });
            }

            if (_projectileEnergy > 0 && _entityManager.HasBuffer<ImpulseBuffer>(cellEntity))
            {
                var impulseBuffer = _entityManager.GetBuffer<ImpulseBuffer>(cellEntity);
                var impulseStrength = _projectileEnergy * effectStrength * 0.01f;
                var impulse = new float3(_direction.x, _direction.y, _direction.z) * impulseStrength * effectStrength;
                impulseBuffer.Add(new ImpulseBuffer { Value = impulse });
            }
        }
    }
}
