using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using Exiled.API.Features;


namespace Subclass.Patches
{
    static class Util
    {
        public static List<CodeInstruction> AddInstructions(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var newInstructions = instructions.ToList();
            var local = generator.DeclareLocal(typeof(Player));
            var label = generator.DefineLabel();
            newInstructions[0].labels.Add(label);
            newInstructions.InsertRange(0, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(PlayerStats), nameof(PlayerStats.gameObject))),
                new CodeInstruction(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(UnityEngine.GameObject) })),
                new CodeInstruction(OpCodes.Stloc_0, local.LocalIndex),
                new CodeInstruction(OpCodes.Ldsfld, Field(typeof(TrackingAndMethods), nameof(TrackingAndMethods.PlayersThatJustGotAClass))),
                new CodeInstruction(OpCodes.Ldloc_0, local.LocalIndex),
                new CodeInstruction(OpCodes.Callvirt, Method(typeof(Dictionary<Player, float>), nameof(Dictionary<Player, float>.ContainsKey))),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldsfld, Field(typeof(TrackingAndMethods), nameof(TrackingAndMethods.PlayersThatJustGotAClass))),
                new CodeInstruction(OpCodes.Ldloc_0, local.LocalIndex),
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Dictionary<Player, float>), "Item")),
                new CodeInstruction(OpCodes.Ldc_R4, 7f),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(UnityEngine.Time), nameof(UnityEngine.Time.time))),
                new CodeInstruction(OpCodes.Ble_Un_S, label),
                new CodeInstruction(OpCodes.Call, Method(typeof(TrackingAndMethods), nameof(TrackingAndMethods.RoundJustStarted))),
                new CodeInstruction(OpCodes.Brfalse, label),
                new CodeInstruction(OpCodes.Ret)
            });

            return newInstructions;
        }
    }

    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.Health), MethodType.Setter)]
    static class PlayerStatsHealthSetterPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var newInstructions = Util.AddInstructions(instructions, generator);

            foreach (var code in newInstructions)
                yield return code;
        }
    }

    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.maxHP), MethodType.Setter)]
    static class PlayerStatsMaxHealthSetterPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var newInstructions = Util.AddInstructions(instructions, generator);

            foreach (var code in newInstructions)
                yield return code;
        }
    }
}
