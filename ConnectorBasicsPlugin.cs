using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkerAPI;
using UnityEngine;

namespace APIConnector;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "10.0.0.0")]
[BepInDependency("thinkerAPI", "1.0.0.0")]
[BepInDependency("OurWindowsFragiled", BepInDependency.DependencyFlags.SoftDependency)]
[BepInProcess("BALDI.exe")]
public class ConnectorBasicsPlugin : BaseUnityPlugin
{
    private const string PLUGIN_GUID = "alexbw145.bbplus.apiconnector";
    private const string PLUGIN_NAME = "ThinkerAPI + MTM101API Connector";
    private const string PLUGIN_VERSION = "0.2.1.2";
    internal static ManualLogSource Log = new ManualLogSource("BaldiAPIConnector");

    internal static bool Connected = false;
    internal static bool Doings = false;
    private void Awake()
    {
        Harmony harmony = new Harmony(PLUGIN_GUID);
        Assembly[] assemblies = [Assembly.GetAssembly(typeof(Baldi)), ..AccessTools.AllTypes().Where(x => x.IsSubclassOf(typeof(BaseUnityPlugin))).Select(x => x.Assembly)]; // So I did this instead??
        foreach (var _enum in AccessTools.AllTypes().Where(x => x.IsEnum && assemblies.Contains(x.Assembly) && x.IsPublic)) // Found out how, but couldn't figure out HOW to exclude system & unity package enums.
            harmony.Patch(AccessTools.Method(typeof(ENanmEXTENDED), nameof(ENanmEXTENDED.GetAnEnumThatDoesntExist), [typeof(string)], [_enum]), transpiler: new HarmonyMethod(AccessTools.Method(typeof(ThinkerAPIPatches), "EnumFromMissedTheTexture")));
        harmony.PatchAllConditionals();
        // The generics are hardmode...
        if (Chainloader.PluginInfos.ContainsKey("OurWindowsFragiled"))
            FragilePatches.PatchFragile(harmony, assemblies);
        //

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
            foreach (var thinkPlugins in Chainloader.PluginInfos.Values)
            {
                if (thinkPlugins.Metadata.GUID == thinkerAPI.Instance.Info.Metadata.GUID) continue;
                if (thinkPlugins.Instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList().Exists(x => x.FieldType.Equals(typeof(MassObjectHolder))))
                {
                    ModdedSaveGame.AddSaveHandler(thinkPlugins);
                    Log.LogInfo($"Handling save for {thinkPlugins.Metadata.GUID}!");
                    HashSet<LevelObject> weAlreadyGotToThat = new HashSet<LevelObject>();
                    foreach (var scene in thinkPlugins.Instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList().FindAll(x => x.FieldType.Equals(typeof(SceneObject))))
                    {
                        var gottenscene = (SceneObject)scene.GetValue(thinkPlugins.Instance);
                        if (gottenscene == null)
                        {
                            Log.LogWarning($"SceneObject field {scene.Name} from {thinkPlugins.Metadata.GUID} is null! Skipping!");
                            continue;
                        }
                        if (SceneObjectMetaStorage.Instance.Get(gottenscene) != null) continue;
                        /*foreach (var levelObject in gottenscene.GetCustomLevelObjects()) // This is better... I think?
                        {
                            if ((int)levelObject.type >= 123 && ENanmEXTENDED.counts[typeof(LevelType)].names[(int)levelObject.type - 123] == extendedEnums[(int)levelObject.type - 123].ToStringExtended())
                                levelObject.type = extendedEnums[(int)levelObject.type - 123];
                            else if ((int)levelObject.type > 0 && (int)levelObject.type < 4) // Custom level objects that does not use a extended enum.
                                levelObject.type = EnumExtensions.ExtendEnum<LevelType>("UnknownType_" + levelObject.name); // Not to be confused with assigning things horribly.
                            weAlreadyGotToThat.Add(levelObject);
                        }*/
                        gottenscene.AddMeta(thinkPlugins.Instance, (gottenscene.levelTitle == "END" && gottenscene.manager is EndlessGameManager) ? ["endless"] : []);
                        // `HideInInspector` (despite its actual purpose is not in during runtime) will make the connector consider the scene object unqueueable.
                        if (!Attribute.IsDefined(scene, typeof(HideInInspector)))
                        {
                            GeneratorManagement.EnqueueGeneratorChanges(gottenscene);
                            Log.LogInfo($"Enqueuing generation changes for {scene.Name} from {thinkPlugins.Metadata.GUID}!");
                        }
                    }
                }
            }
            /*foreach (var levelObject in SceneObjectMetaStorage.Instance.FindAll(x => x.title == "F4" || x.title == "F5").SelectMany(x => x.value.GetCustomLevelObjects())) // Now I realized that I am forgetting something...
            {
                if ((int)levelObject.type >= 123 && ENanmEXTENDED.counts[typeof(LevelType)].names[(int)levelObject.type - 123] == extendedEnums[(int)levelObject.type - 123].ToStringExtended())
                    levelObject.type = extendedEnums[(int)levelObject.type - 123];
            }*/
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
