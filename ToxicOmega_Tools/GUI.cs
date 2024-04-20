using UnityEngine;

namespace ToxicOmega_Tools
{
    internal class GUI : MonoBehaviour
    {
        internal static bool visible = false;
        internal static string posLabelText;
        internal static string itemListText;
        internal static string terminalObjListText;
        internal static string enemyListText;

        void OnGUI()
        {
            if (!visible) 
                return;

            UnityEngine.GUI.Label(new Rect((Screen.width / 2) - (Screen.width / 4), (Screen.height / 2) - (Screen.height / 4), Screen.width / 2, Screen.height / 2), itemListText);
            UnityEngine.GUI.Label(new Rect((Screen.width / 2) - 50, (Screen.height / 2) - (Screen.height / 4), Screen.width / 2, Screen.height / 2), terminalObjListText);
            UnityEngine.GUI.Label(new Rect((Screen.width / 2) + (Screen.width / 4), (Screen.height / 2) - (Screen.height / 4), Screen.width / 2, Screen.height / 2), enemyListText);
            UnityEngine.GUI.Label(new Rect((Screen.width / 2) - 75, (Screen.height / 2) + (Screen.height / 4), 150, 150f), posLabelText);
        }
    }
}
