using brobowindowsmod;
using HarmonyLib;
using MTM101BaldAPI;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace APIConnector;

[ConditionalPatchMod("OurWindowsFragiled"), HarmonyPatch]
internal class FragilePatches
{
    private static IEnumerable<CodeInstruction> Why(IEnumerable<CodeInstruction> instructions)  // Thinker, why the fuck did you add in an enum extension system in Fragile Windows??
    {
        // I am using this now...
        var instructionMatch = new CodeMatcher(instructions);
        instructionMatch.Start().MatchForward(true, new CodeMatch(x => x.opcode == OpCodes.Ldtoken));
        var generic = instructionMatch.Instruction.operand;
        var enumType = AccessTools.TypeByName(generic.ToString());
        //
        return new List<CodeInstruction>()
        {
            new CodeInstruction(OpCodes.Nop),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThinkerAPI.ENanmEXTENDED), nameof(ThinkerAPI.ENanmEXTENDED.GetAnEnumThatDoesntExist), [typeof(string)], [enumType])),
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

    internal static void PatchFragile(Harmony harmony, Assembly[] assemblies)
    {
        foreach (var _enum in AccessTools.AllTypes().Where(x => x.IsEnum && assemblies.Contains(x.Assembly) && x.IsPublic))
            harmony.Patch(AccessTools.Method(typeof(brobowindowsmod.ENanmEXTENDED), nameof(brobowindowsmod.ENanmEXTENDED.GetAnEnumThatDoesntExist), [typeof(string)], [_enum]), transpiler: new HarmonyMethod(AccessTools.Method(typeof(FragilePatches), "Why")));
    }
}
