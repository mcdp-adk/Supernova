using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using _Scripts.Utilities;
using DG.Tweening;

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

        [Header("Oxygen Bar 设置")] [SerializeField]
        private Gradient currentOxygenBarGradient;

        [SerializeField] private Image currentOxygenBar;
        [SerializeField] private Image maxOxygenBar;
        [SerializeField] private float fillSpeed = 0.25f;

        private void Update()
        {
            if (!GameManager.Instance || !GameManager.Instance.spaceFighterController) return;

            UpdateInventoryUI();
            UpdateCubeUI();
            UpdateOxygenBarUI();
        }

        private void UpdateOxygenBarUI()
        {
            var spaceFighter = GameManager.Instance.spaceFighterController;
            var oxygenByMax = spaceFighter.CurrentOxygen / spaceFighter.MaxOxygen;
            var oxygenByUltimate = spaceFighter.CurrentOxygen / spaceFighter.UltimateOxygen;
            var oxygenMaxByUltimate = spaceFighter.MaxOxygen / spaceFighter.UltimateOxygen;
            currentOxygenBar.DOColor(currentOxygenBarGradient.Evaluate(oxygenByMax), fillSpeed);
            currentOxygenBar.DOFillAmount(oxygenByUltimate, fillSpeed);
            maxOxygenBar.DOFillAmount(oxygenMaxByUltimate, fillSpeed);
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

        private void UpdateCubeUI()
        {
            var spaceFighter = GameManager.Instance.spaceFighterController;
            var currentIndex = spaceFighter.CurrentCellIndex;
            var inventoryLength = spaceFighter.CellInventory.Length;

            // CurrentCell
            var currentCellType = GetCellTypeByIndex(currentIndex);
            var currentInventory = spaceFighter.CellInventory[currentIndex];

            if (cubeCurrentImage && currentCellType != CellTypeEnum.None)
                cubeCurrentImage.sprite = GetIconByCellType(currentCellType);
            if (cubeCurrentCountText)
                cubeCurrentCountText.text = currentInventory.Count.ToString();

            // Next1
            var next1Index = (currentIndex + 1) % inventoryLength;
            var next1CellType = GetCellTypeByIndex(next1Index);
            var next1Inventory = spaceFighter.CellInventory[next1Index];

            if (cubeNext1Image && next1CellType != CellTypeEnum.None)
                cubeNext1Image.sprite = GetIconByCellType(next1CellType);
            if (cubeNext1CountText)
                cubeNext1CountText.text = next1Inventory.Count.ToString();

            // Next2
            var next2Index = (currentIndex + 2) % inventoryLength;
            var next2CellType = GetCellTypeByIndex(next2Index);
            var next2Inventory = spaceFighter.CellInventory[next2Index];

            if (cubeNext2Image && next2CellType != CellTypeEnum.None)
                cubeNext2Image.sprite = GetIconByCellType(next2CellType);
            if (cubeNext2CountText)
                cubeNext2CountText.text = next2Inventory.Count.ToString();

            // Previous1
            var prev1Index = (currentIndex - 1 + inventoryLength) % inventoryLength;
            var prev1CellType = GetCellTypeByIndex(prev1Index);
            var prev1Inventory = spaceFighter.CellInventory[prev1Index];

            if (cubePrevious1Image && prev1CellType != CellTypeEnum.None)
                cubePrevious1Image.sprite = GetIconByCellType(prev1CellType);
            if (cubePrevious1CountText)
                cubePrevious1CountText.text = prev1Inventory.Count.ToString();

            // Previous2
            var prev2Index = (currentIndex - 2 + inventoryLength) % inventoryLength;
            var prev2CellType = GetCellTypeByIndex(prev2Index);
            var prev2Inventory = spaceFighter.CellInventory[prev2Index];

            if (cubePrevious2Image && prev2CellType != CellTypeEnum.None)
                cubePrevious2Image.sprite = GetIconByCellType(prev2CellType);
            if (cubePrevious2CountText)
                cubePrevious2CountText.text = prev2Inventory.Count.ToString();
        }

        private static CellTypeEnum GetCellTypeByIndex(int index)
        {
            return (from kvp in GameManager.CellTypeIndexMap where kvp.Value == index select kvp.Key).FirstOrDefault();
        }

        private Sprite GetIconByCellType(CellTypeEnum cellType)
        {
            return (from uiData in inventoryUIData where uiData.cellType == cellType select uiData.icon)
                .FirstOrDefault();
        }
    }
}