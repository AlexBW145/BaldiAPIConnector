using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkerAPI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace APIConnector;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "10.0.0.0")]
[BepInDependency("thinkerAPI", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("OurWindowsFragiled", BepInDependency.DependencyFlags.SoftDependency)]
[BepInProcess("BALDI.exe")]
public class ConnectorBasicsPlugin : BaseUnityPlugin
{
    private const string PLUGIN_GUID = "alexbw145.bbplus.apiconnector";
    private const string PLUGIN_NAME = "ThinkerAPI + MTM101API Connector";
    private const string PLUGIN_VERSION = "0.3.0.0";
    internal static ManualLogSource Log = new ManualLogSource("BaldiAPIConnector");

    internal static bool Connected = false, Doings = false, CaptionsLoaded = false;
    internal static int prevStoppers = 0;
    internal static readonly Dictionary<RandomEvent, Tuple<RandomEvent, string, Type, SoundObject, SoundObject>> randomEventsToQueue = new();
    private void Awake()
    {
        Harmony harmony = new Harmony(PLUGIN_GUID);
        thinkerAPI.WarningScreenLoaded = false;
        thinkerAPI.givemeaheadstart = false;
        prevStoppers = thinkerAPI.warningScreenBlockers;
        thinkerAPI.warningScreenBlockers = 0;
        Assembly[] assemblies = [Assembly.GetAssembly(typeof(Baldi)), ..AccessTools.AllTypes().Where(x => x.IsSubclassOf(typeof(BaseUnityPlugin))).Select(x => x.Assembly)]; // So I did this instead??
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.buildIndex == 0 && !Doings)
            {
                foreach (var _enum in AccessTools.AllTypes().Where(x => x.IsEnum && assemblies.Contains(x.Assembly) && x.IsPublic)) // Found out how, but couldn't figure out HOW to exclude system & unity package enums.
                    harmony.Patch(AccessTools.Method(typeof(ENanmEXTENDED), nameof(ENanmEXTENDED.GetAnEnumThatDoesntExist), [typeof(string)], [_enum]), transpiler: new HarmonyMethod(AccessTools.Method(typeof(ThinkerAPIPatches), "EnumFromMissedTheTexture")));
            }
        };
        harmony.PatchAllConditionals();
        // The generics are hardmode...
        if (Chainloader.PluginInfos.ContainsKey("OurWindowsFragiled"))
            FragilePatches.PatchFragile(harmony, assemblies);
        //

        IEnumerator Postdoings()
        {
            yield return 6;
            yield return "Connecting APIs...";
            Doings = true;
            #region CONNECTION PROCESS
            thinkerAPI.WarningScreenLoaded = true;
            thinkerAPI.assets.AddAssetFolder("WindowPeeBug");
            thinkerAPI.openschoolBuilder = new GameObject("OpenSchoolBuilder").AddComponent<Structure_OpenSchoolThing>();
            thinkerAPI.openschoolBuilder.gameObject.ConvertToPrefab(true);
            thinkerAPI.noexitBuilder = new GameObject("NoExitBuilder").AddComponent<Structure_PushPlayerSomewhereRandom>();
            thinkerAPI.noexitBuilder.gameObject.ConvertToPrefab(true);
            thinkerAPI.infBuilder = new GameObject("InfBuilder").AddComponent<Structure_InfStamina>();
            thinkerAPI.infBuilder.gameObject.ConvertToPrefab(true);

            yield return new WaitForSeconds(1f);
            yield return new WaitUntil(() => prevStoppers <= 0);
            Log.LogInfo("We got past through the stoppers.");
            thinkerAPI.WarningScreenPassed = true;
            int
                prevNPCs = thinkerAPI.modNPCs.Count,
                prevItems = thinkerAPI.modItems.Count,
                prevEvents = thinkerAPI.modEvents.Count,
                prevStructures = thinkerAPI.modObjects.Count,
                prevRooms = thinkerAPI.modRooms.Count,
                prevPosters = thinkerAPI.modPosters.Count;
            bool modified = true;
            IEnumerator WaitUntilFor() // Alternate solution because the loading screen will crash by a `yield return null`
            {
                while (modified)
                {
                    yield return new WaitForSeconds(1f);
                    modified = false;
                    if (prevNPCs != thinkerAPI.modNPCs.Count
                        || prevItems != thinkerAPI.modItems.Count
                        || prevEvents != thinkerAPI.modEvents.Count
                        || prevStructures != thinkerAPI.modObjects.Count
                        || prevRooms != thinkerAPI.modRooms.Count
                        || prevPosters != thinkerAPI.modPosters.Count)
                        modified = true;
                    prevNPCs = thinkerAPI.modNPCs.Count;
                    prevItems = thinkerAPI.modItems.Count;
                    prevEvents = thinkerAPI.modEvents.Count;
                    prevStructures = thinkerAPI.modObjects.Count;
                    prevRooms = thinkerAPI.modRooms.Count;
                    prevPosters = thinkerAPI.modPosters.Count;
                    yield return null;
                }
            }
            yield return StartCoroutine(WaitUntilFor());
            
            thinkerAPI.FileListPassed = true;
            var dummyMenu = new GameObject("Menu", typeof(DummyMenu));
            Log.LogInfo("We got past through checks.");

            foreach (BasicNPCTemplate _npc in thinkerAPI.modNPCs)
            {
                NPC npc = thinkerAPI.CreateNPC(_npc);
                npc.gameObject.name = _npc.name;
                _npc.moh.AddAsset(npc, $"{_npc.enumname}NPCObject");
                WindowPeeBugManager.npcs.Add(npc);
            }

            foreach (BasicItemTemplate _itemobject in thinkerAPI.modItems)
            {
                ItemObject itemobject = thinkerAPI.CreateItem(_itemobject);
                itemobject.name = _itemobject.enumName;
                _itemobject.moh.AddAsset(itemobject, $"{_itemobject.enumName}ItemObject");
                WindowPeeBugManager.items.Add(itemobject);
            }

            FieldInfo 
                _eventscript = AccessTools.DeclaredField(typeof(BasicEventTemplate), "eventscript"),
                modItsIn = AccessTools.DeclaredField(typeof(MassObjectHolder), "modImIn");
            foreach (BasicEventTemplate _randomevent in thinkerAPI.modEvents) // This is not even worthy to create a random event when looking at how random events are created with ThinkerAPI.
            {
                var dummyToGrab = randomEventsToQueue[_randomevent.eventscript];
                randomEventsToQueue.Remove(dummyToGrab.Item1);
                var randomEvent = dummyToGrab.Item1;
                var type = typeof(RandomEventBuilder<>).MakeGenericType(dummyToGrab.Item3);
                var builder = type.GetConstructor([typeof(PluginInfo)]).Invoke([Chainloader.PluginInfos[(string)modItsIn.GetValue(_randomevent.moh)]]);
                type.GetMethod("SetName", [typeof(string)]).Invoke(builder, [dummyToGrab.Item2]);
                type.GetMethod("SetEnum", [typeof(string)]).Invoke(builder, [dummyToGrab.Item2.Replace(" ", "").Replace("_", "")]);
                type.GetMethod("SetSound", [typeof(SoundObject)]).Invoke(builder, [dummyToGrab.Item4]);
                type.GetMethod("SetMinMaxTime", [typeof(float), typeof(float)]).Invoke(builder, [_randomevent.min, _randomevent.max]);
                if (dummyToGrab.Item5 != null)
                    type.GetMethod("SetJingle", [typeof(SoundObject)]).Invoke(builder, [dummyToGrab.Item5]);
                if (randomEvent.PotentialRoomAssets.Length != 0)
                    type.GetMethod("AddRoomAssets").Invoke(builder, [randomEvent.PotentialRoomAssets]);
                _eventscript.SetValue(_randomevent, (RandomEvent)type.GetMethod("Build").Invoke(builder, []));
                GameObject.DestroyImmediate(randomEvent);
            }

            DestroyImmediate(dummyMenu);
            // Note to most coders: ThinkerAPI does not use dictionary stuff for its own level generation management.
            Dictionary<Sprite, PosterObject> postersCreated = new();
            GeneratorManagement.Register(this, GenerationModType.Addend, (title, num, sceneObject) =>
            {
                var meta = sceneObject.GetMeta();
                if (meta == null || (!meta.tags.Contains("main") && !meta.tags.Contains("endless"))) return;
                if (sceneObject.potentialNPCs != null)
                {
                    foreach (BasicNPCTemplate modNPC in thinkerAPI.modNPCs)
                    {
                        NPC actualNPC = (NPC)modNPC.moh.GetAsset($"{modNPC.enumname}NPCObject");
                        for (int i = 0; i < modNPC.floorNames.Count; i++)
                        {
                            if (modNPC.floorNames[i] == title)
                            {
                                sceneObject.potentialNPCs.Add(new WeightedNPC()
                                {
                                    selection = actualNPC,
                                    weight = modNPC.floorWeights[i]
                                });
                            }
                        }
                    }
                }
                if (sceneObject.shopItems != null)
                {
                    foreach (BasicItemTemplate modItem in thinkerAPI.modItems)
                    {
                        if (modItem.isShop)
                        {
                            ItemObject actualItemObject = (ItemObject)modItem.moh.GetAsset($"{modItem.enumName}ItemObject");
                            sceneObject.shopItems = sceneObject.shopItems.AddToArray(new WeightedItemObject()
                            {
                                selection = actualItemObject,
                                weight = modItem.shopWeight
                            });
                        }
                    }
                }
                foreach (var levelObject in sceneObject.GetCustomLevelObjects())
                {
                    if (levelObject.IsModifiedByMod(Info)) continue;
                    if (levelObject.potentialItems != null)
                    {
                        foreach (BasicItemTemplate modItem in thinkerAPI.modItems)
                        {
                            ItemObject actualItemObject = (ItemObject)modItem.moh.GetAsset(modItem.enumName + "ItemObject");
                            for (int i = 0; i < modItem.floorNames.Count; i++)
                            {
                                if (modItem.floorNames[i] == title)
                                {
                                    levelObject.potentialItems = levelObject.potentialItems.AddToArray(new WeightedItemObject()
                                    {
                                        selection = actualItemObject,
                                        weight = modItem.weights[i]
                                    });
                                }
                            }
                        }
                    }
                    foreach (BasicBuilderTemplate modObject in thinkerAPI.modObjects)
                    {
                        for (int i = 0; i < modObject.floorNames.Count; i++)
                        {
                            if (modObject.floorNames[i] == title)
                            {
                                levelObject.potentialStructures = levelObject.potentialStructures.AddToArray(new WeightedStructureWithParameters()
                                {
                                    selection = modObject.builder,
                                    weight = modObject.floorWeights[i]
                                });
                            }
                        }
                    }
                    foreach (BasicRoomTemplate modRoom in thinkerAPI.modRooms)
                    {
                        for (int i = 0; i < modRoom.floorNames.Count; i++)
                        {
                            if (modRoom.floorNames[i] == title)
                            {
                                levelObject.roomGroup = levelObject.roomGroup.AddToArray(new RoomGroup()
                                {
                                    ceilingTexture = modRoom.ceiling,
                                    wallTexture = modRoom.wall,
                                    floorTexture = modRoom.floor,
                                    light = levelObject.hallLights, // Does not allow custom lights?? What the fuck?!
                                    minRooms = modRoom.min,
                                    maxRooms = modRoom.max,
                                    potentialRooms = modRoom.roomDesigns.ToArray(),
                                    stickToHallChance = modRoom.hallstickchance,
                                    name = modRoom.roomgroupname
                                });
                            }
                        }
                    }
                    foreach (BasicEventTemplate modEvent in thinkerAPI.modEvents)
                    {
                        for (int i = 0; i < modEvent.floorNames.Count; i++)
                        {
                            if (modEvent.floorNames[i] == title)
                            {
                                levelObject.randomEvents.Add(new WeightedRandomEvent()
                                {
                                    selection = modEvent.eventscript,
                                    weight = modEvent.floorWeights[i]
                                });
                            }
                        }
                    }
                    foreach (BasicPosterTemplate modPoster in thinkerAPI.modPosters)
                    {
                        PosterObject s;
                        if (postersCreated.ContainsKey(modPoster.poster[0])) // Because the delegate creates more posters than once...
                            s = postersCreated[modPoster.poster[0]];
                        else // And also it blindly creates the poster object with a sprite instead of a texture2D. (And also no localization inclusion...)
                        {
                            s = ObjectCreators.CreatePosterObject(modPoster.poster[0].texture, []);
                            postersCreated.Add(modPoster.poster[0], s);
                        }
                        for (int i = 0; i < modPoster.floorNames.Count; i++)
                        {
                            if (modPoster.floorNames[i] == title)
                            {
                                levelObject.posters = levelObject.posters.AddToArray(new WeightedPosterObject()
                                {
                                    selection = s,
                                    weight = modPoster.floorWeights[i]
                                });
                            }
                        }
                    }
                    levelObject.MarkAsModifiedByMod(Info);
                }
            });

            Connected = true;
            #endregion
            yield return new WaitUntil(() => Connected);
            if (thinkerAPI.captionpaths.Count > 0)
            {
                thinkerAPI.LoadSavedCaptions();
                thinkerAPI.captionpaths.Clear();
            }
            CaptionsLoaded = true;
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
            yield return 1 + ((Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.pinedebug") || !MTM101BaldiDevAPI.SaveGamesEnabled) ? 1 : 0);
            yield return "Forcing MTM101API to regenerate tags";
            ModdedFileManager.Instance.RegenerateTags();
            if (Chainloader.PluginInfos.ContainsKey("alexbw145.baldiplus.pinedebug") || !MTM101BaldiDevAPI.SaveGamesEnabled)
            {
                yield return "Initializing WindowPeeDebug...";
                WindowPeeBugManager.InitializeWPD(); // Due to PineDebug...
            }
        }
        LoadingEvents.RegisterOnAssetsLoaded(Info, FloorStuff.LoadBaseGameFloors, LoadingEventOrder.Start); // Due to Fragile Windows...
        LoadingEvents.RegisterOnAssetsLoaded(Info, Postdoings(), LoadingEventOrder.Pre);
        LoadingEvents.RegisterOnAssetsLoaded(Info, Savefixes(), LoadingEventOrder.Final);
    }
}

internal class DummyMenu : MainMenu
{
    private void Start()
    {

    }

    public new void UpdateNotif()
    {

    }
}