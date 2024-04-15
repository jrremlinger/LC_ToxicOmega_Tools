using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToxicOmega_Tools.Patches;
using UnityEngine;

namespace ToxicOmega_Tools
{
    internal class TOTGUI : MonoBehaviour
    {
        internal static TOTGUI Instance;

        internal static bool visible = false;
        internal static string posLabelText;
        internal static string itemListText;
        internal static string terminalObjListText;
        internal static string enemyListText;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void OnGUI()
        {
            if (!visible) { return; }

            GUI.Label(new Rect((Screen.width / 2) - Screen.width / 4, (Screen.height / 2) - Screen.height / 4, Screen.width / 2, Screen.height / 2), itemListText);
            GUI.Label(new Rect((Screen.width / 2) - 50, (Screen.height / 2) - Screen.height / 4, Screen.width / 2, Screen.height / 2), terminalObjListText);
            GUI.Label(new Rect((Screen.width / 2) + Screen.width / 4, (Screen.height / 2) - Screen.height / 4, Screen.width / 2, Screen.height / 2), enemyListText);
            GUI.Label(new Rect((Screen.width / 2) - 50, (Screen.height / 2 + (Screen.height / 4)), 100, 52f), posLabelText);
        }
    }
}
