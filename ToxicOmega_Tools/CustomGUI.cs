using UnityEngine;

namespace ToxicOmega_Tools
{
    internal class CustomGUI : MonoBehaviour
    {
        internal static bool visible = false;
        internal static bool isFullList = false;
        internal static string posLabelText;
        internal static string itemListText;
        internal static string terminalObjListText;
        internal static string enemyListText;

        void OnGUI()
        {
            if (!visible && !isFullList)
                return;

            GUI.Label(new Rect(Screen.width / 4, Screen.height / 8, Screen.width / 2, Screen.height * 0.75f), itemListText);
            GUI.Label(new Rect(Screen.width / 2, Screen.height / 8, Screen.width / 2, Screen.height * 0.75f), terminalObjListText);
            GUI.Label(new Rect(Screen.width * 0.75f, Screen.height / 8, Screen.width / 2, Screen.height * 0.75f), enemyListText);
            GUI.Label(new Rect((Screen.width / 2) - 75, Screen.height * 0.75f, 150, 150f), posLabelText);
        }
    }
}
