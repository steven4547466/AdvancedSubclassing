using Exiled.API.Features;
using HarmonyLib;
using Mirror;
using PlayableScps;
using PlayableScps.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Targeting;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace Subclass.Patches
{
	//[HarmonyPatch(typeof(PlayableScps.Scp096), nameof(PlayableScps.Scp096.AddTarget))]
	//static class Scp096AddTargetPatch
	//{
	//    public static bool Prefix(PlayableScps.Scp096 __instance, GameObject target)
	//    {
	//        Log.Debug("Inside 096 patch", Subclass.Instance.Config.Debug);
	//        if (!NetworkServer.active)
	//        {
	//            throw new InvalidOperationException("Called AddTarget from client.");
	//        }
	//        Player player = Player.Get(target);
	//        if (Tracking.PlayersWithSubclasses.ContainsKey(player) && Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Disable096Trigger)) return false;
	//        ReferenceHub hub = ReferenceHub.GetHub(target);
	//        if (__instance.CanReceiveTargets && ((hub != null) && !__instance._targets.Contains(hub)))
	//        {
	//            if (!__instance._targets.IsEmpty<ReferenceHub>())
	//            {
	//                __instance.AddReset();
	//            }
	//            __instance._targets.Add(hub);
	//            __instance.AdjustShield(200);
	//        }
	//        return false;
	//    }
	//}

	[HarmonyPatch(typeof(PlayableScps.Scp096), nameof(PlayableScps.Scp096.UpdateVision))]
	static class Scp096UpdateVisionPatch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var newInstructions = instructions.ToList();
			var local = generator.DeclareLocal(typeof(Player));
			var ldloc3Label = generator.DefineLabel();
			var ldlocasLabel = generator.DefineLabel();

			var offset = newInstructions.FindIndex(c => c.opcode == OpCodes.Stloc_3) + 1;
			var ldlocasIndex = newInstructions.FindLastIndex(c => c.opcode == OpCodes.Ldnull) + 6;
			newInstructions[offset].labels.Add(ldloc3Label);
			newInstructions[ldlocasIndex].labels.Add(ldlocasLabel);

			newInstructions.InsertRange(offset, new CodeInstruction[]
			{
				new CodeInstruction(OpCodes.Ldloc_3),
				new CodeInstruction(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] {typeof(ReferenceHub)})),
				new CodeInstruction(OpCodes.Stloc_S, local.LocalIndex),
				new CodeInstruction(OpCodes.Ldloc_S, local.LocalIndex),
				new CodeInstruction(OpCodes.Brfalse_S, ldloc3Label),
				new CodeInstruction(OpCodes.Ldsfld, Field(typeof(TrackingAndMethods), nameof(TrackingAndMethods.PlayersWithSubclasses))),
				new CodeInstruction(OpCodes.Ldloc_S, local.LocalIndex),
				new CodeInstruction(OpCodes.Callvirt, Method(typeof(Dictionary<Player, SubClass>), nameof(Dictionary<Player, SubClass>.ContainsKey), new[] {typeof(Player)})),
				new CodeInstruction(OpCodes.Brfalse_S, ldloc3Label),
				new CodeInstruction(OpCodes.Ldsfld, Field(typeof(TrackingAndMethods), nameof(TrackingAndMethods.PlayersWithSubclasses))),
				new CodeInstruction(OpCodes.Ldloc_S, local.LocalIndex),
				new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Dictionary<Player, SubClass>), "Item")),
				new CodeInstruction(OpCodes.Ldfld, Field(typeof(SubClass), nameof(SubClass.Abilities))),
				new CodeInstruction(OpCodes.Ldc_I4_8),
				new CodeInstruction(OpCodes.Callvirt, Method(typeof(List<AbilityType>), nameof(List<AbilityType>.Contains), new[] { typeof(AbilityType) })),
				new CodeInstruction(OpCodes.Brtrue, ldlocasLabel)
			});

			foreach (var code in newInstructions)
				yield return code;
		}
	}

	[HarmonyPatch(typeof(PlayableScps.Scp096), nameof(PlayableScps.Scp096.OnDamage))]
	static class Scp096OnDamagePatch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var newInstructions = instructions.ToList();

			var returnLabel = newInstructions.First(i => i.opcode == OpCodes.Brfalse_S).operand;
			var continueLabel = generator.DefineLabel();
			var firstOffset = newInstructions.FindIndex(i => i.opcode == OpCodes.Ldloc_0) + 1;

			var player = generator.DeclareLocal(typeof(Player));

			newInstructions.InsertRange(firstOffset, new CodeInstruction[]
			{
				new CodeInstruction(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(UnityEngine.GameObject) })),
				new CodeInstruction(OpCodes.Stloc_1, player.LocalIndex),
				new CodeInstruction(OpCodes.Ldloc_0)
			});

			var secondOffset = newInstructions.FindIndex(i => i.opcode == OpCodes.Ldnull) + 3;

			var secondInstructions = new CodeInstruction[]
			{
				new CodeInstruction(OpCodes.Ldloc_1),
				new CodeInstruction(OpCodes.Brfalse_S, continueLabel),
				new CodeInstruction(OpCodes.Ldsfld, Field(typeof(TrackingAndMethods), nameof(TrackingAndMethods.PlayersWithSubclasses))),
				new CodeInstruction(OpCodes.Ldloc_1),
				new CodeInstruction(OpCodes.Callvirt, Method(typeof(Dictionary<Player, SubClass>), nameof(Dictionary<Player, SubClass>.ContainsKey), new[] {typeof(Player)})),
				new CodeInstruction(OpCodes.Brfalse_S, continueLabel),
				new CodeInstruction(OpCodes.Ldsfld, Field(typeof(TrackingAndMethods), nameof(TrackingAndMethods.PlayersWithSubclasses))),
				new CodeInstruction(OpCodes.Ldloc_1),
				new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Dictionary<Player, SubClass>), "Item")),
				new CodeInstruction(OpCodes.Ldfld, Field(typeof(SubClass), nameof(SubClass.Abilities))),
				new CodeInstruction(OpCodes.Ldc_I4_8),
				new CodeInstruction(OpCodes.Callvirt, Method(typeof(List<AbilityType>), nameof(List<AbilityType>.Contains), new[] { typeof(AbilityType) })),
				new CodeInstruction(OpCodes.Brtrue, returnLabel)
			};

			newInstructions.InsertRange(secondOffset, secondInstructions);

			newInstructions[secondOffset + secondInstructions.Length].labels.Add(continueLabel);

			foreach (var code in newInstructions)
				yield return code;
		}
	}

	//[HarmonyPatch(typeof(PlayableScps.Scp096), nameof(PlayableScps.Scp096.UpdateVision))]
	//static class Scp096OnUpdatePatch
	//{
	//	public static bool Prefix(PlayableScps.Scp096 __instance)
	//	{
	//		if (__instance._flash.Enabled)
	//		{
	//			return false;
	//		}
	//		Vector3 vector = __instance.Hub.transform.TransformPoint(PlayableScps.Scp096._headOffset);
	//		foreach (KeyValuePair<GameObject, ReferenceHub> keyValuePair in ReferenceHub.GetAllHubs())
	//		{
	//			ReferenceHub value = keyValuePair.Value;
	//			Player p = Player.Get(value);
	//			if (p != null && TrackingAndMethods.PlayersWithSubclasses.ContainsKey(p) &&
	//				TrackingAndMethods.PlayersWithSubclasses[p].Abilities.Contains(AbilityType.Disable096Trigger))
	//				continue;
	//			CharacterClassManager characterClassManager = value.characterClassManager;
	//			if (characterClassManager.CurClass != RoleType.Spectator && !(value == __instance.Hub) && !characterClassManager.IsAnyScp()
	//				&& Vector3.Dot((value.PlayerCameraReference.position - vector).normalized, __instance.Hub.PlayerCameraReference.forward) >= 0.1f)
	//			{
	//				VisionInformation visionInformation = VisionInformation.GetVisionInformation(value, vector, -0.1f, 60f, true, true, __instance.Hub.localCurrentRoomEffects);
	//				if (visionInformation.IsLooking)
	//				{
	//					float delay = visionInformation.LookingAmount / 0.25f * (visionInformation.Distance * 0.1f);
	//					if (!__instance.Calming)
	//					{
	//						__instance.AddTarget(value.gameObject);
	//					}
	//					if (__instance.CanEnrage && value.gameObject != null)
	//					{
	//						__instance.PreWindup(delay);
	//					}
	//				}
	//			}
	//		}
	//		return false;
	//	}
	//}
}
