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
    [HarmonyPatch(typeof(MassObjectHolder), nameof(MassObjectHolder.AddAssetFolder)), HarmonyPrefix]
    static bool RedirectCoroutine(string folderpath, MassObjectHolder __instance)
    {
        if (__instance.modImIn == "NULLMODEXCEPTIONABCDEFGHIJKLMNOP")
        {
            ConnectorBasicsPlugin.Log.LogWarning("Couldn't add assets because the guid is not assigned!");
            return false;
        }
        IEnumerator AddAssetolder(string folderpath)
        {
            __instance.loaders++;
            // And yet, no return or throw statement for the error debug log...

            string[] asset = Directory.GetFiles(Path.Combine(thinkerAPI.moddedpath, __instance.modImIn, folderpath));
            foreach (string s in asset)
            {
                var actualPath = Path.Combine(__instance.modImIn, folderpath, s);
                string extension = Path.GetExtension(actualPath);
                if (extension.ToLower() == ".png")
                    yield return thinkerAPI.Instance.StartCoroutine(__instance.AddASprite(actualPath));
                else if (extension.ToLower() == ".ogg" || extension.ToLower() == ".mp3" || extension.ToLower() == ".wav")
                    yield return thinkerAPI.Instance.StartCoroutine(__instance.AddAClip(actualPath));
            }

            __instance.loaders--;
        }
        thinkerAPI.Instance.StartCoroutine(AddAssetolder(folderpath));
        return false;
    }
    [HarmonyPatch(typeof(MassObjectHolder), nameof(MassObjectHolder.LoadSoundObjectsFromFolder)), HarmonyPrefix]
    static bool LoadSoundObjectsActually(string folder, BasicSoundobjectTemplate sos, bool subtitle, MassObjectHolder __instance)
    {
        if (__instance.modImIn == "NULLMODEXCEPTIONABCDEFGHIJKLMNOP")
        {
            ConnectorBasicsPlugin.Log.LogWarning("Couldn't add assets because the guid is not assigned!");
            return false;
        }
        __instance.loaders++;
        // Ok why is it right here before??

        string[] files = Directory.GetFiles(Path.Combine(thinkerAPI.moddedpath, __instance.modImIn, folder));
        foreach (string text in files)
        {
            var actualPath = Path.Combine(__instance.modImIn, folder, text);
            string extension = Path.GetExtension(actualPath);
            if (extension.ToLower() == ".ogg" || extension.ToLower() == ".mp3" || extension.ToLower() == ".wav")
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
                // This is the worst way of sound object creation, where are the subtitle keys?
                __instance.AddAsset(ObjectCreators.CreateSoundObject((AudioClip)__instance.GetAsset(fileNameWithoutExtension, typeof(AudioClip)), fileNameWithoutExtension, sos.typ, sos.col, subtitle ? -1f : 0f), fileNameWithoutExtension + "SO");
            }
        }

        __instance.loaders--;
        return false;
    }
    [HarmonyPatch(typeof(thinkerAPI), nameof(thinkerAPI.LetWarningScreenContinue)), HarmonyPrefix]
    static bool LetConnectorContinue()
    {
        ConnectorBasicsPlugin.prevStoppers--;
        return false;
    }
    [HarmonyPatch(typeof(thinkerAPI), nameof(thinkerAPI.StopWarningScreenFromContinuing)), HarmonyPrefix]
    static bool StopConnectorFromInstantlyLoading()
    {
        ConnectorBasicsPlugin.prevStoppers++;
        return false;
    }
    [HarmonyPatch(typeof(MassObjectHolder), nameof(MassObjectHolder.isLoadingAssets)), HarmonyPrefix]
    static bool DelayingPart(ref bool __result) // Breaking, but really soon?
    {
        if (!ConnectorBasicsPlugin.Doings)
        {
            __result = true;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(thinkerAPI), nameof(thinkerAPI.WaitForWarnings), MethodType.Enumerator), HarmonyPostfix] // Gets called twice.
    static void GrabForStopping(IEnumerator __instance)
    {
        thinkerAPI.Instance.StopCoroutine(__instance);
    }

    [HarmonyPatch(typeof(WindowPeeBugManager), nameof(WindowPeeBugManager.InitializeWPD)), HarmonyPrefix]
    static bool DoExtraStuffPlusPinedebugSupportInit() => ConnectorBasicsPlugin.Connected; // Considering that his code is that messy...


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

    [HarmonyPatch(typeof(thinkerAPI), nameof(thinkerAPI.LoadSavedCaptions)), HarmonyPrefix]
    static bool LoadDaCaptions()
    {
        foreach (string captionpath in thinkerAPI.captionpaths)
        {
            string path = Path.Combine(captionpath, "Captions");
            AssetLoader.LoadLocalizationFolder(path, Language.English);
        }
        thinkerAPI.captionpaths.Clear();
        return false;
    }

    [HarmonyPatch(typeof(thinkerAPI), nameof(thinkerAPI.MakePrefab)), HarmonyPrefix]
    static bool CreatingPrefabsTheEasyWay(GameObject abab, bool active)
    {
        abab.ConvertToPrefab(active);
        return false;
    }

    private static FieldInfo modItsIn = AccessTools.DeclaredField(typeof(MassObjectHolder), "modImIn"),
        _loaders = AccessTools.DeclaredField(typeof(MassObjectHolder), "loaders");
    [HarmonyPatch(typeof(thinkerAPI), nameof(thinkerAPI.CreateEventObject)), HarmonyPrefix]
    static bool CreateRandomEventIntoMeta(string nm, Type t, SoundObject voiceline, SoundObject jingleOverride, ref GameObject __result)
    {
        var dummyPrefab = new GameObject("DummyRandomEvent_" + nm);
        dummyPrefab.ConvertToPrefab(true);
        var dummyEvent = dummyPrefab.AddComponent(t) as RandomEvent;
        ConnectorBasicsPlugin.randomEventsToQueue.Add(dummyEvent, new(dummyEvent, nm, t, voiceline, jingleOverride));
        __result = dummyPrefab;
        return false;
    }

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

    [HarmonyPatch(typeof(BasicNPCTemplate), MethodType.Constructor, [typeof(MassObjectHolder), typeof(bool), typeof(WeightedRoomAsset[]), typeof(RoomCategory[]), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(string), typeof(float), typeof(float), typeof(string), typeof(float), typeof(float), typeof(string), typeof(string), typeof(Texture2D), typeof(bool), typeof(List<string>), typeof(List<int>), typeof(Type), typeof(bool), typeof(bool)]), HarmonyPostfix]
    static void ConstructNPC(ref BasicNPCTemplate __instance)
    {
        NPC npc = thinkerAPI.CreateNPC(__instance);
        npc.gameObject.name = __instance.name;
        __instance.moh.AddAsset(npc, $"{__instance.enumname}NPCObject");
        WindowPeeBugManager.npcs.Add(npc);
    }

    [HarmonyPatch(typeof(BasicItemTemplate), MethodType.Constructor, [typeof(MassObjectHolder), typeof(Sprite), typeof(Sprite), typeof(int), typeof(List<string>), typeof(List<int>), typeof(int), typeof(SoundObject), typeof(string), typeof(string), typeof(string), typeof(bool), typeof(Type), typeof(bool), typeof(int)]), HarmonyPostfix]
    static void ConstructItem(ref BasicItemTemplate __instance)
    {
        ItemObject itemobject = thinkerAPI.CreateItem(__instance);
        itemobject.name = __instance.enumName;
        __instance.moh.AddAsset(itemobject, $"{__instance.enumName}ItemObject");
        WindowPeeBugManager.items.Add(itemobject);
    }
}