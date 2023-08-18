﻿// Note : Some Code take it from TownOfUsReworked https://github.com/AlchlcDvl/TownOfUsReworked/blob/master/TownOfUsReworked/Classes/ModCompatibility.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Reactor.Utilities;
using TownOfUs.Roles;
using Hazel;

namespace TownOfUs.Patches
{
    [HarmonyPatch(typeof(IntroCutscene._ShowRole_d__39), nameof(IntroCutscene._ShowRole_d__39.MoveNext))]
    public static class SubmergedStartPatch
    {
        public static void Postfix()
        {
            if (SubmergedCompatibility.isSubmerged())
            {
                Coroutines.Start(SubmergedCompatibility.waitStart(SubmergedCompatibility.resetTimers));
            }
        }
    }


    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public static class SubmergedHudPatch
    {
        public static void Postfix(HudManager __instance)
        {
            if (SubmergedCompatibility.isSubmerged())
            {
                if (PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.Is(RoleEnum.Haunter))
                {
                    if (!Role.GetRole<Haunter>(PlayerControl.LocalPlayer).Caught) __instance.MapButton.transform.parent.Find(__instance.MapButton.name + "(Clone)").gameObject.SetActive(false);
                    else __instance.MapButton.transform.parent.Find(__instance.MapButton.name + "(Clone)").gameObject.SetActive(true);
                }
                if (PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.Is(RoleEnum.Phantom))
                {
                    if (!Role.GetRole<Phantom>(PlayerControl.LocalPlayer).Caught) __instance.MapButton.transform.parent.Find(__instance.MapButton.name + "(Clone)").gameObject.SetActive(false);
                    else  __instance.MapButton.transform.parent.Find(__instance.MapButton.name + "(Clone)").gameObject.SetActive(true);
                }
            }
                
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
    [HarmonyPriority(Priority.Low)] //make sure it occurs after other patches
    public static class SubmergedPhysicsPatch
    {
        public static void Postfix(PlayerPhysics __instance)
        {
            SubmergedCompatibility.Ghostrolefix(__instance);
        }
    }
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
    [HarmonyPriority(Priority.Low)] //make sure it occurs after other patches
    public static class SubmergedLateUpdatePhysicsPatch
    {
        public static void Postfix(PlayerPhysics __instance)
        {
            SubmergedCompatibility.Ghostrolefix(__instance);
        }
    }


    public static class SubmergedCompatibility
    {
        public static class Classes
        {
            public const string ElevatorMover = "ElevatorMover";
        }

        public const string SUBMERGED_GUID = "Submerged";
        public const ShipStatus.MapType SUBMERGED_MAP_TYPE = (ShipStatus.MapType)5;

        public static SemanticVersioning.Version Version { get; private set; }
        public static bool Loaded { get; private set; }
        public static BasePlugin Plugin { get; private set; }
        public static Assembly Assembly { get; private set; }
        public static Type[] Types { get; private set; }
        public static Dictionary<string, Type> InjectedTypes { get; private set; }

        private static MonoBehaviour _submarineStatus;
        public static MonoBehaviour SubmarineStatus
        {
            get
            {
                if (!Loaded) return null;

                if (_submarineStatus is null || _submarineStatus.WasCollected || !_submarineStatus || _submarineStatus == null)
                {
                    if (ShipStatus.Instance is null || ShipStatus.Instance.WasCollected || !ShipStatus.Instance || ShipStatus.Instance == null)
                    {
                        return _submarineStatus = null;
                    }
                    else
                    {
                        if (ShipStatus.Instance.Type == SUBMERGED_MAP_TYPE)
                        {
                            return _submarineStatus = ShipStatus.Instance.GetComponent(Il2CppType.From(SubmarineStatusType))?.TryCast(SubmarineStatusType) as MonoBehaviour;
                        }
                        else
                        {
                            return _submarineStatus = null;
                        }
                    }
                }
                else
                {
                    return _submarineStatus;
                }
            }
        }

        

        private static Type SubmarineStatusType;
    private static MethodInfo CalculateLightRadiusMethod;

    private static MethodInfo RpcRequestChangeFloorMethod;
    private static Type FloorHandlerType;
    private static MethodInfo GetFloorHandlerMethod;

    private static Type VentPatchDataType;
    private static PropertyInfo InTransitionField;

    private static Type CustomTaskTypesType;
    private static FieldInfo RetrieveOxygenMaskField;

    private static Type SubmarineOxygenSystemType;
    private static PropertyInfo SubmarineOxygenSystemInstanceField;
    private static MethodInfo RepairDamageMethod;
    private static FieldInfo RetTaskType;
    public static TaskTypes RetrieveOxygenMask;

    private static Type SubmergedExileController;
    private static MethodInfo SubmergedExileWrapUpMethod;

    private static Type SubmarineElevator;
    private static MethodInfo GetInElevator;
    private static MethodInfo GetMovementStageFromTime;
    private static FieldInfo GetSubElevatorSystem;

    private static Type SubmarineElevatorSystem;
    private static FieldInfo UpperDeckIsTargetFloor;

    private static FieldInfo SubmergedInstance;
    private static FieldInfo SubmergedElevators;

    private static Type CustomPlayerData;
    private static FieldInfo HasMap;

    private static Type SpawnInState;
    private static FieldInfo CurrentState;


        public static void Initialize()
        {
            Loaded = IL2CPPChainloader.Instance.Plugins.TryGetValue(SUBMERGED_GUID, out PluginInfo plugin);
            if (!Loaded) return;

            Plugin = plugin!.Instance as BasePlugin;
            Version = plugin.Metadata.Version;

            Assembly = Plugin!.GetType().Assembly;
            Types = AccessTools.GetTypesFromAssembly(Assembly);

            InjectedTypes = (Dictionary<string, Type>)AccessTools.PropertyGetter(Types.FirstOrDefault(t => t.Name == "ComponentExtensions"), "RegisteredTypes")
                .Invoke(null, Array.Empty<object>());

            SubmarineStatusType = Types.First(t => t.Name == "SubmarineStatus");
            SubmergedInstance = AccessTools.Field(SubmarineStatusType, "instance"); 
            SubmergedElevators = AccessTools.Field(SubmarineStatusType, "elevators");
            CalculateLightRadiusMethod = AccessTools.Method(SubmarineStatusType, "CalculateLightRadius");


            FloorHandlerType = Types.First(t => t.Name == "FloorHandler");
            GetFloorHandlerMethod = AccessTools.Method(FloorHandlerType, "GetFloorHandler", new Type[] { typeof(PlayerControl) });
            RpcRequestChangeFloorMethod = AccessTools.Method(FloorHandlerType, "RpcRequestChangeFloor");

            CustomPlayerData = InjectedTypes.Where(t => t.Key == "CustomPlayerData").Select(x => x.Value).First();
            HasMap = AccessTools.Field(CustomPlayerData, "_hasMap");

            VentPatchDataType = Types.First(t => t.Name == "VentPatchData");
            InTransitionField = AccessTools.Property(VentPatchDataType, "InTransition");

            CustomTaskTypesType = Types.First(t => t.Name == "CustomTaskTypes");
            RetrieveOxygenMaskField  = AccessTools.Field(CustomTaskTypesType, "RetrieveOxygenMask");
            var retTaskType = AccessTools.Field(CustomTaskTypesType, "taskType");
            RetrieveOxygenMask = (TaskTypes)retTaskType.GetValue(RetrieveOxygenMaskField.GetValue(null));

            SubmarineOxygenSystemType = Types.First(t => t.Name == "SubmarineOxygenSystem");
            SubmarineOxygenSystemInstanceField = AccessTools.Property(SubmarineOxygenSystemType, "Instance");
            RepairDamageMethod = AccessTools.Method(SubmarineOxygenSystemType, "RepairDamage");
            SubmergedExileController = Types.First(t => t.Name == "SubmergedExileController");
            SubmergedExileWrapUpMethod = AccessTools.Method(SubmergedExileController, "WrapUpAndSpawn");

            SubmarineElevator = Types.First(t => t.Name == "SubmarineElevator");
            GetInElevator = AccessTools.Method(SubmarineElevator, "GetInElevator", new[] { typeof(PlayerControl) });
            GetMovementStageFromTime = AccessTools.Method(SubmarineElevator, "GetMovementStageFromTime");
            GetSubElevatorSystem = AccessTools.Field(SubmarineElevator, "system");

            SubmarineElevatorSystem = Types.First(t => t.Name == "SubmarineElevatorSystem");
            UpperDeckIsTargetFloor = AccessTools.Field(SubmarineElevatorSystem, "upperDeckIsTargetFloor");

            CustomPlayerData = InjectedTypes.Where(t => t.Key == "CustomPlayerData").Select(x => x.Value).First();
            HasMap = AccessTools.Field(CustomPlayerData, "_hasMap");

            SpawnInState = Types.First(t => t.Name == "SpawnInState");
            Harmony _harmony = new ("tou.submerged.patch");
           _harmony.Patch(SubmergedExileWrapUpMethod, null, new(AccessTools.Method(typeof(SubmergedCompatibility), nameof(ExileRoleChangePostfix))));
        }

        public static void CheckOutOfBoundsElevator(PlayerControl player)
        {
            if (!Loaded) return;
            if (!isSubmerged()) return;

           var (isInElevator, elevator) = GetPlayerElevator(player);

           if (isInElevator)
           return;


            var currentFloor = (bool)UpperDeckIsTargetFloor.GetValue(GetSubElevatorSystem.GetValue(elevator)); //true is top, false is bottom
            var playerFloor = player.transform.position.y > -7f; //true is top, false is bottom
            
            if (currentFloor != playerFloor)
            {
                ChangeFloor(currentFloor);
            }
        }

        public static void MoveDeadPlayerElevator(PlayerControl player)
        {
            if (!isSubmerged()) return;
            
            var (isInElevator, elevator) = GetPlayerElevator(player);
            if (!isInElevator) return;

            if ((int)GetMovementStageFromTime.Invoke(elevator, null) <= 5)

            {
                //Fade to clear
                var topfloortarget = (bool)UpperDeckIsTargetFloor.GetValue(GetSubElevatorSystem.GetValue(elevator)); //true is top, false is bottom
                var topintendedtarget = player.transform.position.y > -7f; //true is top, false is bottom
                if (topfloortarget != topintendedtarget)
                {
                    ChangeFloor(!topintendedtarget);
                }
            }
        }

        public static (bool isInElevator, object Elevator) GetPlayerElevator(PlayerControl player)
        {
            if (!isSubmerged()) return (false, null);
             foreach (var elevator in (IList)SubmergedElevators.GetValue(SubmergedInstance.GetValue(null)))
        {
            if ((bool)GetInElevator.Invoke(elevator, new[] { player }))
                return (true, elevator);
        }

        return (false, null);
        }


        public static void ExileRoleChangePostfix()
        {
            Coroutines.Start(waitMeeting(resetTimers));
            Coroutines.Start(waitMeeting(GhostRoleBegin));
        }

        public static IEnumerator waitStart (Action next)
        {
            while (DestroyableSingleton<HudManager>.Instance.UICamera.transform.Find("SpawnInMinigame(Clone)") == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
            while (DestroyableSingleton<HudManager>.Instance.UICamera.transform.Find("SpawnInMinigame(Clone)") != null)
            {
                yield return null;
            }
            next();
        }
        public static IEnumerator waitMeeting(Action next)
        {
            while (!PlayerControl.LocalPlayer.moveable)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
            while (DestroyableSingleton<HudManager>.Instance.PlayerCam.transform.Find("SpawnInMinigame(Clone)") != null)
            {
                yield return null;
            }       
            next();
        }

        public static void resetTimers()
        {
            if (PlayerControl.LocalPlayer.Data.IsDead) return;
            Utils.ResetCustomTimers();
        }


        public static void GhostRoleBegin()
        {
            if (!PlayerControl.LocalPlayer.Data.IsDead) return;
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Haunter))
            {
                if (!Role.GetRole<Haunter>(PlayerControl.LocalPlayer).Caught)
                {
                    var startingVent =
                        ShipStatus.Instance.AllVents[UnityEngine.Random.RandomRangeInt(0, ShipStatus.Instance.AllVents.Count)];
                    while (startingVent == ShipStatus.Instance.AllVents[0] || startingVent == ShipStatus.Instance.AllVents[14])
                    {
                        startingVent =
                            ShipStatus.Instance.AllVents[UnityEngine.Random.RandomRangeInt(0, ShipStatus.Instance.AllVents.Count)];
                    }
                    ChangeFloor(startingVent.transform.position.y > -7f);

                    Utils.Rpc(CustomRPC.SetPos, PlayerControl.LocalPlayer.PlayerId, startingVent.transform.position.x, startingVent.transform.position.y + 0.3636f);

                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(new Vector2(startingVent.transform.position.x, startingVent.transform.position.y + 0.3636f));
                    PlayerControl.LocalPlayer.MyPhysics.RpcEnterVent(startingVent.Id);
                }
            }
            if (PlayerControl.LocalPlayer.Is(RoleEnum.Phantom))
            {
                if (!Role.GetRole<Phantom>(PlayerControl.LocalPlayer).Caught)
                {
                    var startingVent =
                        ShipStatus.Instance.AllVents[UnityEngine.Random.RandomRangeInt(0, ShipStatus.Instance.AllVents.Count)];
                    while (startingVent == ShipStatus.Instance.AllVents[0] || startingVent == ShipStatus.Instance.AllVents[14])
                    {
                        startingVent =
                            ShipStatus.Instance.AllVents[UnityEngine.Random.RandomRangeInt(0, ShipStatus.Instance.AllVents.Count)];
                    }
                    ChangeFloor(startingVent.transform.position.y > -7f);

                    Utils.Rpc(CustomRPC.SetPos, PlayerControl.LocalPlayer.PlayerId, startingVent.transform.position.x, startingVent.transform.position.y + 0.3636f);

                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(new Vector2(startingVent.transform.position.x, startingVent.transform.position.y + 0.3636f));
                    PlayerControl.LocalPlayer.MyPhysics.RpcEnterVent(startingVent.Id);
                }
            }
        }

        public static void Ghostrolefix(PlayerPhysics __instance)
        {
            if (Loaded && __instance.myPlayer.Data.IsDead)
            {
                PlayerControl player = __instance.myPlayer;
                if (player.Is(RoleEnum.Phantom))
                {

                    if (!Role.GetRole<Phantom>(player).Caught)
                    {

                        if (player.AmOwner) MoveDeadPlayerElevator(player);
                        else player.Collider.enabled = false;
                        Transform transform = __instance.transform;
                        Vector3 position = transform.position;
                        position.z = position.y/1000;

                        transform.position = position;
                        __instance.myPlayer.gameObject.layer = 8;
                    }
                }
                if (player.Is(RoleEnum.Haunter))
                {
                    if (!Role.GetRole<Haunter>(player).Caught)
                    {

                        if (player.AmOwner) MoveDeadPlayerElevator(player);
                        else player.Collider.enabled = false;
                        Transform transform = __instance.transform;
                        Vector3 position = transform.position;
                        position.z = position.y / 1000;

                        transform.position = position;
                        __instance.myPlayer.gameObject.layer = 8;
                    }
                }
            }
        }
        public static MonoBehaviour AddSubmergedComponent(this GameObject obj, string typeName)
        {
            if (!Loaded) return obj.AddComponent<MissingSubmergedBehaviour>();
            bool validType = InjectedTypes.TryGetValue(typeName, out Type type);
            return validType ? obj.AddComponent(Il2CppType.From(type)).TryCast<MonoBehaviour>() : obj.AddComponent<MissingSubmergedBehaviour>();
        }

        public static float GetSubmergedNeutralLightRadius(bool isImpostor)
        {
            if (!Loaded) return 0;
            return (float)CalculateLightRadiusMethod.Invoke(SubmarineStatus, new object[] { null, true, isImpostor });
        }

        public static void ChangeFloor(bool toUpper)
        {
            if (!Loaded) return;
            MonoBehaviour _floorHandler = ((Component)GetFloorHandlerMethod.Invoke(null, new object[] { PlayerControl.LocalPlayer })).TryCast(FloorHandlerType) as MonoBehaviour;
            RpcRequestChangeFloorMethod.Invoke(_floorHandler, new object[] { toUpper });
        }

        public static bool getInTransition()
        {
            if (!Loaded) return false;
            return (bool)InTransitionField.GetValue(null);
        }


        public static void RepairOxygen()
        {
            if (!Loaded) return;
            try
            {
                ShipStatus.Instance.RpcRepairSystem((SystemTypes)130, 64);
                RepairDamageMethod.Invoke(SubmarineOxygenSystemInstanceField.GetValue(null), new object[] { PlayerControl.LocalPlayer, 64 });
            }
            catch (System.NullReferenceException)
            {
                
            }

        }

        public static bool isSubmerged()
        {
            return Loaded && ShipStatus.Instance && ShipStatus.Instance.Type == SUBMERGED_MAP_TYPE;
        }
    }

    public class MissingSubmergedBehaviour : MonoBehaviour
    {
        static MissingSubmergedBehaviour() => ClassInjector.RegisterTypeInIl2Cpp<MissingSubmergedBehaviour>();
        public MissingSubmergedBehaviour(IntPtr ptr) : base(ptr) { }
    }
}
