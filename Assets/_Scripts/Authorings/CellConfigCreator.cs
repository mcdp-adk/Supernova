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
                    Type = Enum.TryParse(values[1], out CellTypeEnum type) ? type : CellTypeEnum.None,
                    State = Enum.TryParse(values[2], out CellStateEnum state) ? state : CellStateEnum.None,
                    Mass = int.TryParse(values[3], out var mass) ? mass : 1
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