using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ThinkerAPI;
using UnityEngine;

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
    /*[HarmonyPatch(typeof(EnumExtensions), nameof(EnumExtensions.ExtendEnum)), HarmonyPostfix]
    static void AddToThinkerEnumList(string extendName, ref Enum __result)
    {
        if (!ENanmEXTENDED.counts[__result.GetType()].names.Contains(extendName))
            ENanmEXTENDED.counts[__result.GetType()].names.Add(extendName);
    }

    [HarmonyPatch(typeof(NPCBuilder<NPC>), nameof(NPCBuilder<NPC>.Build)), HarmonyPostfix]
    static void AddToThinkerNPCStorage(ref NPC __result, ref BepInEx.PluginInfo ___info, ref bool ___hasLooker,
        ref List<WeightedRoomAsset> ___potentialRoomAssets, List<RoomCategory> ___spawnableRooms, ref bool ___hasTrigger, bool ___autoRotate,
        ref bool ___preciseTarget, ref bool ___decelerate, ref bool ___ignorePlayerOnSpawn, ref bool ___grounded, ref string ___characterEnumName,
        ref float ___fieldOfView, ref float ___maxSightDistance, ref float ___minAudioDistance, ref float ___maxAudioDistance, ref Texture2D ___posterTexture,
        ref NPCFlags ___flags, ref bool ___useHeatmap) => 
        thinkerAPI.modNPCs.Add(new BasicNPCTemplate(ConnectorBasicsPlugin.holder, ___hasLooker, ___potentialRoomAssets.ToArray(), ___spawnableRooms.ToArray(),
            ___hasTrigger, ___autoRotate, ___preciseTarget, ___decelerate, false, ___ignorePlayerOnSpawn, !___grounded, ___characterEnumName,
            ___fieldOfView >= 0f ? ___fieldOfView : 180f, ___maxSightDistance, __result.name, ___minAudioDistance, ___maxAudioDistance, "", "",
            ___posterTexture, !___flags.HasFlag(NPCFlags.CanMove), [], [], __result.GetType(), ___useHeatmap, false));

    [HarmonyPatch(typeof(ItemBuilder), nameof(ItemBuilder.Build)), HarmonyPostfix]
    static void AddToThinkerItemStorage(ref ItemObject __result, ref string ___itemEnumName) => 
        thinkerAPI.modItems.Add(new BasicItemTemplate(ConnectorBasicsPlugin.holder, __result.itemSpriteSmall, __result.itemSpriteLarge, __result.price, [], [], __result.value, __result.audPickupOverride,
            __result.nameKey, __result.descKey, ___itemEnumName, !__result.addToInventory, __result.item.GetType(),
            __result.price != 0, 0));*/
}
