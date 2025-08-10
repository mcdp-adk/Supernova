using UnityEngine;
using UnityEngine.UI;
using _Scripts.Utilities;

namespace _Scripts
{
    public class InGameUIController : MonoBehaviour
    {
        [Header("Cubes 区域设置")] [SerializeField]
        private Image cubeCurrentImage;

        [SerializeField] private Text cubeCurrentCountText;
        [SerializeField] private Image cubeNext1Image;
        [SerializeField] private Text cubeNext1CountText;
        [SerializeField] private Image cubeNext2Image;
        [SerializeField] private Text cubeNext2CountText;
        [SerializeField] private Image cubePrevious1Image;
        [SerializeField] private Text cubePrevious1CountText;
        [SerializeField] private Image cubePrevious2Image;
        [SerializeField] private Text cubePrevious2CountText;

        [Header("Cell UI 设置")] [SerializeField]
        private InventoryUIData[] inventoryUIData = new InventoryUIData[19];

        private SpaceFighterController _spaceFighterController;

        private void Update()
        {
            if (!GameManager.Instance || !GameManager.Instance.spaceFighterController) return;

            UpdateInventoryUI();
        }

        private void UpdateInventoryUI()
        {
            var spaceFighter = GameManager.Instance.spaceFighterController;
            foreach (var uiData in inventoryUIData)
            {
                var index = GameManager.CellTypeIndexMap[uiData.cellType];
                var inventory = spaceFighter.CellInventory[index];

                if (uiData.countText)
                    uiData.countText.text = inventory.Count.ToString();
            }
        }
    }
}