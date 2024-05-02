using BepInEx;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToxicOmega_Tools.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerB_Patch : MonoBehaviour
    {
        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPostfix]
        static void DeadPlayerEnableHUD(PlayerControllerB __instance)
        {
            if (Plugin.CheckPlayerIsHost(__instance))
            {
                HUDManager HUD = HUDManager.Instance;
                HUD.HideHUD(false);
                HUD.ToggleHUD(true);
            }
        }

        [HarmonyPatch(nameof(PlayerControllerB.AllowPlayerDeath))]
        [HarmonyPrefix]
        static bool OverrideDeath(PlayerControllerB __instance)
        {
            if (!Plugin.CheckPlayerIsHost(__instance))
                return true;
            return !Plugin.godmode;
        }

        [HarmonyPatch(nameof(PlayerControllerB.Update))]
        [HarmonyPostfix]
        static void Update(PlayerControllerB __instance)
        {
            if (CustomGUI.nearbyVisible || CustomGUI.fullListVisible)
            {
                Vector3 localPosition = (__instance.isPlayerDead && __instance.spectatedPlayerScript != null) ? __instance.spectatedPlayerScript.transform.position : __instance.transform.position;
                CustomGUI.posLabelText = $"Time: {(RoundManager.Instance.timeScript.hour + 6 > 12 ? RoundManager.Instance.timeScript.hour - 6 : RoundManager.Instance.timeScript.hour + 6)}{(RoundManager.Instance.timeScript.hour + 6 < 12 ? "am" : "pm")}\n";
                CustomGUI.posLabelText += $"GodMode: {(Plugin.godmode ? "Enabled" : "Disabled")}\n";
                CustomGUI.posLabelText += $"X: {Math.Round(localPosition.x, 1)}\nY: {Math.Round(localPosition.y, 1)}\nZ: {Math.Round(localPosition.z, 1)}";
            }

            NoClipHandler();
            DefogHandler();
        }

        private static void NoClipHandler()
        {
            if (SceneManager.GetActiveScene().name != "SampleSceneRelay")
                return;
            var player = GameNetworkManager.Instance.localPlayerController;
            if (player == null)
                return;
            var camera = player.gameplayCamera.transform;
            if (camera == null)
                return;
            var collider = player.GetComponent<CharacterController>() as Collider;
            if (collider == null)
                return;

            if (Plugin.noclip)
            {
                collider.enabled = false;
                var dir = new Vector3();

                // Horizontal
                if (UnityInput.Current.GetKey(KeyCode.W))
                    dir += camera.forward;
                if (UnityInput.Current.GetKey(KeyCode.S))
                    dir += camera.forward * -1;
                if (UnityInput.Current.GetKey(KeyCode.D))
                    dir += camera.right;
                if (UnityInput.Current.GetKey(KeyCode.A))
                    dir += camera.right * -1;

                // Vertical
                if (UnityInput.Current.GetKey(KeyCode.Space))
                    dir.y += camera.up.y;
                if (UnityInput.Current.GetKey(KeyCode.C))
                    dir.y += camera.up.y * -1;

                var prevPos = player.transform.localPosition;
                if (prevPos.Equals(Vector3.zero))
                    return;
                if (!player.isTypingChat)
                {
                    var newPos = prevPos + dir * ((UnityInput.Current.GetKey(KeyCode.LeftShift) ? 15f : 5f) * Time.deltaTime);
                    if (newPos.y < -100f && !player.isInsideFactory)
                    {
                        Plugin.PlayerTeleportEffects(player.playerClientId, true, false);
                    }
                    else if (newPos.y >= -100f && player.isInsideFactory)
                    {
                        Plugin.PlayerTeleportEffects(player.playerClientId, false, false);
                    }
                    player.transform.localPosition = newPos;
                }
            }
            else
            {
                collider.enabled = true;
            }
        }

        private static void DefogHandler()
        {
            GameObject.Find("Systems")?.transform.Find("Rendering")?.Find("VolumeMain")?.gameObject.SetActive(!Plugin.defog);
            GameObject.Find("Environment")?.transform.Find("Lighting")?.Find("GroundFog")?.gameObject.SetActive(!Plugin.defog);
            GameObject.Find("Environment")?.transform.Find("Lighting")?.Find("BrightDay")?.Find("Sun")?.Find("SunAnimContainer")?.Find("StormVolume")?.gameObject.SetActive(!Plugin.defog);
            //GameObject.Find("Environment")?.transform.Find("Lighting")?.Find("BrightDay")?.Find("Local Volumetric Fog")?.gameObject.SetActive(!Plugin.defog);
            //GameObject.Find("Environment")?.transform.Find("Lighting")?.Find("BrightDay")?.Find("Sun")?.Find("BlizzardSunAnimContainer")?.Find("Sky and Fog Global Volume")?.gameObject.SetActive(!Plugin.defog);
            //RenderSettings.fog = !Plugin.defog;
        }
    }
}
