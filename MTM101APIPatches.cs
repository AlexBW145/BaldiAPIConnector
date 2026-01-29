using HarmonyLib;
using MTM101BaldAPI.Registers;
using System.Collections.Generic;
using System.Linq;
using ThinkerAPI;

namespace APIConnector;

/*[HarmonyPatch(typeof(EnumExtensions))]
class EmumGetaroo
{
    [HarmonyPatch(nameof(EnumExtensions.GetExtendedName)), HarmonyTargetMethod]
    static MethodBase TargetMethod(Harmony instance) => typeof(EnumExtensions).GetMethod(nameof(EnumExtensions.GetExtendedName), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod([typeof(Enum)]);
    [HarmonyPostfix]
    static void GetEnumFromThinker(int val, ref string __result, ref Enum __1)
    {
        if (__result != val.ToString()) return;
        __result = AccessTools.Method(typeof(ENanmEXTENDED), nameof(ENanmEXTENDED.GetAnEnumThatDoesntExist), [typeof(string)], [typeof(Enum)]).Invoke(null, [val, __1]);
    }
}*/

[HarmonyPatch]
class MTM101APIPatches
{
    [HarmonyPatch(typeof(LoadingEvents), "SortLoadingEvents"), HarmonyPostfix]
    static void SortAgainIGuess(ref List<LoadingEvents.LoadingEvent> ___LoadingEventsPre) // Workaround due to IEnumerable containers and also they cannot be overwritten from outsiders...
    {
        ___LoadingEventsPre = [___LoadingEventsPre.Find(x => x.info == thinkerAPI.Instance.Info), .. ___LoadingEventsPre.Where(x => x.info != thinkerAPI.Instance.Info && x.info.Metadata.GUID != "alexbw145.bbplus.apiconnector"), ___LoadingEventsPre.Find(x => x.info.Metadata.GUID == "alexbw145.bbplus.apiconnector")];
    }
}
