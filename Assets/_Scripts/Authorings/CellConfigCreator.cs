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
        private Entity _cellConfigEntity;

        private void Start()
        {
            if (csvAsset == null)
            {
                Debug.LogError("[CellConfigCreator] CSV Asset 未设置，请在 Inspector 中设置 CSV 文件。");
                return;
            }

            var configs = new List<CellConfig>();
            var lines = csvAsset.text.Split('\n');

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

            _cellConfigEntity = CellUtility.CreateCellConfigEntity("CellConfig",
                World.DefaultGameObjectInjectionWorld.EntityManager, configs);

            Debug.Log("[CellConfigCreator] Cell Config Entity 创建完成");
        }
    }
}