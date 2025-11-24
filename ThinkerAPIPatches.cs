using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ThinkerAPI;
using UnityEngine;

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
                var methods = eventt.eventscript.GetType().GetMethods(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
                RandomEventFlags flags = RandomEventFlags.None;
                if (eventt.eventscript.PotentialRoomAssets.Length > 0)
                    flags |= RandomEventFlags.RoomSpecific;
                if (methods.Exists(x => x.Name == "AfterUpdateSetup") || eventt.eventscript.PotentialRoomAssets.Length > 0)
                    flags |= RandomEventFlags.AffectsGenerator;
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

    [HarmonyPatch(typeof(MassObjectHolder), nameof(MassObjectHolder.AddAssetFolder)), HarmonyPrefix]
    static bool RedirectCoroutine(string folderpath, MassObjectHolder __instance)
    {
        var modImIn = (string)modItsIn.GetValue(__instance);
        if (modImIn == "NULLMODEXCEPTIONABCDEFGHIJKLMNOP") return false;
        IEnumerator AddAssetolder(string folderpath)
        {
            var loaders = (int)_loaders.GetValue(__instance);
            _loaders.SetValue(__instance, loaders++);

            string[] asset = Directory.GetFiles(Path.Combine(thinkerAPI.moddedpath, modImIn, folderpath));
            string[] array = asset;
            foreach (string s in array)
            {
                var actualPath = Path.Combine(modImIn, folderpath, s);
                string extension = Path.GetExtension(actualPath); // Do better.
                if (extension.ToLower() == ".png")
                    yield return thinkerAPI.Instance.StartCoroutine(__instance.AddASprite(actualPath));
                else if (extension.ToLower() == ".ogg" || extension.ToLower() == ".mp3" || extension.ToLower() == ".wav")
                    yield return thinkerAPI.Instance.StartCoroutine(__instance.AddAClip(actualPath));
            }

            loaders = (int)_loaders.GetValue(__instance);
            _loaders.SetValue(__instance, loaders--);
        }
        thinkerAPI.Instance.StartCoroutine(AddAssetolder(folderpath));
        return false;
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
        if (ConnectorBasicsPlugin.CaptionsLoaded) return false; // BUT WHY INVOKE THIS AFTER PRELOAD??
        foreach (string captionpath in ___captionpaths)
        {
            string path = Path.Combine(captionpath, "Captions");
            string[] files = Directory.GetFiles(path);
            
            if (files.Length == 0)
                ConnectorBasicsPlugin.Log.LogInfo($"Localization path {Path.GetDirectoryName(path)} has no captions!");
            else
            {
                foreach (string lpath in files)
                    AssetLoader.LocalizationFromFile(lpath, Language.English);
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

    private static FieldInfo modItsIn = AccessTools.DeclaredField(typeof(MassObjectHolder), "modImIn"),
        _loaders = AccessTools.DeclaredField(typeof(MassObjectHolder), "loaders");
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
        ItemFlags flags = ItemFlags.None;
        var fields = bit.itemType.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
        var methods = bit.itemType.GetMethods(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
        if (bit.itemType == typeof(Item) || !methods.Exists(x => x.Name == "Use"))
            flags |= ItemFlags.NoUses;
        if (fields.Exists(x => x.FieldType.Equals(typeof(Entity))))
            flags |= ItemFlags.CreatesEntity;
        if (bit.pickup != null)
            builder.SetPickupSound(bit.pickup);
        if (bit.instantuse)
            builder.SetAsInstantUse();
        type.GetMethod("SetItemComponent", []).MakeGenericMethod(bit.itemType).Invoke(builder, []);
        // ThinkerAPI is missing `overrideDisabled` which Eco Friendly stupidly patches something so that it can be used in Pitstop FOR NO REASON.
        __result = builder.Build();
        return false;
    }
}