using HarmonyLib;
using UnityEngine;

namespace TownOfUs.Patches {
    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
    static class LobbyBehaviourPatch {
        [HarmonyPostfix]
        public static void Postfix() {
            // Fix Grenadier blind in lobby
            ((Renderer)FastDestroyableSingleton<HudManager>.Instance.FullScreen).gameObject.active = false;
        }
    }
}
