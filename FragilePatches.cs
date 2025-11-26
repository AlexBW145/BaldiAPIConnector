using brobowindowsmod;
using brobowindowsmod.ItemScripts;
using brobowindowsmod.NPCs;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ThinkerAPI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace APIConnector;

[ConditionalPatchMod("OurWindowsFragiled"), HarmonyPatch]
internal class FragilePatches
{
    private static IEnumerable<CodeInstruction> Why(IEnumerable<CodeInstruction> instructions)  // Thinker, why the fuck did you add in an enum extension system in Fragile Windows??
    {
        // I am using this now...
        var instructionMatch = new CodeMatcher(instructions);
        instructionMatch.Start().MatchForward(true, new CodeMatch(x => x.opcode == OpCodes.Ldtoken));
        var generic = instructionMatch.Instruction.operand;
        var enumType = AccessTools.TypeByName(generic.ToString());
        //
        return new List<CodeInstruction>()
        {
            new CodeInstruction(OpCodes.Nop),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThinkerAPI.ENanmEXTENDED), nameof(ThinkerAPI.ENanmEXTENDED.GetAnEnumThatDoesntExist), [typeof(string)], [enumType])),
            new CodeInstruction(OpCodes.Ret)
        };
    }

    [HarmonyPatch(typeof(FragileWindowBase), nameof(FragileWindowBase.MakeWindowWorld)), HarmonyPostfix]
    private static void WindowWorldEnumSetToUnique(ref LevelObject __result)
    {
        if (!EnumExtensions.EnumWithExtendedNameExists<LevelType>("WindowWorld"))
            EnumExtensions.ExtendEnum<LevelType>("WindowWorld");
        __result.type = EnumExtensions.GetFromExtendedName<LevelType>("WindowWorld");
    }

    internal static void PatchFragile(Harmony harmony, Assembly[] assemblies)
    {
        IEnumerator LoadingAssetsFragile() // Manually add assets in a cleaner way.
        {
            yield return 1;
            yield return "Loading Fragile Assets..."; // Not to be confused with another of these methods, which does the same thing but with more stuff to implement.
            yield return FragileWindowBase.Instance.AddAssetFolder("ETC");
            yield return FragileWindowBase.Instance.AddAssetFolder("ExtraBreakSounds");
            var Assets = FragileWindowBase.Instance.Assets;
            if (Assets.Assets.Count == 0)
            {
                MTM101BaldiDevAPI.CauseCrash(FragileWindowBase.Instance.Info, new FileLoadException("Assets are not installed correctly!"));
                yield break;
            }

            ConnectorBasicsPlugin.Doings = true; // Yeah so...
            yield return new WaitUntil(() => !Assets.isLoadingAssets());

            List<string> foldersToLoad = new List<string>
            {
                "Posters", "Items/WindowYTP", "Items/CupOLittleWindows", "Items/GlassShard", "Items/Stone", "Items/WindowConstructionKit", "Items/WindowBlaster", "Items/ShardSoda", "Items/SuperHammer", "Items/EdibleWindow",
                "Items/PlaceableFloorWindow", "Items/WindowSuit", "Items/GlassCannon", "Items/MagnifyingGlass/Sprites", "Items/MagnifyingGlass/Audio", "WindowVariants/ClickableWindow", "WindowVariants/TrapWindow", "WindowVariants/DirtyWindow", "WindowVariants/TestWindow", "WindowVariants/PortalWindow",
                "WindowVariants/MechanicalHammerWindow", "WindowVariants/LoudWindow", "WindowVariants/IceWindow", "WindowVariants/ConvenientWindow", "WindowVariants/LinkedWindow", "WindowVariants/ElectricWindow", "WindowVariants/PushyWindow", "WindowVariants/OnewayWindow", "WindowVariants/PlasticWindow", "WindowVariants/GiftingWindow",
                "WindowVariants/CloudyWindow", "WindowVariants/CoinWindow", "WindowVariants/TeethyWindow", "WindowVariants/SecurityWindow", "LittleWindowVariants/Coin", "LittleWindowVariants/Teethy", "LittleWindowVariants/Security", "LittleWindowVariants/Clickable", "LittleWindowVariants/Trap", "LittleWindowVariants/Dirty",
                "LittleWindowVariants/Test", "LittleWindowVariants/Portal", "LittleWindowVariants/MechanicalHammer", "LittleWindowVariants/Loud", "LittleWindowVariants/Ice", "LittleWindowVariants/Convenient", "LittleWindowVariants/Linked", "LittleWindowVariants/Electric", "LittleWindowVariants/Pushy", "LittleWindowVariants/Cloudy",
                "LittleWindowVariants/Oneway", "LittleWindowVariants/Gifting", "LittleWindowVariants/Classic", "LittleWindowVariants/Plastic", "NPCs/LittleWindowGuy", "NPCs/Windowborf", "NPCs/MrDemolition", "Structures/LWGMachine", "Structures/FloorWindow", "Rooms/GlassHouse",
                "EditorUI"
            };
            var path = FragileWindowBase.Instance.path;
            foreach (string s in foldersToLoad)
            {
                string[] asset = Directory.GetFiles(Path.Combine(path, s));
                foreach (string ss in asset)
                {
                    var actualPath = Path.Combine(path, s, ss);
                    if (s == "ExtraBreakSounds")
                        FragileWindowBase.Instance.windowsoundcount++;
                    else if (s == "Posters")
                        FragileWindowBase.Instance.myPosterAssetNames.Add(Path.GetFileNameWithoutExtension(actualPath));

                    string extension = Path.GetExtension(actualPath);
                    if (extension.ToLower() == ".png")
                    {
                        Texture2D texture2D = AssetLoader.TextureFromFile(actualPath);
                        Sprite sprite = AssetLoader.SpriteFromTexture2D(texture2D, Vector2.one / 2f, Mathf.Max(texture2D.width, texture2D.height));
                        sprite.name = Path.GetFileNameWithoutExtension(actualPath);
                        Assets.AddAsset(sprite);
                    }
                    else if (extension.ToLower() == ".ogg" || extension.ToLower() == ".mp3" || extension.ToLower() == ".wav")
                    {
                        AudioClip aud = AssetLoader.AudioClipFromFile(actualPath);
                        aud.name = Path.GetFileNameWithoutExtension(actualPath);
                        Assets.AddAsset(aud);
                    }
                }
            }

            yield return new WaitUntil(() => !Assets.isLoadingAssets());
        }
        IEnumerator LoadingStuffFragile() // Manually do stuff in a cleaner way.
        {
            yield return 1;
            yield return "Welcome to Fragile Windows V3, where we break windows.";
            if (FloorStuff.F1Lvl == null)
                FloorStuff.LoadBaseGameFloors();
            FragileWindowBase f = FragileWindowBase.Instance;
            var Assets = FragileWindowBase.Instance.Assets;
            List<BasicSoundobjectTemplate> sos = new List<BasicSoundobjectTemplate>
            {
                new BasicSoundobjectTemplate(SoundType.Effect, Color.white),
                new BasicSoundobjectTemplate(SoundType.Voice, new Color(1f, 0.6f, 0.2f)),
                new BasicSoundobjectTemplate(SoundType.Effect, new Color(0.8f, 1f, 0.9f)),
                new BasicSoundobjectTemplate(SoundType.Voice, Color.cyan)
            };
            List<BasicItemTemplate> basicItemTemplates = new List<BasicItemTemplate>
            {
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("WindowYTPIcon"), (Sprite)f.GetAsset("WindowYTPIcon"), 50, new List<string> { "F1", "F2", "F3", "END" }, new List<int> { 50, 90, 105, 90 }, 30, f.CreateSoundObject((AudioClip)f.GetAsset("YTPPickup_Window"), sos[0], subtitle: false), "YTPPickup_Window", "YTPPickup_Window", "WindowYTP", instantusea: true, typeof(ITM_YTPWindow), isShopa: false, 0),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("LittleWindowCupSmall"), (Sprite)f.GetAsset("LittleWindowCupLarge"), 500, new List<string> { "F2", "F3", "END" }, new List<int> { 110, 125, 110 }, 50, null, "LWGCup", "LWGCupDesc", "LittleWindowCup", instantusea: false, typeof(ITM_CupOfWindow), isShopa: true, 110),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("GlassShardSmall"), (Sprite)f.GetAsset("GlassShardLarge"), 123, new List<string>(), new List<int>(), 20, null, "GlassShardName", "GlassShardDesc", "GlassShard", instantusea: false, typeof(ITM_GlassShard), isShopa: false, 110),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("StoneSmall"), (Sprite)f.GetAsset("StoneLarge"), 100, new List<string> { "F1", "F2", "F3", "END" }, new List<int> { 120, 140, 180, 140 }, 30, null, "StoneName", "StoneDesc", "Stone", instantusea: false, typeof(ITM_Stone), isShopa: true, 135),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("WCKSmall"), (Sprite)f.GetAsset("WCKLarge"), 500, new List<string> { "F2", "F3", "END" }, new List<int> { 100, 130, 100 }, 60, null, "WCKName", "WCKDesc", "WindowPlacer", instantusea: false, typeof(ITM_WindowConstructionKit), isShopa: true, 120),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("WindowBlasterSmall"), (Sprite)f.GetAsset("WindowBlasterLarge"), 300, new List<string>(), new List<int>(), 70, null, "WindowBlasterName5", "WindowBlasterDesc", "WindowBlaster5", instantusea: false, typeof(ITM_WindowBlaster), isShopa: false, 110),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("WindowBlasterSmall"), (Sprite)f.GetAsset("WindowBlasterLarge"), 0, new List<string>(), new List<int>(), 0, null, "WindowBlasterName4", "WindowBlasterDesc", "WindowBlaster4", instantusea: false, typeof(ITM_WindowBlaster), isShopa: false, 0),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("WindowBlasterSmall"), (Sprite)f.GetAsset("WindowBlasterLarge"), 0, new List<string> { "F3" }, new List<int> { 70 }, 0, null, "WindowBlasterName3", "WindowBlasterDesc", "WindowBlaster3", instantusea: false, typeof(ITM_WindowBlaster), isShopa: false, 0),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("WindowBlasterSmall"), (Sprite)f.GetAsset("WindowBlasterLarge"), 0, new List<string> { "F2", "F3", "END" }, new List<int> { 80, 80, 80 }, 0, null, "WindowBlasterName2", "WindowBlasterDesc", "WindowBlaster2", instantusea: false, typeof(ITM_WindowBlaster), isShopa: false, 0),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("WindowBlasterSmall"), (Sprite)f.GetAsset("WindowBlasterLarge"), 0, new List<string> { "F1", "F2", "F3", "END" }, new List<int> { 90, 90, 90, 90 }, 0, null, "WindowBlasterName1", "WindowBlasterDesc", "WindowBlaster1", instantusea: false, typeof(ITM_WindowBlaster), isShopa: false, 0),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("ShardSodaSmall"), (Sprite)f.GetAsset("ShardSodaLarge"), 300, new List<string> { "F1", "F2", "F3", "END" }, new List<int> { 50, 130, 140, 130 }, 50, null, "ShardSodaName", "ShardSodaDesc", "ShardSoda", instantusea: false, typeof(ITM_ShardSoda), isShopa: true, 125),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("SuperHammerSmall"), (Sprite)f.GetAsset("SuperHammerLarge"), 900, new List<string> { "F3", "END" }, new List<int> { 80, 80 }, 90, null, "SuperHammerName", "SuperHammerDesc", "SuperHammer", instantusea: false, typeof(ITM_SuperHammer), isShopa: true, 120),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("PFWSmall"), (Sprite)f.GetAsset("PFWLarge"), 350, new List<string> { "F2", "F3", "END" }, new List<int> { 120, 140, 130 }, 60, null, "PFWName", "PFWDesc", "PFW", instantusea: false, typeof(ITM_PFW), isShopa: true, 125),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("WindowSuitSmall"), (Sprite)f.GetAsset("WindowSuitLarge"), 400, new List<string> { "F2", "F3", "END" }, new List<int> { 100, 120, 100 }, 60, null, "WindowSuitName", "WindowSuitDesc", "WindowSuit", instantusea: false, typeof(ITM_WindowSuit), isShopa: true, 125),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("GlassCannonSmall"), (Sprite)f.GetAsset("GlassCannonLarge"), 500, new List<string> { "F2", "F3", "END" }, new List<int> { 100, 120, 100 }, 60, null, "GlassCannonName", "GlassCannonDesc", "GlassCannon", instantusea: false, typeof(ITM_GlassCannon), isShopa: true, 125),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("MarbleIconSmall"), (Sprite)f.GetAsset("MarbleIconLarge"), 250, new List<string> { "F1", "F2", "F3", "END" }, new List<int> { 90, 120, 145, 130 }, 40, null, "MarbleName", "MarbleDesc", "Marble", instantusea: false, typeof(ITM_Marble), isShopa: false, 110),
                new BasicItemTemplate(Assets, (Sprite)f.GetAsset("magnifyingglassSmall"), (Sprite)f.GetAsset("magnifyingglassLarge"), 100, new List<string>(), new List<int>(), 20, null, "MagnifyingGlassName", "MagnifyingGlassDesc", "MagnifyingGlass", instantusea: false, typeof(ITM_Magnify), isShopa: false, 130)
            };
            var floorwindowbuilder = new GameObject("fwbuilder");
            floorwindowbuilder.AddComponent<FloorWindowBuilder>();
            thinkerAPI.MakePrefab(floorwindowbuilder, active: true);
            f.floorwindowbuilder = floorwindowbuilder;
            var windowdispenserbuilder = new GameObject("wdbuilder");
            windowdispenserbuilder.AddComponent<WindowDispenserBuilder>();
            thinkerAPI.MakePrefab(windowdispenserbuilder, active: true);
            f.windowdispenserbuilder = windowdispenserbuilder;
            var windowSwapperbuilder = new GameObject("wrbuilder");
            windowSwapperbuilder.AddComponent<WindowReplacerStructureThing>();
            thinkerAPI.MakePrefab(windowSwapperbuilder, active: true);
            f.windowSwapperbuilder = windowSwapperbuilder;
            List<BasicBuilderTemplate> basicObjectTemplates = new List<BasicBuilderTemplate>
            {
                new BasicBuilderTemplate(Assets, "Floor Window Builder", new StructureWithParameters
                {
                    parameters = new StructureParameters
                    {
                        chance = new float[1] { 1983f },
                        minMax = new IntVector2[1]
                        {
                            new IntVector2(1, 1)
                        },
                        prefab = new WeightedGameObject[1]
                        {
                            new WeightedGameObject
                            {
                                selection = floorwindowbuilder,
                                weight = 1983
                            }
                        }
                    },
                    prefab = floorwindowbuilder.GetComponent<StructureBuilder>()
                }, new List<string> { "F2", "F3", "END" }, new List<int> { 127, 190, 160 }),
                new BasicBuilderTemplate(Assets, "Window Dispenser Builder", new StructureWithParameters
                {
                    parameters = new StructureParameters
                    {
                        chance = new float[1] { 1983f },
                        minMax = new IntVector2[1]
                        {
                            new IntVector2(1, 1)
                        },
                        prefab = new WeightedGameObject[1]
                        {
                            new WeightedGameObject
                            {
                                selection = windowdispenserbuilder,
                                weight = 1983
                            }
                        }
                    },
                    prefab = windowdispenserbuilder.GetComponent<StructureBuilder>()
                }, new List<string> { "F2", "F3", "END" }, new List<int> { 280, 340, 400 })
            };
            foreach (BasicBuilderTemplate bit2 in basicObjectTemplates)
                thinkerAPI.modObjects.Add(bit2);

            foreach (BasicItemTemplate bit in basicItemTemplates)
                thinkerAPI.modItems.Add(bit);

            new WaitUntil(() => !Assets.isLoadingAssets());

            // This is unclean, why did I leave this here?
            // Also uhh, `GetAsset` is not a generic method and does not return a generic object.
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("WindowRespawn"), sos[0], subtitle: true), "WindowRespawnSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("TrapWindowBuzz"), sos[0], subtitle: true), "TrapWindowBuzzSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("StrongSoundWaveWindowBreak"), sos[0], subtitle: true), "StrongSoundWaveWindowBreakSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("TestWindowActivate"), sos[0], subtitle: false), "TestWindowActivateSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("LittleWindowCupUse"), sos[0], subtitle: true), "LittleWindowCupUseSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("GlassShardStab"), sos[0], subtitle: true), "GlassShardStabSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("PrincipalNoBreakingWindow"), sos[1], subtitle: true), "PrincipalNoBreakingWindowSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("GlassCrack"), sos[0], subtitle: true), "GlassCrackSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("GoofyScream"), sos[0], subtitle: true), "GoofyScreamSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("LWGMachineScramble"), sos[0], subtitle: true), "LWGMachineScrambleSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("LWGMachineDispense"), sos[0], subtitle: true), "LWGMachineDispenseSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("LWGMachineTumble"), sos[0], subtitle: true), "LWGMachineTumbleSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("LWGMachineThud"), sos[0], subtitle: true), "LWGMachineThudSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("StoneHit"), sos[0], subtitle: true), "StoneHitSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("WCKUse"), sos[0], subtitle: true), "WCKUseSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("WindowBlast"), sos[0], subtitle: true), "WindowBlastSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("WindowBlastGet"), sos[0], subtitle: true), "WindowBlastGetSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("WindowSuitSound"), sos[0], subtitle: false), "WindowSuitSoundSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("GlassCannon_Place"), sos[0], subtitle: true), "GlassCannon_PlaceSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("GlassCannon_Shoot"), sos[0], subtitle: true), "GlassCannon_ShootSO");
            f.AddAsset(f.CreateSoundObject((AudioClip)f.GetAsset("GlassCannon_Hit"), sos[0], subtitle: true), "GlassCannon_HitSO");
            f.LoadSoundObjectsFromFolder("WindowVariants/ConvenientWindow", sos[0], subtitle: true);
            f.LoadSoundObjectsFromFolder("NPCs/LittleWindowGuy", sos[1], subtitle: true);
            f.LoadSoundObjectsFromFolder("NPCs/Windowborf", sos[2], subtitle: true);
            f.LoadSoundObjectsFromFolder("NPCs/MrDemolition", sos[3], subtitle: true);
            f.LoadSoundObjectsFromFolder("Items/MagnifyingGlass/Audio", sos[0], subtitle: false);

            yield return new WaitUntil(() => !Assets.isLoadingAssets());

            LittleWindowGuy.InitWindowVariants();
            f.worked = true;
            f.PostersPlease();

            yield return new WaitUntil(() => !Assets.isLoadingAssets());
            ConnectorBasicsPlugin.Doings = false;

            List<BasicNPCTemplate> basicNPCTemplates = new List<BasicNPCTemplate>
        {
            new BasicNPCTemplate(Assets, looker: false, new WeightedRoomAsset[0], new RoomCategory[1] { RoomCategory.Hall }, trigger: true, autorotate: true, precisenavigation: false, accelerates: false, ignorebelts: false, dontwaittospawn: false, airborne: false, "LittleWindowGuy", 0f, 99f, "Little Window Guy", 30f, 100f, "LWGName", "LWGDesc", f.GetTexture2D("NPCs/LittleWindowGuy/pri_littlewindowguy.png"), stationary: false, new List<string> { "F1", "F2", "F3", "F4", "F5", "END" }, new List<int> { 200, 160, 125, 99, 99, 180 }, typeof(LittleWindowGuy), useheatmap: false, isForced: false),
            new BasicNPCTemplate(Assets, looker: true, new WeightedRoomAsset[0], new RoomCategory[1] { RoomCategory.Hall }, trigger: true, autorotate: true, precisenavigation: true, accelerates: false, ignorebelts: false, dontwaittospawn: false, airborne: false, "Windowborf", 0f, 150f, "Windowborf Hammersnark", 30f, 100f, "WindowborfName", "WindowborfDesc", f.GetTexture2D("NPCs/Windowborf/pri_windowborf.png"), stationary: false, new List<string> { "F2", "F3", "F4", "F5", "END" }, new List<int> { 200, 190, 200, 190, 180 }, typeof(WindowBorf), useheatmap: false, isForced: false),
            new BasicNPCTemplate(Assets, looker: true, new WeightedRoomAsset[0], new RoomCategory[2]
            {
                RoomCategory.Faculty,
                RoomCategory.Special
            }, trigger: true, autorotate: true, precisenavigation: true, accelerates: false, ignorebelts: false, dontwaittospawn: false, airborne: false, "MrDemolition", 0f, 10000f, "Mr.Demolition", 30f, 100f, "mrd_Name", "mrd_Desc", f.GetTexture2D("NPCs/MrDemolition/pri_demolition.png"), stationary: false, new List<string> { "F3", "F4", "F5", "END" }, new List<int> { 220, 200, 200, 160 }, typeof(MrDemolition), useheatmap: false, isForced: false)
        };
            foreach (BasicNPCTemplate bit in basicNPCTemplates)
                thinkerAPI.modNPCs.Add(bit);

            // I cannot believe that most of those functions are public in general...
            f.GenerationAdditions();
            Assets.LoadSomeCaptions(Path.Combine(thinkerAPI.moddedpath, "OurWindowsFragiled")); // But why??
        }
        FragileWindowBase.Instance.StopAllCoroutines();
        thinkerAPI.LetWarningScreenContinue();
        LoadingEvents.RegisterOnAssetsLoaded(FragileWindowBase.Instance.Info, LoadingAssetsFragile(), LoadingEventOrder.Start);
        LoadingEvents.RegisterOnAssetsLoaded(FragileWindowBase.Instance.Info, LoadingStuffFragile(), LoadingEventOrder.Pre);
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.buildIndex == 0 && !ConnectorBasicsPlugin.Doings)
            {
                foreach (var _enum in AccessTools.AllTypes().Where(x => x.IsEnum && assemblies.Contains(x.Assembly) && x.IsPublic))
                    harmony.Patch(AccessTools.Method(typeof(brobowindowsmod.ENanmEXTENDED), nameof(brobowindowsmod.ENanmEXTENDED.GetAnEnumThatDoesntExist), [typeof(string)], [_enum]), transpiler: new HarmonyMethod(AccessTools.Method(typeof(FragilePatches), "Why")));
            }
        };
    }
}
