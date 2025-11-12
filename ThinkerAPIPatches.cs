using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using ThinkerAPI;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using MTM101BaldAPI.AssetTools;
using System.Reflection.Emit;
using System.Reflection;
using MTM101BaldAPI.ObjectCreation;
using BepInEx;

namespace APIConnector;

[HarmonyPatch]
internal class ThinkerAPIPatches
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
                //eventt.eventscript.ReflectionSetVariable("eventType", EnumExtensions.ExtendEnum<RandomEventType>(eventt.eventname.Replace(" ", "")));
                RandomEventMetaStorage.Instance.Add(new RandomEventMetadata(Chainloader.PluginInfos[(string)modItsIn.GetValue(eventt.moh)], eventt.eventscript, flags));
            }
            return false;
        }
        return true;
        /*(if (Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.pinedebug"))
            PineDebugSupport.Initalize();*/
    }

    [HarmonyPatch]
    static IEnumerable<CodeInstruction> EnumFromMissedTheTexture(IEnumerable<CodeInstruction> instructions)
    {
        var instructionMatch = new CodeMatcher(instructions);
        instructionMatch.Start().MatchForward(true, new CodeMatch(x => x.opcode == OpCodes.Ldtoken));
        var generic = instructionMatch.Instruction.operand;
        var enumType = AccessTools.TypeByName(generic.ToString());
        return new List<CodeInstruction>()
        {
            new CodeInstruction(OpCodes.Nop),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EnumExtensions), nameof(EnumExtensions.ExtendEnum), [typeof(string)], [enumType])),
            new CodeInstruction(OpCodes.Ret)
        };
    }

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

    private static FieldInfo modItsIn = AccessTools.DeclaredField(typeof(MassObjectHolder), "modImIn");
    private static FieldInfo _Character = AccessTools.Field(typeof(NPC), "character");
#if false
    [HarmonyPatch(typeof(thinkerAPI), nameof(thinkerAPI.CreateNPC)), HarmonyPostfix]
    static void AddToMetaDataNPC(BasicNPCTemplate bit, ref NPC __result)
    {
        __result.enabled = true; // It was the radar's fault! I blame two people for this incompat!
        if (NPCMetaStorage.Instance.Find(npc => npc.character.ToStringExtended() == bit.enumname) != null)
        {
            _Character.SetValue(__result, EnumExtensions.GetFromExtendedName<Character>(bit.enumname));
            NPCMetaStorage.Instance.Get(__result.Character).prefabs.Add(bit.name, __result);
            return;
        }
        NPCFlags flags = NPCFlags.CanMove | NPCFlags.HasSprite;
        if (bit.stationary)
            flags &= ~NPCFlags.CanMove;
        if (bit.trigger)
            flags |= NPCFlags.HasTrigger;
        if (bit.looker && bit.sightdistance > 0)
            flags |= NPCFlags.CanSee;
        NPCMetaStorage.Instance.Add(new NPCMetadata(Chainloader.PluginInfos[(string)modItsIn.GetValue(bit.moh)], [__result], bit.name, flags));
    }
#else
    [HarmonyPatch(typeof(thinkerAPI), nameof(thinkerAPI.CreateNPC)), HarmonyPrefix]
    static bool AddToMetaDataNPC(ref BasicNPCTemplate bit, ref NPC __result)
    {
        var type = typeof(NPCBuilder<>).MakeGenericType(bit.npcType);
        var builder = type.GetConstructor([typeof(PluginInfo)]).Invoke([Chainloader.PluginInfos[(string)modItsIn.GetValue(bit.moh)]]);
        type.GetMethod("SetName", [typeof(string)]).Invoke(builder, [bit.name]);
        type.GetMethod("SetEnum", [typeof(string)]).Invoke(builder, [bit.enumname]);
        type.GetMethod("SetPoster", [typeof(Texture2D), typeof(string), typeof(string)]).Invoke(builder, [bit.poster, bit.nameKey, bit.descKey]);
        type.GetMethod("AddSpawnableRoomCategories", [typeof(RoomCategory[])]).Invoke(builder, [bit.spawnroom]);
        type.GetMethod("SetMinMaxAudioDistance", [typeof(float), typeof(float)]).Invoke(builder, [bit.minaudiodistance, bit.maxaudiodistance]);
        type.GetMethod("SetMaxSightDistance", [typeof(float)]).Invoke(builder, [bit.sightdistance]);
        type.GetMethod("AddPotentialRoomAssets", [typeof(WeightedRoomAsset[])]).Invoke(builder, [bit.requireroom]);
        type.GetMethod("SetFOV", [typeof(float)]).Invoke(builder, [bit.fov <= 0f ? -1f : bit.fov]); // The way that ThinkerAPI sets the fov is "above 0f" and not "above or equals to 0f"
        if (bit.stationary) // The part where if statements has no else statements.
            type.GetMethod("SetStationary").Invoke(builder, []);
        if (bit.looker)
            type.GetMethod("AddLooker").Invoke(builder, []);
        if (bit.useheatmap)
            type.GetMethod("AddHeatmap").Invoke(builder, []);
        if (bit.accelerates)
            type.GetMethod("EnableAcceleration").Invoke(builder, []);
        if (bit.airborne)
            type.GetMethod("SetAirborne").Invoke(builder, []);
        if (bit.trigger)
            type.GetMethod("AddTrigger").Invoke(builder, []);
        if (!bit.autorotate)
            type.GetMethod("DisableAutoRotation").Invoke(builder, []);
        if (bit.dontwaittospawn)
            type.GetMethod("IgnorePlayerOnSpawn").Invoke(builder, []);
        if (!bit.precisenavigation)
            type.GetMethod("DisableNavigationPrecision").Invoke(builder, []);
        __result = (NPC)type.GetMethod("Build").Invoke(builder, []);
        return false;
    }
#endif

#if false
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
        var meta = new ItemMetaData(Chainloader.PluginInfos[(string)modItsIn.GetValue(bit.moh)], __result);
        meta.flags = flags;
        ItemMetaStorage.Instance.Add(meta);
    }
#else
    [HarmonyPatch(typeof(thinkerAPI), nameof(thinkerAPI.CreateItem)), HarmonyPrefix]
    static bool AddToMetaDataItem(ref BasicItemTemplate bit, ref ItemObject __result)
    {
        var type = typeof(ItemBuilder);
        var builder = new ItemBuilder(Chainloader.PluginInfos[(string)modItsIn.GetValue(bit.moh)])
            .SetNameAndDescription(bit.nameKey, bit.descKey)
            .SetEnum(bit.enumName)
            .SetSprites(bit.smallSprite, bit.largeSprite)
            .SetShopPrice(bit.shopPrice)
            .SetGeneratorCost(bit.genCost);
        if (bit.pickup != null)
            builder.SetPickupSound(bit.pickup);
        if (bit.instantuse)
            builder.SetAsInstantUse();
        type.GetMethod("SetItemComponent", []).MakeGenericMethod(bit.itemType).Invoke(builder, []);
        // ThinkerAPI is missing `overrideDisabled` which Eco Friendly stupidly patches something so that it can be used in Pitstop FOR NO REASON.
        __result = builder.Build();
        return false;
    }
#endif
}