#if false
using HarmonyLib;
using PineDebug;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ThinkerAPI;
using UnityEngine;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI;
using System.Linq;
using UnityEngine.UI;
using MTM101BaldAPI.Registers;
using TMPro;
using UnityEngine.SceneManagement;

namespace APIConnector.PineDebug;

static internal class PineDebugSupport
{
    public static void Initalize()
    {
        var npclist = PineDebugManager.PineButtonList.Get("NPCs");
        foreach (var npc in WindowPeeBugManager.npcs)
        {
            Texture2D icon = PineDebugManager.pinedebugAssets.Get<Texture2D>("BorderUnknown");
            if (PineDebugManager.pinedebugAssets.ContainsKey("Border" + npc.Character.ToStringExtended()))
                icon = PineDebugManager.pinedebugAssets.Get<Texture2D>("Border" + npc.Character.ToStringExtended());
            else if (npc.Poster?.baseTexture != null && npc.Poster.baseTexture.width == 256 && npc.Poster.baseTexture.height == 256)
            {
                Color[] pixels = npc.Poster.baseTexture.GetPixels(23, 129, 80, 72);
                for (int i = 0; i < pixels.Length; i++)
                    if (pixels[i] == Color.black)
                        pixels[i] = Color.clear;
                icon = new Texture2D(80, 72);
                icon.SetPixels(pixels);
                icon.Apply();
            }
            npclist.Add(
                    PineDebugManager.CreateButton(npc.name, () =>
                    {
                        if (GlobalCam.Instance.TransitionActive && (BaseGameManager.Instance == null || BaseGameManager.Instance.Ec == null))
                            return;
                        Vector3 position = Singleton<CoreGameManager>.Instance.GetPlayer(0).transform.position;
                        Vector3 floorWorldPosition = BaseGameManager.Instance.Ec.RandomCell(false, false, true).FloorWorldPosition;
                        if (BaseGameManager.Instance.Ec.rooms.Find(j => npc.spawnableRooms.Contains(j.category))) // A room exists with that category!
                        {
                            if (BaseGameManager.Instance.Ec.rooms.Find(j => npc.spawnableRooms.Contains(j.category)).category == RoomCategory.Null
                            || BaseGameManager.Instance.Ec.rooms.FindAll(j => npc.spawnableRooms.Contains(j.category)).Count <= 0)
                                return;
                            floorWorldPosition = BaseGameManager.Instance.Ec.rooms.FindAll(j => npc.spawnableRooms.Contains(j.category))
                            [UnityEngine.Random.RandomRangeInt(0, BaseGameManager.Instance.Ec.rooms.FindAll(j => npc.spawnableRooms.Contains(j.category)).Count - 1)]
                            .RandomEntitySafeCellNoGarbage().FloorWorldPosition;
                        }
                        else if (npc.spawnableRooms.Contains(RoomCategory.Hall))
                        {
                            if (BaseGameManager.Instance.Ec.rooms.FindAll(j => npc.spawnableRooms.Contains(RoomCategory.Hall)).Count <= 0)
                                return;
                            int randomChose = UnityEngine.Random.RandomRangeInt(0, BaseGameManager.Instance.Ec.FindHallways().Count - 1);
                            floorWorldPosition = BaseGameManager.Instance.Ec.FindHallways()[randomChose][BaseGameManager.Instance.Ec.FindHallways()[randomChose].Count - 1].FloorWorldPosition;
                        }
                        if (npc.IgnorePlayerOnSpawn || (Vector3.Distance(floorWorldPosition, position) > (float)BaseGameManager.Instance.Ec.ReflectionGetVariable("npcSpawnBufferRadius") && (floorWorldPosition.x > position.x + (float)BaseGameManager.Instance.Ec.ReflectionGetVariable("npcSpawnBufferWidth") / 2f || floorWorldPosition.x < position.x - (float)BaseGameManager.Instance.Ec.ReflectionGetVariable("npcSpawnBufferWidth") / 2f) && (floorWorldPosition.z > position.z + (float)BaseGameManager.Instance.Ec.ReflectionGetVariable("npcSpawnBufferWidth") / 2f || floorWorldPosition.z < position.z - (float)BaseGameManager.Instance.Ec.ReflectionGetVariable("npcSpawnBufferWidth") / 2f)))
                        {
                            BaseGameManager.Instance.Ec.SpawnNPC(npc, IntVector2.GetGridPosition(floorWorldPosition));
                            if (npc.Character == Character.Baldi && BaseGameManager.Instance.Ec.angerOnSpawn)
                                BaseGameManager.Instance.AngerBaldi(BaseGameManager.Instance.NotebookAngerVal);
                        }
                        PineDebugManager.Instance.audMan.PlaySingle(PineDebugManager.pinedebugAssets.Get<SoundObject>("Button2"));
                    }, icon));
        }
        var itemlist = PineDebugManager.PineButtonList.Get("Items");
        HashSet<ItemMetaData> metas = [];
        foreach (var item in WindowPeeBugManager.items)
            metas.Add(item?.GetMeta());
        metas.Do(x =>
        {
            Texture2D icon = x.value.itemSpriteSmall.texture;
            if (x.flags.HasFlag(ItemFlags.InstantUse) || x.id == Items.None)
                return;
            else if (x.itemObjects.Length > 1)
            {
                PineDebugManager.PineButtonList prefabButtons = new PineDebugManager.PineButtonList("ItemsSubList_" + x.value.name);
                x.itemObjects.Do(p =>
                {
                    prefabButtons.Add(PineDebugManager.CreateButton(LocalizationManager.Instance.GetLocalizedText(p.nameKey), () =>
                    {
                        if (GlobalCam.Instance.TransitionActive && (BaseGameManager.Instance == null || BaseGameManager.Instance.Ec == null))
                            return;
                        CoreGameManager.Instance.GetPlayer(0).itm.AddItem(p);
                        PineDebugManager.Instance.audMan.PlaySingle(PineDebugManager.pinedebugAssets.Get<SoundObject>("Button2"));
                    }, p.itemSpriteSmall.texture));
                });
                itemlist.Add(PineDebugManager.CreateButton(LocalizationManager.Instance.GetLocalizedText(x.nameKey), () =>
                {
                    PineDebugManager.Instance.ChangePage(itemlist, prefabButtons);
                    PineDebugManager.Instance.audMan.PlaySingle(PineDebugManager.pinedebugAssets.Get<SoundObject>("Button1"));
                }, icon));
                return;
            }
            else
            {

                itemlist.Add(PineDebugManager.CreateButton(LocalizationManager.Instance.GetLocalizedText(x.nameKey), () =>
                {
                    if (GlobalCam.Instance.TransitionActive && BaseGameManager.Instance == null)
                        return;
                    CoreGameManager.Instance.GetPlayer(0).itm.AddItem(x.value);
                    PineDebugManager.Instance.audMan.PlaySingle(PineDebugManager.pinedebugAssets.Get<SoundObject>("Button2"));
                }, icon));
            }
        });
        var eventList = PineDebugManager.PineButtonList.Get("Events");
        foreach (var _event in thinkerAPI.modEvents)
        {
            var x = _event.eventscript;
            Texture2D icon = PineDebugManager.pinedebugAssets.Get<Texture2D>("BorderUnknown");
            if (PineDebugManager.pinedebugAssets.ContainsKey("Border" + x.Type.ToStringExtended()))
                icon = PineDebugManager.pinedebugAssets.Get<Texture2D>("Border" + x.Type.ToStringExtended());
            switch (x.GetMeta()?.flags)
            {
                default:
                    eventList.Add(
                        PineDebugManager.CreateButton(x.Type.ToStringExtended(), () =>
                        {
                            if (GlobalCam.Instance.TransitionActive && (BaseGameManager.Instance == null || BaseGameManager.Instance.Ec == null))
                                return;
                            RandomEvent _event = UnityEngine.Object.Instantiate(x);
                            _event.transform.SetParent(BaseGameManager.Instance.Ec.transform, false);
                            System.Random rng = new System.Random(CoreGameManager.Instance.Seed());
                            _event.Initialize(BaseGameManager.Instance.Ec, rng);
                            _event.SetEventTime(rng);
                            BaseGameManager.Instance.Ec.AddEvent(_event, 0f);
                            _event.Begin();
                            PineDebugManager.Instance.audMan.PlaySingle(PineDebugManager.pinedebugAssets.Get<SoundObject>("Button2"));
                        }, icon));
                    break;
                case RandomEventFlags.Permanent or RandomEventFlags.AffectsGenerator:
                    eventList.Add(
                        PineDebugManager.CreateButton(x.Type.ToStringExtended(), () =>
                        {
                            if (GlobalCam.Instance.TransitionActive && (BaseGameManager.Instance == null || BaseGameManager.Instance.Ec == null))
                                return;
                            RandomEvent _event = UnityEngine.Object.FindObjectOfType(x.GetType()) as RandomEvent;
                            if (_event != null) _event.Begin();
                            PineDebugManager.Instance.audMan.PlaySingle(PineDebugManager.pinedebugAssets.Get<SoundObject>("Button2"));
                        }, icon));
                    break;
            }
        }
        var levelList = PineDebugManager.PineButtonList.Get("Levels");
        Texture2D transparent = Resources.FindObjectsOfTypeAll<Texture2D>().ToList().Find(x => x.name == "Transparent");
        foreach (var level in Resources.FindObjectsOfTypeAll<SceneObject>())
        {
            if (levelList.Exists(x => x.name == level.name.ToLower().Replace(" ", "_"))) continue;
            if (level.levelObject != null || level.randomizedLevelObject?.Length > 0)
            {
                var butt = PineDebugManager.CreateButton(level.name, () =>
                {
                    PineDebugManager.Instance.audMan.PlaySingle(PineDebugManager.pinedebugAssets.Get<SoundObject>("Button2"));
                    GlobalCam.Instance.Transition(UiTransition.Dither, 0.01666667f);
                    PineDebugManager.Instance.mainCanvas.gameObject.SetActive(false);
                    CoreGameManager.Instance.disablePause = PineDebugManager.Instance.mainCanvas.gameObject.activeSelf;
                    if (BasePlugin.pauseInMenu.Value)
                        Time.timeScale = PineDebugManager.Instance.mainCanvas.gameObject.activeSelf ? 0f : 1f;
                    // Loading like it was...
                    BaseGameManager.Instance.StopAllCoroutines();
                    BaseGameManager.Instance.Ec.ResetEvents();
                    Time.timeScale = 0f;
                    CoreGameManager.Instance.readyToStart = false;
                    CoreGameManager.Instance.disablePause = true;
                    PropagatedAudioManager.paused = true;

                    BaseGameManager.Instance.ReflectionSetVariable("elevatorScreen", UnityEngine.Object.Instantiate((ElevatorScreen)BaseGameManager.Instance.ReflectionGetVariable("elevatorScreenPre")));
                    ElevatorScreen elv = BaseGameManager.Instance.ReflectionGetVariable("elevatorScreen") as ElevatorScreen;
                    elv.OnLoadReady += () =>
                    {
                        // So we're reusing these actions again?
                        BaseGameManager.Instance.StopAllCoroutines();
                        BaseGameManager.Instance.Ec.ResetEvents();
                        Time.timeScale = 0f;
                        CoreGameManager.Instance.readyToStart = false;
                        CoreGameManager.Instance.disablePause = true;
                        PropagatedAudioManager.paused = true;

                        // Here's the real deal!
                        CoreGameManager.Instance.PrepareForReload();
                        CoreGameManager.Instance.SetLives(3, true);
                        CoreGameManager.Instance.tripPlayed = false;
                        SubtitleManager.Instance.DestroyAll();
                        CoreGameManager.Instance.sceneObject = level;
                        SceneManager.LoadSceneAsync("Game");
                    };
                    elv.Initialize();
                }, transparent);
                TMP_Text test = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                test.transform.SetParent(butt.button.transform.Find("Image"), false);
                test.gameObject.layer = LayerMask.NameToLayer("UI");
                test.alignment = TextAlignmentOptions.Center;
                test.richText = false;
                test.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().ToList().Find(f => f.name == "COMIC_18_Pro");
                test.color = Color.black;
                test.fontSize += 12f;
                test.raycastTarget = false;
                test.text = level.levelTitle;

                levelList.Add(butt);
            }
        }
        var theme = PineDebugManager.Instance.ReflectionGetVariable("themeObjects") as ThemeObjects;
        foreach (var button in npclist)
            button.button.GetComponent<RawImage>().texture = theme.Themes[ThemeOptions.curen.themename].button;
        foreach (var button in itemlist)
            button.button.GetComponent<RawImage>().texture = theme.Themes[ThemeOptions.curen.themename].button;
        foreach (var button in eventList)
            button.button.GetComponent<RawImage>().texture = theme.Themes[ThemeOptions.curen.themename].button;
    }
}
#endif