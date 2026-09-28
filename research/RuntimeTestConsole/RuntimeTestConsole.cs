// SPDX-License-Identifier: MPL-2.0
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace KeepersAlertsResearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class RuntimeTestConsole : BaseUnityPlugin
    {
        private const string PluginGuid =
            "nikichmods.keepersalerts.research.runtimetestconsole";
        private const string PluginName =
            "Keeper's Alerts Runtime Test Console";
        private const string PluginVersion = "0.1.2";

        private const string ConfessionEvent = "confession_available";
        private const string Confessional1 = "church_budka_1";
        private const string Confessional2 = "church_budka_2";
        private const string DonkeySound = "donkey_bell";

        // Accepted working UI baseline. Persistent values are energy-bar-local.
        private const float BaselineFirstSlotOffset = 24.92f;
        private const float BaselineSlotStep = 30.68f;
        private const float BaselineCorpseY = -6.25f;
        private const float BaselineCorpseScale = 0.62f;
        private const float BaselineConfessionY = -1.88f;
        private const float BaselineConfessionScale = 0.95f;
        private const float BaselineToastIconX = 5.53f;
        private const float BaselineToastIconY = 14.06f;
        private const float BaselineToastIconScale = 1.67f;
        private const float BaselineToastVisibleY = 80f;

        private static readonly Guid SupportedGameMvid =
            new Guid("6f50b8e7-156b-49ac-bbe8-7505894b2364");

        private const BindingFlags AllInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AllStatic =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private Rect _window = new Rect(20f, 120f, 650f, 820f);
        private Vector2 _scroll;
        private bool _visible = true;
        private string _status = "Waiting for Keeper's Alerts...";

        private object _production;
        private Type _productionType;
        private FieldInfo _corpseActive;
        private FieldInfo _confessionActive;
        private FieldInfo _corpseIndicator;
        private FieldInfo _confessionIndicator;
        private FieldInfo _confessionToast;
        private MethodInfo _renderHud;
        private MethodInfo _playCue;
        private MethodInfo _showToast;
        private MethodInfo _resyncConfession;
        private MethodInfo _resyncCorpse;
        private MethodInfo _tryGetCorpseCorridor;
        private MethodInfo _isBodyDrop;

        private Assembly _game;
        private Type _worldMapType;
        private Type _worldGameObjectType;
        private Type _mainGameType;
        private Type _gameSaveType;
        private Type _itemType;
        private Type _dropType;
        private Type _directionType;
        private Type _dropsListType;
        private Type _guiElementsType;
        private Type _newBodyArrivedGuiType;
        private Type _uiWidgetType;
        private Type _soundsType;

        private MethodInfo _getWgosByObjId;
        private MethodInfo _addInteractionEvent;
        private FieldInfo _customInteractionEvents;

        private FieldInfo _mainGameMe;
        private FieldInfo _mainGameSave;
        private FieldInfo _mainGameWorldRoot;
        private MethodInfo _generateBody;
        private MethodInfo _drop;
        private FieldInfo _dropCollected;
        private MethodInfo _destroyLinkedHint;
        private PropertyInfo _dropsListMe;
        private FieldInfo _drops;

        private PropertyInfo _guiElementsMe;
        private FieldInfo _guiBodyArrived;
        private MethodInfo _bodyArrivedDisplay;
        private MethodInfo _playSound;
        private PropertyInfo _uiWidgetAlpha;
        private PropertyInfo _uiWidgetWidth;

        private object _testConfessional;
        private bool _createdNativeConfession;
        private object _testCorpse;

        private bool _calibrationCaptured;
        private bool _holdConfessionToast;

        private float _firstSlotOffset;
        private float _slotStep;
        private float _corpseY;
        private float _corpseScale;
        private float _confessionY;
        private float _confessionScale;

        private float _toastPanelX;
        private float _toastPanelScale;
        private float _toastIconX;
        private float _toastIconY;
        private float _toastIconScale;
        private float _toastVisibleY;
        private float _toastBackgroundAlpha;

        private float _baselineToastPanelX;
        private float _baselineToastPanelScale;
        private float _baselineToastBackgroundAlpha;

        private void Awake()
        {
            try
            {
                BindHost();
                Logger.LogInfo(
                    PluginName + " " + PluginVersion
                    + " loaded. F9 toggles the console.");
            }
            catch (Exception ex)
            {
                Logger.LogError(PluginName + " disabled: " + ex);
                enabled = false;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
                _visible = !_visible;

            if (_production == null)
                TryBindProduction();

            if (!_calibrationCaptured)
                TryCaptureCalibration();

            if (_holdConfessionToast)
                MaintainHeldConfessionToast();
        }

        private void OnDestroy()
        {
            try
            {
                ClearConsoleCreatedConfession();
                RemoveTestCorpse();
            }
            catch
            {
            }
        }

        private void BindHost()
        {
            _game = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a =>
                    string.Equals(
                        a.GetName().Name,
                        "Assembly-CSharp",
                        StringComparison.Ordinal));

            if (_game == null)
                throw new InvalidOperationException("Assembly-CSharp is unavailable.");

            Guid actualMvid = _game.ManifestModule.ModuleVersionId;
            if (actualMvid != SupportedGameMvid)
            {
                throw new InvalidOperationException(
                    "Unsupported Assembly-CSharp MVID " + actualMvid
                    + "; expected " + SupportedGameMvid + ".");
            }

            _worldMapType = RequireGameType("WorldMap");
            _worldGameObjectType = RequireGameType("WorldGameObject");
            _mainGameType = RequireGameType("MainGame");
            _gameSaveType = RequireGameType("GameSave");
            _itemType = RequireGameType("Item");
            _dropType = RequireGameType("DropResGameObject");
            _directionType = RequireGameType("Direction");
            _dropsListType = RequireGameType("DropsList");
            _guiElementsType = RequireGameType("GUIElements");
            _newBodyArrivedGuiType = RequireGameType("NewBodyArrivedGUI");
            _soundsType = RequireGameType("Sounds");
            _uiWidgetType = FindTypeAcrossAssemblies("UIWidget");

            if (_uiWidgetType == null)
                throw new TypeLoadException("Could not find UIWidget.");

            _getWgosByObjId = RequireMethod(
                _worldMapType,
                "GetWorldGameObjectsByObjId",
                AllStatic,
                new[] { typeof(string) });

            _addInteractionEvent = RequireMethod(
                _worldGameObjectType,
                "AddInteractionEvent",
                AllInstance,
                new[] { typeof(string) });

            _customInteractionEvents = RequireField(
                _worldGameObjectType,
                "custom_interaction_events",
                AllInstance);

            _mainGameMe = RequireField(
                _mainGameType,
                "me",
                AllStatic);

            _mainGameSave = RequireField(
                _mainGameType,
                "save",
                AllInstance);

            _mainGameWorldRoot = RequireField(
                _mainGameType,
                "world_root",
                AllInstance);

            _generateBody = RequireMethod(
                _gameSaveType,
                "GenerateBody",
                AllInstance,
                new[]
                {
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(int)
                });

            _drop = RequireMethod(
                _dropType,
                "Drop",
                AllStatic,
                new[]
                {
                    typeof(Vector3),
                    _itemType,
                    typeof(Transform),
                    _directionType,
                    typeof(float),
                    typeof(int),
                    typeof(bool),
                    typeof(bool)
                });

            _dropCollected = RequireField(
                _dropType,
                "is_collected",
                AllInstance);

            _destroyLinkedHint = RequireMethod(
                _dropType,
                "DestroyLinkedHint",
                AllInstance,
                Type.EmptyTypes);

            _dropsListMe = RequireProperty(
                _dropsListType,
                "me",
                AllStatic);

            _drops = RequireField(
                _dropsListType,
                "drops",
                AllInstance);

            _guiElementsMe = RequireProperty(
                _guiElementsType,
                "me",
                AllStatic);

            _guiBodyArrived = RequireField(
                _guiElementsType,
                "body_arrived_gui",
                AllInstance);

            _bodyArrivedDisplay = RequireMethod(
                _newBodyArrivedGuiType,
                "Display",
                AllInstance,
                Type.EmptyTypes);

            _playSound = RequireMethod(
                _soundsType,
                "PlaySound",
                AllStatic,
                new[] { typeof(string), typeof(Vector2?), typeof(bool), typeof(float) });

            _uiWidgetAlpha = _uiWidgetType.GetProperty(
                "alpha",
                AllInstance);

            if (_uiWidgetAlpha == null || !_uiWidgetAlpha.CanWrite)
                throw new MissingMemberException("UIWidget", "alpha");

            _uiWidgetWidth = _uiWidgetType.GetProperty(
                "width",
                AllInstance);

            if (_uiWidgetWidth == null)
                throw new MissingMemberException("UIWidget", "width");
        }

        private bool TryBindProduction()
        {
            BaseUnityPlugin[] plugins =
                Resources.FindObjectsOfTypeAll<BaseUnityPlugin>();

            BaseUnityPlugin plugin = plugins.FirstOrDefault(p =>
                p != null
                && string.Equals(
                    p.GetType().FullName,
                    "KeepersAlerts.Plugin",
                    StringComparison.Ordinal));

            if (plugin == null)
            {
                _status = "Keeper's Alerts production DLL not found.";
                return false;
            }

            _production = plugin;
            _productionType = plugin.GetType();

            _corpseActive =
                RequireField(_productionType, "_corpseActive", AllInstance);
            _confessionActive =
                RequireField(_productionType, "_confessionActive", AllInstance);
            _corpseIndicator =
                RequireField(_productionType, "_corpseIndicator", AllInstance);
            _confessionIndicator =
                RequireField(_productionType, "_confessionIndicator", AllInstance);
            _confessionToast =
                RequireField(_productionType, "_confessionToast", AllInstance);

            _renderHud = RequireMethod(
                _productionType,
                "RenderHud",
                AllInstance,
                Type.EmptyTypes);

            _playCue = RequireMethod(
                _productionType,
                "PlayConfessionCue",
                AllInstance,
                Type.EmptyTypes);

            _showToast = RequireMethod(
                _productionType,
                "ShowConfessionToast",
                AllInstance,
                Type.EmptyTypes);

            _resyncConfession = RequireMethod(
                _productionType,
                "ResyncConfession",
                AllInstance,
                new[] { typeof(bool), typeof(string) });

            _resyncCorpse = RequireMethod(
                _productionType,
                "ResyncCorpse",
                AllInstance,
                new[] { typeof(string) });

            _tryGetCorpseCorridor = RequireMethod(
                _productionType,
                "TryGetCorpseCorridor",
                AllInstance,
                new[]
                {
                    typeof(Vector3).MakeByRefType(),
                    typeof(bool).MakeByRefType()
                });

            _isBodyDrop = _productionType.GetMethod(
                "IsBodyDrop",
                AllInstance);

            if (_isBodyDrop == null)
                throw new MissingMethodException(_productionType.FullName, "IsBodyDrop");

            _status = "Bound to Keeper's Alerts " + ReadProductionVersion() + ".";
            return true;
        }

        private string ReadProductionVersion()
        {
            FieldInfo version =
                _productionType.GetField(
                    "PluginVersion",
                    BindingFlags.Public | BindingFlags.Static);

            object value =
                version == null ? null : version.GetValue(null);

            return value == null ? "<unknown>" : value.ToString();
        }

        private void OnGUI()
        {
            if (!_visible)
                return;

            _window = GUI.Window(
                0x4B415443,
                _window,
                DrawWindow,
                "Keeper's Alerts — Test Console 0.1.1");
        }

        private void DrawWindow(int id)
        {
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("PRESENTATION PREVIEW");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Corpse HUD ON", GUILayout.Height(30f)))
                ForceState(_corpseActive, true, "Corpse HUD forced ON.");
            if (GUILayout.Button("Corpse HUD OFF", GUILayout.Height(30f)))
                ForceState(_corpseActive, false, "Corpse HUD forced OFF.");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Confession HUD ON", GUILayout.Height(30f)))
                ForceState(
                    _confessionActive,
                    true,
                    "Confession HUD forced ON.");
            if (GUILayout.Button("Confession HUD OFF", GUILayout.Height(30f)))
                ForceState(
                    _confessionActive,
                    false,
                    "Confession HUD forced OFF.");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview confession cue", GUILayout.Height(30f)))
                InvokePresentationCue();
            if (GUILayout.Button("Preview stock corpse cue", GUILayout.Height(30f)))
                PreviewStockCorpseCue();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Preview BOTH cues", GUILayout.Height(30f)))
                PreviewBothCues();

            bool holdNext = GUILayout.Toggle(
                _holdConfessionToast,
                "Hold confession transient on screen");

            if (holdNext != _holdConfessionToast)
            {
                _holdConfessionToast = holdNext;
                if (_holdConfessionToast)
                {
                    MaintainHeldConfessionToast();
                    _status = "Confession transient held for calibration.";
                }
                else
                {
                    HideConfessionToast();
                    _status = "Confession transient hold released.";
                }
            }

            if (GUILayout.Button("Restore native HUD state", GUILayout.Height(30f)))
                RestoreNativeState();

            GUILayout.Space(8f);
            GUILayout.Label("NATIVE TEST STATES — DO NOT SAVE WHILE ACTIVE");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add test confession", GUILayout.Height(30f)))
                AddNativeConfession();
            if (GUILayout.Button("Clear console confession", GUILayout.Height(30f)))
                ClearConsoleCreatedConfession();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn test corpse", GUILayout.Height(30f)))
                SpawnTestCorpse();
            if (GUILayout.Button("Remove console corpse", GUILayout.Height(30f)))
                RemoveTestCorpse();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CLEAR ALL confessions", GUILayout.Height(30f)))
                ClearAllConfessions();
            if (GUILayout.Button("CLEAR corpses in alert zone", GUILayout.Height(30f)))
                ClearAllCorpsesInAlertZone();
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "CLEAR ALL actions are destructive test helpers. Avoid saving test state.");

            GUILayout.Space(10f);
            GUILayout.Label("UI CALIBRATION — CURRENT WORKING BASELINE");

            if (!_calibrationCaptured)
                TryCaptureCalibration();

            if (_calibrationCaptured)
            {
                float firstSlotOffset = SliderRow(
                    "First slot offset", _firstSlotOffset, 0f, 80f);
                float slotStep = SliderRow(
                    "Slot spacing", _slotStep, 10f, 80f);
                float corpseY = SliderRow(
                    "Corpse Y", _corpseY, -40f, 40f);
                float corpseS = SliderRow(
                    "Corpse scale", _corpseScale, 0.20f, 2f);

                float confessionY = SliderRow(
                    "Confession Y", _confessionY, -40f, 40f);
                float confessionS = SliderRow(
                    "Confession scale", _confessionScale, 0.20f, 2f);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset Persistent", GUILayout.Height(28f)))
                    ResetPersistent();
                if (GUILayout.Button("Reset All UI", GUILayout.Height(28f)))
                    ResetAllCalibration();
                GUILayout.EndHorizontal();

                GUILayout.Space(6f);
                GUILayout.Label("CONFESSION TRANSIENT");

                float panelX = SliderRow(
                    "Panel X", _toastPanelX, -160f, 160f);
                float panelScale = SliderRow(
                    "Panel scale", _toastPanelScale, 0.50f, 2.50f);
                float toastX = SliderRow(
                    "Icon X", _toastIconX, -80f, 80f);
                float toastY = SliderRow(
                    "Icon Y", _toastIconY, -80f, 80f);
                float toastS = SliderRow(
                    "Icon scale", _toastIconScale, 0.25f, 4f);
                float visibleY = SliderRow(
                    "Visible Y", _toastVisibleY, 0f, 220f);
                float backgroundAlpha = SliderRow(
                    "Background alpha",
                    _toastBackgroundAlpha,
                    0f,
                    1f);

                bool changed =
                    !Approximately(firstSlotOffset, _firstSlotOffset)
                    || !Approximately(slotStep, _slotStep)
                    || !Approximately(corpseY, _corpseY)
                    || !Approximately(corpseS, _corpseScale)
                    || !Approximately(confessionY, _confessionY)
                    || !Approximately(confessionS, _confessionScale)
                    || !Approximately(panelX, _toastPanelX)
                    || !Approximately(panelScale, _toastPanelScale)
                    || !Approximately(toastX, _toastIconX)
                    || !Approximately(toastY, _toastIconY)
                    || !Approximately(toastS, _toastIconScale)
                    || !Approximately(visibleY, _toastVisibleY)
                    || !Approximately(backgroundAlpha, _toastBackgroundAlpha);

                _firstSlotOffset = firstSlotOffset;
                _slotStep = slotStep;
                _corpseY = corpseY;
                _corpseScale = corpseS;
                _confessionY = confessionY;
                _confessionScale = confessionS;
                _toastPanelX = panelX;
                _toastPanelScale = panelScale;
                _toastIconX = toastX;
                _toastIconY = toastY;
                _toastIconScale = toastS;
                _toastVisibleY = visibleY;
                _toastBackgroundAlpha = backgroundAlpha;

                if (changed)
                    ApplyCalibration();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset Transient", GUILayout.Height(28f)))
                    ResetTransient();
                if (GUILayout.Button("Log exact UI values", GUILayout.Height(28f)))
                    LogCalibration();
                GUILayout.EndHorizontal();

                GUILayout.Label(
                    "Baseline: offset 24.92; spacing 30.68; corpse Y -6.25 / 0.62; confession Y -1.88 / 0.95.");
            }
            else
            {
                GUILayout.Label(
                    "UI objects are not ready yet. Load into normal gameplay.");
            }

            GUILayout.Space(10f);
            GUILayout.Label("STATUS");
            GUILayout.TextArea(_status, GUILayout.Height(52f));
            GUILayout.Label("F9: show / hide");

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static float SliderRow(
            string label,
            float value,
            float min,
            float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(165f));
            float next =
                GUILayout.HorizontalSlider(
                    value,
                    min,
                    max,
                    GUILayout.Width(345f));
            GUILayout.Label(next.ToString("0.00"), GUILayout.Width(72f));
            GUILayout.EndHorizontal();
            return next;
        }

        private static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.001f;
        }

        private bool EnsureProduction()
        {
            return _production != null || TryBindProduction();
        }

        private void ForceState(
            FieldInfo field,
            bool value,
            string status)
        {
            try
            {
                if (!EnsureProduction())
                    return;

                field.SetValue(_production, value);
                _renderHud.Invoke(_production, null);
                _status = status;
            }
            catch (Exception ex)
            {
                Fail("force HUD state", ex);
            }
        }

        private void InvokePresentationCue()
        {
            try
            {
                if (!EnsureProduction())
                    return;

                _playCue.Invoke(_production, null);
                _showToast.Invoke(_production, null);

                if (_holdConfessionToast)
                    MaintainHeldConfessionToast();

                _status = "Played bell_single + confession transient.";
            }
            catch (Exception ex)
            {
                Fail("confession cue", ex);
            }
        }

        private void PreviewStockCorpseCue()
        {
            try
            {
                PlayStockCorpseCue();
                _status =
                    "Played stock donkey_bell + stock corpse-arrival visual.";
            }
            catch (Exception ex)
            {
                Fail("stock corpse cue", ex);
            }
        }

        private void PreviewBothCues()
        {
            try
            {
                if (!EnsureProduction())
                    return;

                PlayStockCorpseCue();
                _playCue.Invoke(_production, null);
                _showToast.Invoke(_production, null);

                if (_holdConfessionToast)
                    MaintainHeldConfessionToast();

                _status =
                    "Played stock corpse cue and confession cue together.";
            }
            catch (Exception ex)
            {
                Fail("combined cue preview", ex);
            }
        }

        private void PlayStockCorpseCue()
        {
            object gui = _guiElementsMe.GetValue(null, null);
            if (IsUnityNull(gui))
                throw new InvalidOperationException("GUIElements.me is unavailable.");

            Component stock =
                _guiBodyArrived.GetValue(gui) as Component;

            if (stock == null)
                throw new InvalidOperationException(
                    "Stock body-arrival GUI is unavailable.");

            _playSound.Invoke(
                null,
                new object[]
                {
                    DonkeySound,
                    null,
                    true,
                    0f
                });

            _bodyArrivedDisplay.Invoke(stock, null);
        }

        private void RestoreNativeState()
        {
            try
            {
                if (!EnsureProduction())
                    return;

                _resyncConfession.Invoke(
                    _production,
                    new object[] { false, "TestConsole.Restore" });

                _resyncCorpse.Invoke(
                    _production,
                    new object[] { "TestConsole.Restore" });

                _status = "HUD restored from native game state.";
            }
            catch (Exception ex)
            {
                Fail("restore native state", ex);
            }
        }

        private void AddNativeConfession()
        {
            try
            {
                object booth = FindFirstWgo(Confessional1);
                if (booth == null)
                {
                    _status = "church_budka_1 is not loaded.";
                    return;
                }

                IList events =
                    _customInteractionEvents.GetValue(booth) as IList;

                if (events != null && events.Contains(ConfessionEvent))
                {
                    _status = "Confession already exists naturally; unchanged.";
                    return;
                }

                _addInteractionEvent.Invoke(
                    booth,
                    new object[] { ConfessionEvent });

                _testConfessional = booth;
                _createdNativeConfession = true;
                _status =
                    "Native confession_available added. Do not save until cleared.";
            }
            catch (Exception ex)
            {
                Fail("add native confession", ex);
            }
        }

        private void ClearConsoleCreatedConfession()
        {
            try
            {
                if (!_createdNativeConfession || IsUnityNull(_testConfessional))
                {
                    _createdNativeConfession = false;
                    _testConfessional = null;
                    _status = "No console-created confession is active.";
                    return;
                }

                IList events =
                    _customInteractionEvents.GetValue(
                        _testConfessional) as IList;

                if (events != null)
                {
                    while (events.Contains(ConfessionEvent))
                        events.Remove(ConfessionEvent);
                }

                _createdNativeConfession = false;
                _testConfessional = null;

                if (EnsureProduction())
                {
                    _resyncConfession.Invoke(
                        _production,
                        new object[] { false, "TestConsole.ClearOwnedConfession" });
                }

                _status =
                    "Console-created confession cleared without forcing RedrawBubble.";
            }
            catch (Exception ex)
            {
                Fail("clear console confession", ex);
            }
        }

        private void ClearAllConfessions()
        {
            try
            {
                int removed = 0;

                removed += RemoveConfessionFromAll(Confessional1);
                removed += RemoveConfessionFromAll(Confessional2);

                _createdNativeConfession = false;
                _testConfessional = null;

                if (EnsureProduction())
                {
                    _resyncConfession.Invoke(
                        _production,
                        new object[] { false, "TestConsole.ClearAllConfessions" });
                }

                _status =
                    "Removed confession_available entries: "
                    + removed
                    + ". This is a destructive test action.";
            }
            catch (Exception ex)
            {
                Fail("clear all confessions", ex);
            }
        }

        private int RemoveConfessionFromAll(string objId)
        {
            int removed = 0;
            IEnumerable result =
                _getWgosByObjId.Invoke(
                    null,
                    new object[] { objId }) as IEnumerable;

            if (result == null)
                return 0;

            foreach (object booth in result)
            {
                if (IsUnityNull(booth))
                    continue;

                IList events =
                    _customInteractionEvents.GetValue(booth) as IList;

                if (events == null)
                    continue;

                while (events.Contains(ConfessionEvent))
                {
                    events.Remove(ConfessionEvent);
                    removed++;
                }
            }

            return removed;
        }

        private object FindFirstWgo(string objId)
        {
            IEnumerable result =
                _getWgosByObjId.Invoke(
                    null,
                    new object[] { objId }) as IEnumerable;

            if (result == null)
                return null;

            foreach (object item in result)
            {
                if (!IsUnityNull(item))
                    return item;
            }

            return null;
        }

        private void SpawnTestCorpse()
        {
            try
            {
                if (!EnsureProduction())
                    return;

                if (!IsUnityNull(_testCorpse))
                {
                    _status = "A console-created test corpse already exists.";
                    return;
                }

                object[] corridorArgs =
                {
                    Vector3.zero,
                    false
                };

                bool haveCorridor =
                    Convert.ToBoolean(
                        _tryGetCorpseCorridor.Invoke(
                            _production,
                            corridorArgs));

                if (!haveCorridor)
                {
                    _status = "Production corpse corridor is unavailable.";
                    return;
                }

                Vector3 origin = (Vector3)corridorArgs[0];
                bool directionUp = (bool)corridorArgs[1];

                object mainGame = _mainGameMe.GetValue(null);
                if (IsUnityNull(mainGame))
                {
                    _status = "MainGame.me is unavailable.";
                    return;
                }

                object save = _mainGameSave.GetValue(mainGame);
                Transform worldRoot =
                    _mainGameWorldRoot.GetValue(mainGame) as Transform;

                if (save == null || worldRoot == null)
                {
                    _status = "Save/world root is unavailable.";
                    return;
                }

                object body =
                    _generateBody.Invoke(
                        save,
                        new object[] { 0, 100, -1, -1 });

                if (body == null)
                {
                    _status = "GenerateBody returned null.";
                    return;
                }

                object direction =
                    Enum.Parse(
                        _directionType,
                        directionUp ? "Up" : "Down");

                object drop =
                    _drop.Invoke(
                        null,
                        new object[]
                        {
                            origin,
                            body,
                            worldRoot,
                            direction,
                            3f,
                            -1,
                            true,
                            false
                        });

                if (IsUnityNull(drop))
                {
                    _status = "Native Drop returned null.";
                    return;
                }

                _testCorpse = drop;
                _status =
                    "Native test corpse spawned in receiving corridor. DO NOT SAVE.";
            }
            catch (Exception ex)
            {
                Fail("spawn test corpse", ex);
            }
        }

        private void RemoveTestCorpse()
        {
            try
            {
                if (IsUnityNull(_testCorpse))
                {
                    _testCorpse = null;
                    return;
                }

                RemoveDropObject(_testCorpse);
                _testCorpse = null;

                if (EnsureProduction())
                {
                    _resyncCorpse.Invoke(
                        _production,
                        new object[] { "TestConsole.RemoveOwnedDrop" });
                }

                _status = "Console-created test corpse removed.";
            }
            catch (Exception ex)
            {
                Fail("remove test corpse", ex);
            }
        }

        private void ClearAllCorpsesInAlertZone()
        {
            try
            {
                if (!EnsureProduction())
                    return;

                object[] corridorArgs =
                {
                    Vector3.zero,
                    false
                };

                bool haveCorridor =
                    Convert.ToBoolean(
                        _tryGetCorpseCorridor.Invoke(
                            _production,
                            corridorArgs));

                if (!haveCorridor)
                {
                    _status = "Production corpse corridor is unavailable.";
                    return;
                }

                Vector3 origin = (Vector3)corridorArgs[0];
                bool directionUp = (bool)corridorArgs[1];

                float halfWidth =
                    ReadProductionConstFloat("CorridorHalfWidth");
                float backward =
                    ReadProductionConstFloat("CorridorBackward");
                float forwardLimit =
                    ReadProductionConstFloat("CorridorForward");

                object owner = _dropsListMe.GetValue(null, null);
                IList drops =
                    owner == null
                        ? null
                        : _drops.GetValue(owner) as IList;

                if (drops == null)
                {
                    _status = "DropsList is unavailable.";
                    return;
                }

                var toRemove = new List<object>();

                for (int i = 0; i < drops.Count; i++)
                {
                    object drop = drops[i];
                    if (IsUnityNull(drop))
                        continue;

                    bool isBody =
                        Convert.ToBoolean(
                            _isBodyDrop.Invoke(
                                _production,
                                new[] { drop }));

                    if (!isBody)
                        continue;

                    Component component = drop as Component;
                    if (component == null)
                        continue;

                    Vector3 delta =
                        component.transform.position - origin;

                    float lateral = Mathf.Abs(delta.x);
                    float along =
                        directionUp ? delta.y : -delta.y;

                    if (lateral <= halfWidth
                        && along >= -backward
                        && along <= forwardLimit)
                    {
                        toRemove.Add(drop);
                    }
                }

                foreach (object drop in toRemove)
                    RemoveDropObject(drop);

                if (!IsUnityNull(_testCorpse)
                    && toRemove.Contains(_testCorpse))
                {
                    _testCorpse = null;
                }

                _resyncCorpse.Invoke(
                    _production,
                    new object[] { "TestConsole.ClearAlertZone" });

                _status =
                    "Removed bodies from Keeper's Alerts corridor: "
                    + toRemove.Count
                    + ". This is a destructive test action.";
            }
            catch (Exception ex)
            {
                Fail("clear corpses in alert zone", ex);
            }
        }

        private void RemoveDropObject(object drop)
        {
            if (IsUnityNull(drop))
                return;

            _dropCollected.SetValue(drop, true);
            _destroyLinkedHint.Invoke(drop, null);

            object owner =
                _dropsListMe.GetValue(null, null);

            IList drops =
                owner == null
                    ? null
                    : _drops.GetValue(owner) as IList;

            if (drops != null && drops.Contains(drop))
                drops.Remove(drop);

            Component component = drop as Component;
            if (component != null && component.gameObject != null)
                UnityEngine.Object.Destroy(component.gameObject);
        }

        private float ReadProductionConstFloat(string fieldName)
        {
            FieldInfo field =
                _productionType.GetField(
                    fieldName,
                    BindingFlags.NonPublic | BindingFlags.Static);

            if (field == null)
            {
                throw new MissingFieldException(
                    _productionType.FullName,
                    fieldName);
            }

            return Convert.ToSingle(field.GetRawConstantValue());
        }

        private bool TryCaptureCalibration()
        {
            try
            {
                if (!EnsureProduction())
                    return false;

                object gui = _guiElementsMe.GetValue(null, null);
                if (IsUnityNull(gui)
                    || IsUnityNull(_guiBodyArrived.GetValue(gui)))
                {
                    return false;
                }

                GameObject corpse =
                    _corpseIndicator.GetValue(_production) as GameObject;
                GameObject confession =
                    _confessionIndicator.GetValue(_production) as GameObject;
                GameObject toast =
                    _confessionToast.GetValue(_production) as GameObject;

                if (corpse == null || confession == null || toast == null)
                    return false;

                Transform toastIcon =
                    FindDescendant(toast.transform, "PrayerIcon")
                    ?? FindDescendant(toast.transform, "PlusText");
                Transform background =
                    FindDescendant(toast.transform, "Background");

                if (toastIcon == null || background == null)
                    return false;

                Component toastController =
                    toast.GetComponent(_newBodyArrivedGuiType);

                if (toastController == null)
                    return false;

                FieldInfo visiblePointY =
                    toastController.GetType().GetField(
                        "_visible_point_y",
                        AllInstance);

                if (visiblePointY == null)
                    return false;

                Component backgroundWidget =
                    background.GetComponent(_uiWidgetType);

                if (backgroundWidget == null)
                    return false;

                _baselineToastPanelX =
                    toast.transform.localPosition.x;
                _baselineToastPanelScale =
                    toast.transform.localScale.x;
                _baselineToastBackgroundAlpha =
                    Convert.ToSingle(
                        _uiWidgetAlpha.GetValue(
                            backgroundWidget,
                            null));

                _firstSlotOffset = BaselineFirstSlotOffset;
                _slotStep = BaselineSlotStep;
                _corpseY = BaselineCorpseY;
                _corpseScale = BaselineCorpseScale;

                _confessionY = BaselineConfessionY;
                _confessionScale = BaselineConfessionScale;

                _toastPanelX = _baselineToastPanelX;
                _toastPanelScale = _baselineToastPanelScale;
                _toastIconX = BaselineToastIconX;
                _toastIconY = BaselineToastIconY;
                _toastIconScale = BaselineToastIconScale;
                _toastVisibleY = BaselineToastVisibleY;
                _toastBackgroundAlpha =
                    _baselineToastBackgroundAlpha;

                _calibrationCaptured = true;
                ApplyCalibration();

                _status =
                    "Loaded current working UI baseline. Reset buttons return here.";
                return true;
            }
            catch (Exception ex)
            {
                Fail("capture UI calibration", ex);
                return false;
            }
        }

        private void ApplyCalibration()
        {
            try
            {
                if (!EnsureProduction())
                    return;

                GameObject corpse =
                    _corpseIndicator.GetValue(_production) as GameObject;
                GameObject confession =
                    _confessionIndicator.GetValue(_production) as GameObject;
                GameObject toast =
                    _confessionToast.GetValue(_production) as GameObject;

                Transform energyBar =
                    corpse != null
                        ? corpse.transform.parent
                        : confession != null
                            ? confession.transform.parent
                            : null;

                if (energyBar != null)
                {
                    Component energyWidget =
                        energyBar.GetComponent(_uiWidgetType);

                    if (energyWidget != null)
                    {
                        float barWidth =
                            Convert.ToSingle(
                                _uiWidgetWidth.GetValue(
                                    energyWidget,
                                    null));
                        float x =
                            barWidth + _firstSlotOffset;

                        bool corpseOn =
                            Convert.ToBoolean(
                                _corpseActive.GetValue(_production));
                        bool confessionOn =
                            Convert.ToBoolean(
                                _confessionActive.GetValue(_production));

                        if (corpse != null)
                        {
                            SetUniformScale(
                                corpse.transform,
                                _corpseScale);

                            if (corpseOn)
                            {
                                SetXY(
                                    corpse.transform,
                                    x,
                                    _corpseY);
                                x += _slotStep;
                            }
                        }

                        if (confession != null)
                        {
                            SetUniformScale(
                                confession.transform,
                                _confessionScale);

                            if (confessionOn)
                            {
                                SetXY(
                                    confession.transform,
                                    x,
                                    _confessionY);
                            }
                        }
                    }
                }

                if (toast != null)
                {
                    Vector3 panelPosition =
                        toast.transform.localPosition;
                    panelPosition.x = _toastPanelX;
                    toast.transform.localPosition = panelPosition;
                    SetUniformScale(
                        toast.transform,
                        _toastPanelScale);

                    Transform toastIcon =
                        FindDescendant(
                            toast.transform,
                            "PrayerIcon")
                        ?? FindDescendant(
                            toast.transform,
                            "PlusText");

                    if (toastIcon != null)
                    {
                        SetXY(
                            toastIcon,
                            _toastIconX,
                            _toastIconY);
                        SetUniformScale(
                            toastIcon,
                            _toastIconScale);
                    }

                    Transform background =
                        FindDescendant(
                            toast.transform,
                            "Background");

                    if (background != null)
                    {
                        Component widget =
                            background.GetComponent(
                                _uiWidgetType);

                        if (widget != null)
                        {
                            _uiWidgetAlpha.SetValue(
                                widget,
                                _toastBackgroundAlpha,
                                null);
                        }
                    }

                    Component toastController =
                        toast.GetComponent(
                            _newBodyArrivedGuiType);

                    if (toastController != null)
                    {
                        FieldInfo visiblePointY =
                            toastController.GetType().GetField(
                                "_visible_point_y",
                                AllInstance);

                        if (visiblePointY != null)
                        {
                            visiblePointY.SetValue(
                                toastController,
                                _toastVisibleY);
                        }
                    }
                }

                if (_holdConfessionToast)
                    MaintainHeldConfessionToast();
            }
            catch (Exception ex)
            {
                Fail("apply UI calibration", ex);
            }
        }

        private void MaintainHeldConfessionToast()
        {
            try
            {
                if (!EnsureProduction())
                    return;

                GameObject toast =
                    _confessionToast.GetValue(
                        _production) as GameObject;

                if (toast == null)
                    return;

                Transform parent =
                    toast.transform.parent;

                if (parent != null
                    && !parent.gameObject.activeSelf)
                {
                    parent.gameObject.SetActive(true);
                }

                if (!toast.activeSelf)
                    toast.SetActive(true);

                Vector3 p =
                    toast.transform.localPosition;
                p.x = _toastPanelX;
                p.y =
                    (float)Screen.height / 4f
                    - _toastVisibleY;
                toast.transform.localPosition = p;

                SetUniformScale(
                    toast.transform,
                    _toastPanelScale);
            }
            catch (Exception ex)
            {
                Fail("hold confession transient", ex);
                _holdConfessionToast = false;
            }
        }

        private void HideConfessionToast()
        {
            try
            {
                if (!EnsureProduction())
                    return;

                GameObject toast =
                    _confessionToast.GetValue(
                        _production) as GameObject;

                if (toast != null)
                    toast.SetActive(false);
            }
            catch (Exception ex)
            {
                Fail("hide confession transient", ex);
            }
        }

        private void ResetPersistent()
        {
            if (!_calibrationCaptured)
                return;

            _firstSlotOffset = BaselineFirstSlotOffset;
            _slotStep = BaselineSlotStep;
            _corpseY = BaselineCorpseY;
            _corpseScale = BaselineCorpseScale;
            _confessionY = BaselineConfessionY;
            _confessionScale = BaselineConfessionScale;

            ApplyCalibration();
            _status =
                "Persistent HUD reset to current working baseline.";
        }

        private void ResetTransient()
        {
            if (!_calibrationCaptured)
                return;

            _toastPanelX = _baselineToastPanelX;
            _toastPanelScale = _baselineToastPanelScale;
            _toastIconX = BaselineToastIconX;
            _toastIconY = BaselineToastIconY;
            _toastIconScale = BaselineToastIconScale;
            _toastVisibleY = BaselineToastVisibleY;
            _toastBackgroundAlpha =
                _baselineToastBackgroundAlpha;

            ApplyCalibration();
            _status =
                "Confession transient reset to current working baseline.";
        }

        private void ResetAllCalibration()
        {
            ResetPersistent();
            ResetTransient();
            _status =
                "All UI values reset to current working baseline.";
        }

        private void LogCalibration()
        {
            string line =
                "UI_CALIBRATION "
                + "persistent=("
                + "offset=" + _firstSlotOffset.ToString("0.00")
                + ",step=" + _slotStep.ToString("0.00")
                + ",corpseY=" + _corpseY.ToString("0.00")
                + ",corpseScale=" + _corpseScale.ToString("0.00")
                + ",confessionY=" + _confessionY.ToString("0.00")
                + ",confessionScale=" + _confessionScale.ToString("0.00")
                + ") "
                + "transientPanel=("
                + _toastPanelX.ToString("0.00") + ","
                + _toastPanelScale.ToString("0.00") + ") "
                + "transientIcon=("
                + _toastIconX.ToString("0.00") + ","
                + _toastIconY.ToString("0.00") + ","
                + _toastIconScale.ToString("0.00") + ") "
                + "transientVisibleY="
                + _toastVisibleY.ToString("0.00") + " "
                + "backgroundAlpha="
                + _toastBackgroundAlpha.ToString("0.00");

            Logger.LogInfo(line);
            _status = line;
        }

        private static void SetXY(
            Transform transform,
            float x,
            float y)
        {
            Vector3 p =
                transform.localPosition;
            p.x = x;
            p.y = y;
            transform.localPosition = p;
        }

        private static void SetUniformScale(
            Transform transform,
            float scale)
        {
            transform.localScale =
                new Vector3(scale, scale, 1f);
        }

        private static Transform FindDescendant(
            Transform root,
            string exactName)
        {
            if (root == null)
                return null;

            if (string.Equals(
                root.name,
                exactName,
                StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found =
                    FindDescendant(
                        root.GetChild(i),
                        exactName);

                if (found != null)
                    return found;
            }

            return null;
        }

        private Type RequireGameType(string name)
        {
            Type type =
                _game.GetType(name, false)
                ?? FindTypeAcrossAssemblies(name);

            if (type == null)
            {
                throw new TypeLoadException(
                    "Could not find game type '" + name + "'.");
            }

            return type;
        }

        private static Type FindTypeAcrossAssemblies(
            string fullOrSimpleName)
        {
            foreach (Assembly assembly
                in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type direct =
                    assembly.GetType(
                        fullOrSimpleName,
                        false);

                if (direct != null)
                    return direct;

                try
                {
                    Type match =
                        assembly.GetTypes()
                            .FirstOrDefault(t =>
                                string.Equals(
                                    t.Name,
                                    fullOrSimpleName,
                                    StringComparison.Ordinal));

                    if (match != null)
                        return match;
                }
                catch (ReflectionTypeLoadException)
                {
                }
            }

            return null;
        }

        private static FieldInfo RequireField(
            Type type,
            string name,
            BindingFlags flags)
        {
            FieldInfo field =
                type.GetField(name, flags);

            if (field == null)
            {
                throw new MissingFieldException(
                    type.FullName,
                    name);
            }

            return field;
        }

        private static PropertyInfo RequireProperty(
            Type type,
            string name,
            BindingFlags flags)
        {
            PropertyInfo property =
                type.GetProperty(name, flags);

            if (property == null)
            {
                throw new MissingMemberException(
                    type.FullName,
                    name);
            }

            return property;
        }

        private static MethodInfo RequireMethod(
            Type type,
            string name,
            BindingFlags flags,
            Type[] parameterTypes)
        {
            MethodInfo method =
                type.GetMethod(
                    name,
                    flags,
                    null,
                    parameterTypes,
                    null);

            if (method == null)
            {
                throw new MissingMethodException(
                    type.FullName,
                    name + "("
                    + string.Join(
                        ",",
                        parameterTypes
                            .Select(t => t.Name)
                            .ToArray())
                    + ")");
            }

            return method;
        }

        private static bool IsUnityNull(object value)
        {
            if (ReferenceEquals(value, null))
                return true;

            UnityEngine.Object unityObject =
                value as UnityEngine.Object;

            return !ReferenceEquals(unityObject, null)
                && unityObject == null;
        }

        private void Fail(
            string stage,
            Exception ex)
        {
            _status = "FAILED: " + stage;
            Logger.LogError(
                "TEST_CONSOLE stage="
                + stage
                + " error="
                + ex);
        }
    }
}
