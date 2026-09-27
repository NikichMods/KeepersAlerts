// SPDX-License-Identifier: MPL-2.0
using System;
using System.Collections;
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
        private const string PluginVersion = "0.1.0";

        private const string ConfessionEvent = "confession_available";
        private const string ConfessionalId = "church_budka_1";

        private static readonly Guid SupportedGameMvid =
            new Guid("6f50b8e7-156b-49ac-bbe8-7505894b2364");

        private const BindingFlags AllInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AllStatic =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private Rect _window = new Rect(20f, 150f, 620f, 710f);
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

        private Assembly _game;
        private Type _worldMapType;
        private Type _worldGameObjectType;
        private Type _mainGameType;
        private Type _gameSaveType;
        private Type _itemType;
        private Type _dropType;
        private Type _directionType;
        private Type _dropsListType;

        private MethodInfo _getWgosByObjId;
        private MethodInfo _addInteractionEvent;
        private MethodInfo _redrawBubble;
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

        private object _testConfessional;
        private bool _createdNativeConfession;
        private object _testCorpse;

        private bool _calibrationCaptured;
        private float _corpseX;
        private float _corpseY;
        private float _corpseScale;
        private float _confessionX;
        private float _confessionY;
        private float _confessionScale;
        private float _toastIconX;
        private float _toastIconY;
        private float _toastIconScale;
        private float _toastVisibleY;

        private float _defaultCorpseX;
        private float _defaultCorpseY;
        private float _defaultCorpseScale;
        private float _defaultConfessionX;
        private float _defaultConfessionY;
        private float _defaultConfessionScale;
        private float _defaultToastIconX;
        private float _defaultToastIconY;
        private float _defaultToastIconScale;
        private float _defaultToastVisibleY;

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
                TryCaptureCalibration(false);
        }

        private void OnDestroy()
        {
            try
            {
                ClearNativeConfession();
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

            _redrawBubble = RequireMethod(
                _worldGameObjectType,
                "RedrawBubble",
                AllInstance,
                new[] { typeof(bool?) });

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

            _corpseActive = RequireField(
                _productionType,
                "_corpseActive",
                AllInstance);

            _confessionActive = RequireField(
                _productionType,
                "_confessionActive",
                AllInstance);

            _corpseIndicator = RequireField(
                _productionType,
                "_corpseIndicator",
                AllInstance);

            _confessionIndicator = RequireField(
                _productionType,
                "_confessionIndicator",
                AllInstance);

            _confessionToast = RequireField(
                _productionType,
                "_confessionToast",
                AllInstance);

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
                new[] { typeof(Vector3).MakeByRefType(), typeof(bool).MakeByRefType() });

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
                "Keeper's Alerts — Test Console");
        }

        private void DrawWindow(int id)
        {
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("EVENT / STATE TESTING");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Corpse HUD ON", GUILayout.Height(32f)))
                ForceState(_corpseActive, true, "Corpse HUD forced ON.");
            if (GUILayout.Button("Corpse HUD OFF", GUILayout.Height(32f)))
                ForceState(_corpseActive, false, "Corpse HUD forced OFF.");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Confession HUD ON", GUILayout.Height(32f)))
                ForceState(_confessionActive, true, "Confession HUD forced ON.");
            if (GUILayout.Button("Confession HUD OFF", GUILayout.Height(32f)))
                ForceState(_confessionActive, false, "Confession HUD forced OFF.");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play confession cue", GUILayout.Height(32f)))
                InvokePresentationCue();
            if (GUILayout.Button("Restore native state", GUILayout.Height(32f)))
                RestoreNativeState();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("NATIVE OBSERVER PATHS");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add test confession", GUILayout.Height(32f)))
                AddNativeConfession();
            if (GUILayout.Button("Clear test confession", GUILayout.Height(32f)))
                ClearNativeConfession();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn test corpse", GUILayout.Height(32f)))
                SpawnTestCorpse();
            if (GUILayout.Button("Remove test corpse", GUILayout.Height(32f)))
                RemoveTestCorpse();
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Do NOT save while a native test confession or test corpse exists.");

            GUILayout.Space(10f);
            GUILayout.Label("UI CALIBRATION");

            bool hadCalibration = _calibrationCaptured;
            if (!hadCalibration)
                TryCaptureCalibration(false);

            if (_calibrationCaptured)
            {
                float corpseX = SliderRow("Corpse X", _corpseX, -120f, 300f);
                float corpseY = SliderRow("Corpse Y", _corpseY, -180f, 100f);
                float corpseS = SliderRow("Corpse scale", _corpseScale, 0.25f, 4f);

                float confessionX =
                    SliderRow("Confession X", _confessionX, -120f, 300f);
                float confessionY =
                    SliderRow("Confession Y", _confessionY, -180f, 100f);
                float confessionS =
                    SliderRow("Confession scale", _confessionScale, 0.25f, 4f);

                float toastX =
                    SliderRow("Transient icon X", _toastIconX, -80f, 80f);
                float toastY =
                    SliderRow("Transient icon Y", _toastIconY, -80f, 80f);
                float toastS =
                    SliderRow("Transient icon scale", _toastIconScale, 0.25f, 4f);
                float visibleY =
                    SliderRow("Transient visible Y", _toastVisibleY, 20f, 220f);

                bool changed =
                    !Approximately(corpseX, _corpseX)
                    || !Approximately(corpseY, _corpseY)
                    || !Approximately(corpseS, _corpseScale)
                    || !Approximately(confessionX, _confessionX)
                    || !Approximately(confessionY, _confessionY)
                    || !Approximately(confessionS, _confessionScale)
                    || !Approximately(toastX, _toastIconX)
                    || !Approximately(toastY, _toastIconY)
                    || !Approximately(toastS, _toastIconScale)
                    || !Approximately(visibleY, _toastVisibleY);

                _corpseX = corpseX;
                _corpseY = corpseY;
                _corpseScale = corpseS;
                _confessionX = confessionX;
                _confessionY = confessionY;
                _confessionScale = confessionS;
                _toastIconX = toastX;
                _toastIconY = toastY;
                _toastIconScale = toastS;
                _toastVisibleY = visibleY;

                if (changed)
                    ApplyCalibration();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset UI values", GUILayout.Height(32f)))
                    ResetCalibration();
                if (GUILayout.Button("Log exact UI values", GUILayout.Height(32f)))
                    LogCalibration();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label(
                    "UI objects are not ready yet. Load into normal gameplay.");
            }

            GUILayout.Space(10f);
            GUILayout.Label("STATUS");
            GUILayout.TextArea(_status, GUILayout.Height(48f));
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
            GUILayout.Label(label, GUILayout.Width(150f));
            float next =
                GUILayout.HorizontalSlider(
                    value,
                    min,
                    max,
                    GUILayout.Width(330f));
            GUILayout.Label(next.ToString("0.00"), GUILayout.Width(70f));
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
                _status = "Played bell_single + confession transient.";
            }
            catch (Exception ex)
            {
                Fail("confession cue", ex);
            }
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
                IEnumerable result =
                    _getWgosByObjId.Invoke(
                        null,
                        new object[] { ConfessionalId }) as IEnumerable;

                if (result == null)
                {
                    _status = "Confessional list unavailable.";
                    return;
                }

                object booth = null;
                foreach (object item in result)
                {
                    if (item != null)
                    {
                        booth = item;
                        break;
                    }
                }

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

        private void ClearNativeConfession()
        {
            try
            {
                if (!_createdNativeConfession || _testConfessional == null)
                    return;

                IList events =
                    _customInteractionEvents.GetValue(
                        _testConfessional) as IList;

                if (events != null && events.Contains(ConfessionEvent))
                    events.Remove(ConfessionEvent);

                _redrawBubble.Invoke(
                    _testConfessional,
                    new object[] { null });

                _createdNativeConfession = false;
                _testConfessional = null;
                _status = "Console-created confession cleared.";
            }
            catch (Exception ex)
            {
                Fail("clear native confession", ex);
            }
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

                _dropCollected.SetValue(_testCorpse, true);

                _destroyLinkedHint.Invoke(
                    _testCorpse,
                    null);

                object dropsOwner =
                    _dropsListMe.GetValue(null, null);

                IList drops =
                    dropsOwner == null
                        ? null
                        : _drops.GetValue(dropsOwner) as IList;

                if (drops != null && drops.Contains(_testCorpse))
                    drops.Remove(_testCorpse);

                Component component = _testCorpse as Component;
                if (component != null && component.gameObject != null)
                    UnityEngine.Object.Destroy(component.gameObject);

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

        private bool TryCaptureCalibration(bool force)
        {
            try
            {
                if (!EnsureProduction())
                    return false;

                _renderHud.Invoke(_production, null);

                GameObject corpse =
                    _corpseIndicator.GetValue(_production) as GameObject;
                GameObject confession =
                    _confessionIndicator.GetValue(_production) as GameObject;
                GameObject toast =
                    _confessionToast.GetValue(_production) as GameObject;

                if (corpse == null || confession == null || toast == null)
                    return false;

                Transform toastIcon =
                    FindDescendant(toast.transform, "PlusText");

                Component toastController =
                    toast.GetComponent(
                        _game.GetType("NewBodyArrivedGUI", true));

                if (toastIcon == null || toastController == null)
                    return false;

                FieldInfo visiblePointY =
                    toastController.GetType().GetField(
                        "_visible_point_y",
                        AllInstance);

                if (visiblePointY == null)
                    return false;

                if (_calibrationCaptured && !force)
                    return true;

                _corpseX = corpse.transform.localPosition.x;
                _corpseY = corpse.transform.localPosition.y;
                _corpseScale = corpse.transform.localScale.x;

                _confessionX = confession.transform.localPosition.x;
                _confessionY = confession.transform.localPosition.y;
                _confessionScale = confession.transform.localScale.x;

                _toastIconX = toastIcon.localPosition.x;
                _toastIconY = toastIcon.localPosition.y;
                _toastIconScale = toastIcon.localScale.x;
                _toastVisibleY =
                    Convert.ToSingle(
                        visiblePointY.GetValue(toastController));

                _defaultCorpseX = _corpseX;
                _defaultCorpseY = _corpseY;
                _defaultCorpseScale = _corpseScale;
                _defaultConfessionX = _confessionX;
                _defaultConfessionY = _confessionY;
                _defaultConfessionScale = _confessionScale;
                _defaultToastIconX = _toastIconX;
                _defaultToastIconY = _toastIconY;
                _defaultToastIconScale = _toastIconScale;
                _defaultToastVisibleY = _toastVisibleY;

                _calibrationCaptured = true;
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

                if (corpse != null)
                {
                    SetXY(corpse.transform, _corpseX, _corpseY);
                    SetUniformScale(corpse.transform, _corpseScale);
                }

                if (confession != null)
                {
                    SetXY(confession.transform, _confessionX, _confessionY);
                    SetUniformScale(confession.transform, _confessionScale);
                }

                if (toast != null)
                {
                    Transform toastIcon =
                        FindDescendant(toast.transform, "PlusText");

                    if (toastIcon != null)
                    {
                        SetXY(toastIcon, _toastIconX, _toastIconY);
                        SetUniformScale(toastIcon, _toastIconScale);
                    }

                    Component toastController =
                        toast.GetComponent(
                            _game.GetType("NewBodyArrivedGUI", true));

                    if (toastController != null)
                    {
                        FieldInfo visiblePointY =
                            toastController.GetType().GetField(
                                "_visible_point_y",
                                AllInstance);

                        if (visiblePointY != null)
                            visiblePointY.SetValue(
                                toastController,
                                _toastVisibleY);
                    }
                }
            }
            catch (Exception ex)
            {
                Fail("apply UI calibration", ex);
            }
        }

        private void ResetCalibration()
        {
            if (!_calibrationCaptured)
                return;

            _corpseX = _defaultCorpseX;
            _corpseY = _defaultCorpseY;
            _corpseScale = _defaultCorpseScale;
            _confessionX = _defaultConfessionX;
            _confessionY = _defaultConfessionY;
            _confessionScale = _defaultConfessionScale;
            _toastIconX = _defaultToastIconX;
            _toastIconY = _defaultToastIconY;
            _toastIconScale = _defaultToastIconScale;
            _toastVisibleY = _defaultToastVisibleY;

            ApplyCalibration();
            _status = "UI calibration reset to candidate defaults.";
        }

        private void LogCalibration()
        {
            string line =
                "UI_CALIBRATION "
                + "corpse=("
                + _corpseX.ToString("0.00") + ","
                + _corpseY.ToString("0.00") + ","
                + _corpseScale.ToString("0.00") + ") "
                + "confession=("
                + _confessionX.ToString("0.00") + ","
                + _confessionY.ToString("0.00") + ","
                + _confessionScale.ToString("0.00") + ") "
                + "transientIcon=("
                + _toastIconX.ToString("0.00") + ","
                + _toastIconY.ToString("0.00") + ","
                + _toastIconScale.ToString("0.00") + ") "
                + "transientVisibleY="
                + _toastVisibleY.ToString("0.00");

            Logger.LogInfo(line);
            _status = line;
        }

        private static void SetXY(
            Transform transform,
            float x,
            float y)
        {
            Vector3 p = transform.localPosition;
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
            Type type = _game.GetType(name, false);
            if (type == null)
                throw new TypeLoadException("Could not find game type '" + name + "'.");
            return type;
        }

        private static FieldInfo RequireField(
            Type type,
            string name,
            BindingFlags flags)
        {
            FieldInfo field = type.GetField(name, flags);
            if (field == null)
                throw new MissingFieldException(type.FullName, name);
            return field;
        }

        private static PropertyInfo RequireProperty(
            Type type,
            string name,
            BindingFlags flags)
        {
            PropertyInfo property = type.GetProperty(name, flags);
            if (property == null)
                throw new MissingMemberException(type.FullName, name);
            return property;
        }

        private static MethodInfo RequireMethod(
            Type type,
            string name,
            BindingFlags flags,
            Type[] parameterTypes)
        {
            MethodInfo method = type.GetMethod(
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
                        parameterTypes.Select(t => t.Name).ToArray())
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

        private void Fail(string stage, Exception ex)
        {
            _status = "FAILED: " + stage;
            Logger.LogError(
                "TEST_CONSOLE stage=" + stage + " error=" + ex);
        }
    }
}
