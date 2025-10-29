using brobowindowsmod;
using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace APIConnector;

[ConditionalPatchMod("OurWindowsFragiled"), HarmonyPatch]
internal class FragilePatches
{
    private static IEnumerable<CodeInstruction> Why(IEnumerable<CodeInstruction> instructions)  // Thinker, why the fuck did you add in an enum extension system in Fragile Windows??
    {
        // Useless but I want to keep it.
        var instructionMatch = new CodeMatcher(instructions);
        instructionMatch.Start().MatchForward(true, new CodeMatch(x => x.opcode == OpCodes.Ldtoken));
        var generic = instructionMatch.Instruction.operand;
        //
        return new List<CodeInstruction>()
        {
            new CodeInstruction(OpCodes.Nop),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ThinkerAPI.ENanmEXTENDED), nameof(ThinkerAPI.ENanmEXTENDED.GetAnEnumThatDoesntExist), [typeof(string)], [typeof(Enum)])),
            new CodeInstruction(OpCodes.Ret)
        };
    }

    [HarmonyPatch(typeof(FragileWindowBase), nameof(FragileWindowBase.MakeWindowWorld)), HarmonyPostfix]
    private static void WindowWorldEnumSetToUnique(ref LevelObject __result)
    {
        if (!EnumExtensions.EnumWithExtendedNameExists<LevelType>("WindowWorld"))
            EnumExtensions.ExtendEnum<LevelType>("WindowWorld");
        __result.type = EnumExtensions.GetFromExtendedName<LevelType>("WindowWorld");
    }

    internal static void PatchFragile(Harmony harmony) => harmony.Patch(typeof(brobowindowsmod.ENanmEXTENDED).GetMethod(nameof(brobowindowsmod.ENanmEXTENDED.GetAnEnumThatDoesntExist)).MakeGenericMethod(typeof(Enum)), transpiler: new HarmonyMethod(AccessTools.Method(typeof(FragilePatches), "Why")));  
}
