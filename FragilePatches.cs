/*using HarmonyLib;
using System.Collections.Generic;
using MTM101BaldAPI;
using System.Reflection.Emit;
using System;
using brobowindowsmod;
using ThinkerAPI;

namespace APIConnector;

[ConditionalPatchMod("OurWindowsFragiled"), HarmonyPatch]
class FragilePatches
{
    [HarmonyPatch(typeof(brobowindowsmod.ENanmEXTENDED), nameof(brobowindowsmod.ENanmEXTENDED.GetAnEnumThatDoesntExist)), HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Why(IEnumerable<CodeInstruction> instructions) 
    {
        CodeInstruction generic = null;
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldtoken)
            {
                generic = instruction;
                break;
            }
        }
        yield return new CodeInstruction(OpCodes.Nop);
        yield return generic;
        yield return new CodeInstruction(OpCodes.Ldarg_0);
        yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ThinkerAPI.ENanmEXTENDED), nameof(ThinkerAPI.ENanmEXTENDED.GetAnEnumThatDoesntExist), [typeof(string)]));
        yield return new CodeInstruction(OpCodes.Ret);
        yield break;
    }
}*/
