using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using ThinkerAPI;
using System;
using System.Collections.Generic;
using MTM101BaldAPI.Reflection;
using UnityEngine;
using System.IO;
using MTM101BaldAPI.AssetTools;

namespace APIConnector;

[HarmonyPatch]
class ThinkerAPIPatches
{
    [HarmonyPatch(typeof(WindowPeeBugManager), nameof(WindowPeeBugManager.InitializeWPD)), HarmonyPrefix]
    static bool DoExtraStuffPlusPinedebugSupportInit() // Considering that his code is that messy...
    {
        if (!ConnectorBasicsPlugin.Connected)
        {
            foreach (var eventt in thinkerAPI.modEvents) // There isn't anything else to do with events...
            {
                RandomEventFlags flags = RandomEventFlags.None;
                if (eventt.eventscript.PotentialRoomAssets.Length > 0)
                    flags |= RandomEventFlags.RoomSpecific | RandomEventFlags.AffectsGenerator;
                if (eventt.floorNames.Count <= 0)
                    flags |= RandomEventFlags.Special;
                eventt.eventscript.ReflectionSetVariable("eventType", EnumExtensions.ExtendEnum<RandomEventType>(eventt.eventname.Replace(" ", "")));
                RandomEventMetaStorage.Instance.Add(new RandomEventMetadata(Chainloader.PluginInfos[(string)AccessTools.DeclaredField(typeof(MassObjectHolder), "modImIn").GetValue(eventt.moh)], eventt.eventscript, flags));
            }
            return false;
        }
        return true;
        /*(if (Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.pinedebug"))
            PineDebugSupport.Initalize();*/
    }

    /*[HarmonyPatch(typeof(ENanmEXTENDED), "ConnectorHelper")]
    [HarmonyPostfix]
    private static void AdvancedFixer(Type enumType, string extendedName)
    {
        if (enumType.GetType() == typeof(RandomEventType))
            EnumExtensions.ExtendEnum<RandomEventType>(extendedName);
        else if (enumType.GetType() == typeof(Items))
            EnumExtensions.ExtendEnum<Items>(extendedName);
        else if (enumType.GetType() == typeof(Character))
            EnumExtensions.ExtendEnum<Character>(extendedName);
        else if (enumType.GetType() == typeof(LevelType))
            EnumExtensions.ExtendEnum<LevelType>(extendedName);
    }*/

    [HarmonyPatch(typeof(thinkerAPI), "LoadSavedCaptions"), HarmonyPrefix]
    static bool LoadDaCaptions(ref List<string> ___captionpaths)
    {
        foreach (string captionpath in ___captionpaths)
        {
            string path = Path.Combine(captionpath, "Captions");
            string[] files = Directory.GetFiles(path);
            
            if (files.Length == 0)
            {
                Debug.LogError("you got no captions");
            }
            else
            {
                AssetLoader.LocalizationFromFunction((lang) =>
                {
                    var dictionary = new Dictionary<string, string>();
                    string[] array = files;
                    foreach (string path2 in array)
                    {
                        LocalizationData localizationData = null;
                        localizationData = JsonUtility.FromJson<LocalizationData>(File.ReadAllText(path2));
                        for (int j = 0; j < localizationData.items.Length; j++)
                        {
                            if (!dictionary.ContainsKey(localizationData.items[j].key))
                            {
                                dictionary.Add(localizationData.items[j].key, localizationData.items[j].value);
                            }
                            else
                            {
                                dictionary[localizationData.items[j].key] = localizationData.items[j].value;
                            }
                        }
                    }
                    return dictionary;
                });
            }
        }
        return false;
    }


    [HarmonyPatch(typeof(thinkerAPI), "WaitForWarnings", MethodType.Enumerator), HarmonyTranspiler]
    static IEnumerable<CodeInstruction> ConnectorWaitUntil(IEnumerable<CodeInstruction> instructions) => new CodeMatcher(instructions)
        .End()
        .Insert(Transpilers.EmitDelegate<Action>(() => ConnectorBasicsPlugin.Connected = true))
        /*.Start()
        .MatchForward(false,
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), nameof(UnityEngine.Object.FindObjectOfType), [], [typeof(MainMenu)])),
        new CodeMatch(OpCodes.Ldnull),
        new CodeMatch(x => x.opcode == OpCodes.Call && ((MethodBase)x.operand).Name == "op_Inequality"),
        new CodeMatch(ci => ci.IsStloc())
        )
        .ThrowIfInvalid("UHHH")
        .Set(OpCodes.Call, AccessTools.Method(typeof(ThinkerAPIPatches), nameof(UhhhUh)))*/
        .InstructionEnumeration();
#if false
    static MainMenu UhhhUh()
    {
        if (!ConnectorBasicsPlugin.Doings)
            return null;
        return UnityEngine.Object.FindObjectOfType<MainMenu>(true);
    }
#endif

    [HarmonyPatch(typeof(thinkerAPI), nameof(thinkerAPI.CreateNPC)), HarmonyPostfix]
    static void AddToMetaDataNPC(BasicNPCTemplate bit, ref NPC __result)
    {
        __result.enabled = true; // It was the radar's fault! I blame two people for this incompat!
        if (NPCMetaStorage.Instance.Find(npc => npc.character.ToStringExtended() == bit.enumname) != null)
        {
            AccessTools.Field(typeof(NPC), "character").SetValue(__result, EnumExtensions.GetFromExtendedName<Character>(bit.enumname));
            NPCMetaStorage.Instance.Get(__result.Character).prefabs.Add(bit.name, __result);
            return;
        }
        NPCFlags flags = NPCFlags.CanMove | NPCFlags.HasSprite;
        if (bit.stationary)
            flags &= ~NPCFlags.CanMove;
        if (bit.trigger)
            flags |= NPCFlags.HasTrigger;
        if (bit.looker)
            flags |= NPCFlags.CanSee;
        AccessTools.Field(typeof(NPC), "character").SetValue(__result, EnumExtensions.ExtendEnum<Character>(bit.enumname)); // Temporary because the method is generic and is hard to patch
        NPCMetaStorage.Instance.Add(new NPCMetadata(Chainloader.PluginInfos[(string)AccessTools.DeclaredField(typeof(MassObjectHolder), "modImIn").GetValue(bit.moh)], [__result], bit.name, flags));
    }

    [HarmonyPatch(typeof(thinkerAPI), nameof(thinkerAPI.CreateItem)), HarmonyPostfix]
    static void AddToMetaDataItem(BasicItemTemplate bit, ref ItemObject __result)
    {
        if (ItemMetaStorage.Instance.Find(item => item.id.ToStringExtended() == bit.enumName) != null)
        {
            __result.itemType = EnumExtensions.GetFromExtendedName<Items>(bit.enumName);
            var array = ItemMetaStorage.Instance.FindByEnum(__result.itemType).itemObjects;
            ItemMetaStorage.Instance.FindByEnum(__result.itemType).itemObjects = [__result, .. array];
            return;
        }
        ItemFlags flags = ItemFlags.None;
        if (bit.instantuse)
            flags |= ItemFlags.InstantUse;
        /*if (bit.itemType.Equals(typeof(Item)))
            flags |= ItemFlags.NoUses;*/
        // Is there a way for persistant & multiple use items??
        __result.itemType = EnumExtensions.ExtendEnum<Items>(bit.enumName);
        var meta = new ItemMetaData(Chainloader.PluginInfos[(string)AccessTools.DeclaredField(typeof(MassObjectHolder), "modImIn").GetValue(bit.moh)], __result);
        meta.flags = flags;
        ItemMetaStorage.Instance.Add(meta);
    }
}
