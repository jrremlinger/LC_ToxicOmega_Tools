using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ToxicOmega_Tools.Patches
{
    internal class InvertHotbar_Patch
    {
        [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> InvertHotbarScrollDirection(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> source = new List<CodeInstruction>(instructions);
            for (int index = 1; index < source.Count; ++index)
            {
                if (source[index].opcode == OpCodes.Ble_Un && source[index - 1].opcode == OpCodes.Ldc_R4 && (double)(float)source[index - 1].operand == 0.0)
                {
                    source[index].opcode = OpCodes.Bge_Un;
                    break;
                }
            }
            Plugin.mls.LogInfo("ScrollMouse function has been patched.");
            return source.AsEnumerable();
        }
    }
}
