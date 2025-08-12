using UnityEngine;
using UnityEngine.UI;

namespace _Scripts
{
    public class ToolUIController : MonoBehaviour
    {
        [SerializeField] private Text grassToolText;
        [SerializeField] private Text soilToolText;
        [SerializeField] private Text groundToolText;
        [SerializeField] private Text groundDryToolText;
        [SerializeField] private Text waterToolText;
        [SerializeField] private Text iceToolText;
        [SerializeField] private Text snowToolText;
        [SerializeField] private Text woodWetToolText;
        [SerializeField] private Text woodToolText;
        [SerializeField] private Text woodScorchedToolText;
        [SerializeField] private Text lavaToolText;
        [SerializeField] private Text rockVolcanicToolText;

        private void Update()
        {
            if (!GameManager.Instance) return;

            grassToolText.text = GameManager.Instance.toolCount[0].ToString();
            soilToolText.text = GameManager.Instance.toolCount[1].ToString();
            groundToolText.text = GameManager.Instance.toolCount[2].ToString();
            groundDryToolText.text = GameManager.Instance.toolCount[3].ToString();
            waterToolText.text = GameManager.Instance.toolCount[4].ToString();
            iceToolText.text = GameManager.Instance.toolCount[5].ToString();
            snowToolText.text = GameManager.Instance.toolCount[6].ToString();
            woodWetToolText.text = GameManager.Instance.toolCount[7].ToString();
            woodToolText.text = GameManager.Instance.toolCount[8].ToString();
            woodScorchedToolText.text = GameManager.Instance.toolCount[9].ToString();
            lavaToolText.text = GameManager.Instance.toolCount[10].ToString();
            rockVolcanicToolText.text = GameManager.Instance.toolCount[11].ToString();
        }
    }
}