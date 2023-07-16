using HarmonyLib;

namespace TownOfUs
{
    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.HandleHud))]
    public class KeyboardJoystickPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (FastDestroyableSingleton<HudManager>.Instance != null && FastDestroyableSingleton<HudManager>.Instance.ImpostorVentButton != null && FastDestroyableSingleton<HudManager>.Instance.ImpostorVentButton.isActiveAndEnabled && ConsoleJoystick.player.GetButtonDown(50))
                FastDestroyableSingleton<HudManager>.Instance.ImpostorVentButton.DoClick();
        }
    }
}
