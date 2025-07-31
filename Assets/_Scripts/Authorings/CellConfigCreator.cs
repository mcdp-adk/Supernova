using System;
using System.IO;
using UnityEngine;
using _Scripts.Utilities;

namespace _Scripts.Authorings
{
    public class CellConfigCreator : MonoBehaviour
    {
        [SerializeField] private string csvPath;

        private void OnEnable()
        {
            var lines = File.ReadAllLines(csvPath);

            for (var i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');

                var config = new CellConfig
                {
                    Type = Enum.TryParse(values[1], out CellTypeEnum type) ? type : CellTypeEnum.None,
                    State = Enum.TryParse(values[2], out CellStateEnum state) ? state : CellStateEnum.None,
                    Mass = int.TryParse(values[3], out var mass) ? mass : 1
                };

                Debug.Log($"配置: {values[0]} - Type:{config.Type}, State:{config.State}, Mass:{config.Mass}");
            }
        }
    }
}