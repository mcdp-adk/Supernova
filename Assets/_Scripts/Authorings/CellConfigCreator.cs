using System;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.Utilities;
using Unity.Entities;

namespace _Scripts.Authorings
{
    public class CellConfigCreator : MonoBehaviour
    {
        [SerializeField] private TextAsset csvAsset;
        private static CellConfigCreator Instance { get; set; }
        private Entity _cellConfigEntity;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (csvAsset == null)
            {
                Debug.LogError("[CellConfigCreator] CSV Asset 未设置，请在 Inspector 中设置 CSV 文件。");
                return;
            }

            var configs = ParseCellConfigsFromCsv(csvAsset.text);
            CreateCellConfigEntity(configs);
        }

        private static List<CellConfig> ParseCellConfigsFromCsv(string csvText)
        {
            var configs = new List<CellConfig>();
            var lines = csvText.Split('\n');

            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var values = line.Split(',');
                var config = new CellConfig
                {
                    Type = Enum.TryParse(values[1], true, out CellTypeEnum type) ? type : CellTypeEnum.None,
                    State = Enum.TryParse(values[2], true, out CellStateEnum state) ? state : CellStateEnum.None,
                    Mass = int.TryParse(values[3], out var mass) ? mass : 1,
                    Fluidity = float.TryParse(values[4], out var fluidity) ? fluidity : 0f,
                    Viscosity = float.TryParse(values[5], out var viscosity) ? viscosity : 0f,
                    TemperatureDefault = float.TryParse(values[6], out var tempDefault) ? tempDefault : 20f,
                    TemperatureMin = float.TryParse(values[7], out var tempMin) ? tempMin : -99999f,
                    TemperatureMax = float.TryParse(values[8], out var tempMax) ? tempMax : 99999f,
                    HeatConductivity = float.TryParse(values[9], out var heatCond) ? heatCond : 0.5f,
                    IgnitionPoint = float.TryParse(values[10], out var ignition) ? ignition : 99999f,
                    ExplosionPoint = float.TryParse(values[11], out var explosion) ? explosion : 99999f,
                    EnergyDefault = float.TryParse(values[12], out var energy) ? energy : 0f,
                    MoistureDefault = float.TryParse(values[13], out var moistDefault) ? moistDefault : 0f,
                    MoistureMin = float.TryParse(values[14], out var moistMin) ? moistMin : 0f,
                    MoistureMax = float.TryParse(values[15], out var moistMax) ? moistMax : 1f,
                    DropChanceGold = float.TryParse(values[16], out var gold) ? gold : 0f,
                    DropChanceSilver = float.TryParse(values[17], out var silver) ? silver : 0f,
                    DropChanceCopper = float.TryParse(values[18], out var copper) ? copper : 0f,
                    DropChanceIron = float.TryParse(values[19], out var iron) ? iron : 0f
                };

                configs.Add(config);
            }

            return configs;
        }

        private void CreateCellConfigEntity(List<CellConfig> configs)
        {
            _cellConfigEntity = CellUtility.CreateCellConfigEntity("Cell_Configs",
                World.DefaultGameObjectInjectionWorld.EntityManager, configs);

            Debug.Log("[CellConfigCreator] Cell Config Entity 创建完成");
        }
    }
}