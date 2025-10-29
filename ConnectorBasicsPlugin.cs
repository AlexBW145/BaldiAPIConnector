﻿using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using ThinkerAPI;
using System.Collections;
using UnityEngine;
using BepInEx.Bootstrap;
using UnityEngine.SceneManagement;
using MTM101BaldAPI.Reflection;
using System.Linq;
using System.Reflection;
using System;
using System.Collections.Generic;

namespace APIConnector;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "8.1.0.0")]
[BepInDependency("thinkerAPI", "1.0.0.0")]
[BepInDependency("OurWindowsFragiled", BepInDependency.DependencyFlags.SoftDependency)]
[BepInProcess("BALDI.exe")]
public class ConnectorBasicsPlugin : BaseUnityPlugin
{
    private const string PLUGIN_GUID = "alexbw145.bbplus.apiconnector";
    private const string PLUGIN_NAME = "ThinkerAPI + MTM101API Connector";
    private const string PLUGIN_VERSION = "0.1.1.0";

    internal static bool Connected = false;
    internal static bool Doings = false;
    private void Awake()
    {
        Harmony harmony = new Harmony(PLUGIN_GUID);
        harmony.PatchAllConditionals();
        if (Chainloader.PluginInfos.ContainsKey("OurWindowsFragiled")) // The generics are hardmode...
            harmony.Patch(typeof(brobowindowsmod.ENanmEXTENDED).GetMethod(nameof(brobowindowsmod.ENanmEXTENDED.GetAnEnumThatDoesntExist)).MakeGenericMethod(typeof(Enum)), transpiler: new HarmonyMethod(AccessTools.Method(typeof(FragilePatches), "Why")));

        IEnumerator Postdoings()
        {
            yield return 2;
            yield return "Connecting APIs...";
            var nameentry = GameObject.Find("NameEntry");
            nameentry.name = "IAMDOINGSTUF";
            Doings = true;
            var menu = new GameObject("MenuThatKills", typeof(MainMenu));
            menu.SetActive(true);
            yield return new WaitUntil(() => Connected);
            DestroyImmediate(menu);
            nameentry.name = "NameEntry";
            yield return "Adding save handlers and scene generator enqueues...";
            List<LevelType> extendedEnums = new List<LevelType>();
            if (ENanmEXTENDED.counts.Keys.Contains(typeof(LevelType)))
            { // Patches to generic methods with generic returns are hard...

                foreach (var name in ENanmEXTENDED.counts[typeof(LevelType)].names)
                    extendedEnums.Add(EnumExtensions.ExtendEnum<LevelType>(name));
            }
            foreach (var thinkPlugins in Chainloader.PluginInfos.Values)
            {
                if (thinkPlugins.Metadata.GUID == thinkerAPI.Instance.Info.Metadata.GUID) continue;
                if (thinkPlugins.Instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).ToList().Exists(x => x.FieldType.Equals(typeof(MassObjectHolder))))
                {
                    ModdedSaveGame.AddSaveHandler(thinkPlugins);
                    UnityEngine.Debug.Log($"Handling save for {thinkPlugins.Metadata.GUID}!");
                    HashSet<LevelObject> weAlreadyGotToThat = new HashSet<LevelObject>();
                    foreach (var scene in thinkPlugins.Instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList().FindAll(x => x.FieldType.Equals(typeof(SceneObject))))
                    {
                        var gottenscene = (SceneObject)scene.GetValue(thinkPlugins.Instance);
                        foreach (var levelObject in gottenscene.GetCustomLevelObjects()) // This is better... I think?
                        {
                            if ((int)levelObject.type >= 123 && ENanmEXTENDED.counts[typeof(LevelType)].names[(int)levelObject.type - 123] == extendedEnums[(int)levelObject.type - 123].ToStringExtended())
                                levelObject.type = extendedEnums[(int)levelObject.type - 123];
                            else if ((int)levelObject.type > 0 && (int)levelObject.type < 4) // Custom level objects that have does not use a extended enum. (I blame Skid for that)
                                levelObject.type = EnumExtensions.ExtendEnum<LevelType>("UnknownType_" + levelObject.name); // Not to be confused with assigning things horribly.
                            weAlreadyGotToThat.Add(levelObject);
                        }
                        gottenscene.AddMeta(thinkPlugins.Instance, gottenscene.levelTitle == "END" ? ["endless"] : []);
                        GeneratorManagement.EnqueueGeneratorChanges(gottenscene);
                        UnityEngine.Debug.Log($"Enqueuing generation changes for {scene.Name} from {thinkPlugins.Metadata.GUID}!");
                    }
                }
            }
            foreach (var levelObject in SceneObjectMetaStorage.Instance.FindAll(x => x.title == "F4" || x.title == "F5").SelectMany(x => x.value.GetCustomLevelObjects())) // Now I realized that I am forgetting something...
            {
                if ((int)levelObject.type >= 123 && ENanmEXTENDED.counts[typeof(LevelType)].names[(int)levelObject.type - 123] == extendedEnums[(int)levelObject.type - 123].ToStringExtended())
                    levelObject.type = extendedEnums[(int)levelObject.type - 123];
            }
        }
        IEnumerator Savefixes()
        {
            yield return 1;
            yield return "Forcing MTM101API to regenerate tags";
            ModdedFileManager.Instance.RegenerateTags();
        }
        LoadingEvents.RegisterOnAssetsLoaded(Info, Postdoings(), LoadingEventOrder.Pre);
        LoadingEvents.RegisterOnAssetsLoaded(Info, WindowPeeBugManager.InitializeWPD, LoadingEventOrder.Post);
        LoadingEvents.RegisterOnAssetsLoaded(Info, Savefixes(), LoadingEventOrder.Final);
    }
}
