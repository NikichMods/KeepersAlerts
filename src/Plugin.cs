// SPDX-License-Identifier: MPL-2.0
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace KeepersAlerts
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "nikichmods.keepersalerts";
        public const string PluginName = "Keeper's Alerts";
        public const string PluginVersion = "1.0.0";

        private static readonly Guid SupportedGameMvid =
            new Guid("6f50b8e7-156b-49ac-bbe8-7505894b2364");

        private const BindingFlags AllInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AllStatic =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private const string ConfessionEvent = "confession_available";
        private const string Confessional1 = "church_budka_1";
        private const string Confessional2 = "church_budka_2";
        private const string CorpseBodyId = "body";
        private const string RepairedDropTag = "morgue_throw_out";
        private const string PreRepairDropPoint = "donkey_cemetery_point";
        private const string ConfessionSound = "bell_single";
        private const string PraySymbol = "(pray_bubble)";
        private const string BodySymbol = "(body)";

        private const float CorridorHalfWidth = 96f;
        private const float CorridorBackward = 48f;
        private const float CorridorForward = 544f;

        // Accepted 2560x1440 / HUD-scale 1.1 calibration, expressed in
        // energy-bar-local NGUI coordinates so the layout follows the host HUD.
        private const float IndicatorFirstSlotOffset = 24.92f;
        private const float IndicatorSlotStep = 30.68f;
        private const float CorpseIndicatorY = -6.25f;
        private const float CorpseIndicatorScale = 0.62f;
        private const float ConfessionIndicatorY = -1.88f;
        private const float ConfessionIndicatorScale = 0.95f;

        private const float ConfessionToastIconX = 8.89f;
        private const float ConfessionToastIconY = 14.06f;
        private const float ConfessionToastIconScale = 1.67f;

        internal static Plugin Instance;

        private Harmony _harmony;
        private Assembly _gameAssembly;

        private Type _worldGameObjectType;
        private Type _dropsListType;
        private Type _dropResGameObjectType;
        private Type _mainGameType;
        private Type _hudType;
        private Type _guiElementsType;
        private Type _worldMapType;
        private Type _soundsType;
        private Type _newBodyArrivedGuiType;
        private Type _uiWidgetType;

        private PropertyInfo _dropsListMeProperty;
        private FieldInfo _dropsField;
        private FieldInfo _dropResField;
        private FieldInfo _dropCollectedField;
        private FieldInfo _wgoObjIdField;
        private FieldInfo _wgoInteractionEventsField;
        private PropertyInfo _guiElementsMeProperty;
        private FieldInfo _guiHudField;
        private FieldInfo _guiBodyArrivedField;
        private FieldInfo _hudBarEnergyField;

        private MethodInfo _getWgosByObjId;
        private MethodInfo _getWgosByCustomTag;
        private MethodInfo _wgoIsDisabled;
        private MethodInfo _getGdPointByTag;
        private MethodInfo _playSound;
        private MethodInfo _bodyArrivedDisplay;

        private bool _presentationArmed;
        private bool _worldReady;
        private bool _confessionActive;
        private bool _corpseActive;
        private bool _presentationWarningLogged;
        private bool _stateWarningLogged;

        private GameObject _corpseIndicator;
        private GameObject _confessionIndicator;
        private GameObject _confessionToast;
        private Component _confessionToastController;
        private Component _energyBar;

        private void Awake()
        {
            Instance = this;

            try
            {
                BindRuntime();
                PatchRuntime();

                Logger.LogInfo(
                    PluginName + " " + PluginVersion
                    + " loaded for verified Graveyard Keeper 1.407 host build.");
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    PluginName + " initialization failed; no patches remain active. " + ex);

                try
                {
                    if (_harmony != null)
                        _harmony.UnpatchSelf();
                }
                catch
                {
                }

                enabled = false;
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (_harmony != null)
                    _harmony.UnpatchSelf();
            }
            catch
            {
            }

            DestroyOwnedObject(_corpseIndicator);
            DestroyOwnedObject(_confessionIndicator);
            DestroyOwnedObject(_confessionToast);

            _corpseIndicator = null;
            _confessionIndicator = null;
            _confessionToast = null;
            _confessionToastController = null;
            _energyBar = null;

            if (ReferenceEquals(Instance, this))
                Instance = null;
        }

        private void BindRuntime()
        {
            _gameAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a =>
                    string.Equals(
                        a.GetName().Name,
                        "Assembly-CSharp",
                        StringComparison.Ordinal));

            if (_gameAssembly == null)
                throw new InvalidOperationException("Assembly-CSharp is unavailable.");

            Guid actualMvid = _gameAssembly.ManifestModule.ModuleVersionId;
            if (actualMvid != SupportedGameMvid)
            {
                throw new InvalidOperationException(
                    "Unsupported Assembly-CSharp MVID " + actualMvid
                    + "; expected " + SupportedGameMvid + ".");
            }

            _worldGameObjectType = RequireGameType("WorldGameObject");
            _dropsListType = RequireGameType("DropsList");
            _dropResGameObjectType = RequireGameType("DropResGameObject");
            _mainGameType = RequireGameType("MainGame");
            _hudType = RequireGameType("HUD");
            _guiElementsType = RequireGameType("GUIElements");
            _worldMapType = RequireGameType("WorldMap");
            _soundsType = RequireGameType("Sounds");
            _newBodyArrivedGuiType = RequireGameType("NewBodyArrivedGUI");
            _uiWidgetType = RequireGameType("UIWidget");

            _dropsListMeProperty = RequireProperty(_dropsListType, "me", AllStatic);
            _dropsField = RequireField(_dropsListType, "drops", AllInstance);

            _dropResField = RequireField(_dropResGameObjectType, "res", AllInstance);
            _dropCollectedField =
                RequireField(_dropResGameObjectType, "is_collected", AllInstance);

            _wgoObjIdField =
                RequireField(_worldGameObjectType, "obj_id", AllInstance);
            _wgoInteractionEventsField =
                RequireField(
                    _worldGameObjectType,
                    "custom_interaction_events",
                    AllInstance);

            _guiElementsMeProperty =
                RequireProperty(_guiElementsType, "me", AllStatic);
            _guiHudField =
                RequireField(_guiElementsType, "hud", AllInstance);
            _guiBodyArrivedField =
                RequireField(_guiElementsType, "body_arrived_gui", AllInstance);
            _hudBarEnergyField =
                RequireField(_hudType, "bar_energy", AllInstance);

            _getWgosByObjId = RequireMethod(
                _worldMapType,
                "GetWorldGameObjectsByObjId",
                AllStatic,
                new[] { typeof(string) });

            _getWgosByCustomTag = RequireMethod(
                _worldMapType,
                "GetWorldGameObjectsByCustomTag",
                AllStatic,
                new[] { typeof(string), typeof(bool) });

            _wgoIsDisabled = RequireMethod(
                _worldGameObjectType,
                "IsDisabled",
                AllInstance,
                Type.EmptyTypes);

            _getGdPointByTag = RequireMethod(
                _worldMapType,
                "GetGDPointByGDTag",
                AllStatic,
                new[] { typeof(string), typeof(bool), typeof(bool) });

            _playSound = RequireMethod(
                _soundsType,
                "PlaySound",
                AllStatic,
                new[] { typeof(string), typeof(Vector2?), typeof(bool), typeof(float) });

            _bodyArrivedDisplay = RequireMethod(
                _newBodyArrivedGuiType,
                "Display",
                AllInstance,
                Type.EmptyTypes);
        }

        private void PatchRuntime()
        {
            _harmony = new Harmony(PluginGuid);

            MethodInfo redrawBubble = RequireMethod(
                _worldGameObjectType,
                "RedrawBubble",
                AllInstance,
                new[] { typeof(bool?) });

            MethodInfo addDrop = _dropsListType
                .GetMethods(AllInstance)
                .SingleOrDefault(m =>
                {
                    if (!string.Equals(m.Name, "Add", StringComparison.Ordinal))
                        return false;

                    ParameterInfo[] p = m.GetParameters();
                    return p.Length == 1
                        && p[0].ParameterType == _dropResGameObjectType
                        && m.ReturnType == typeof(bool);
                });

            if (addDrop == null)
                throw new MissingMethodException("DropsList.Add(DropResGameObject)");

            MethodInfo destroyLinkedHint = RequireMethod(
                _dropResGameObjectType,
                "DestroyLinkedHint",
                AllInstance,
                Type.EmptyTypes);

            MethodInfo fromGameSave = _dropsListType
                .GetMethods(AllInstance)
                .SingleOrDefault(m =>
                    string.Equals(m.Name, "FromGameSave", StringComparison.Ordinal)
                    && m.GetParameters().Length == 1);

            if (fromGameSave == null)
                throw new MissingMethodException("DropsList.FromGameSave(GameSave)");

            MethodInfo onGameStarted = RequireMethod(
                _mainGameType,
                "OnGameStartedPlaying",
                AllInstance,
                Type.EmptyTypes);

            MethodInfo hudOpen = RequireMethod(
                _hudType,
                "Open",
                AllInstance,
                Type.EmptyTypes);

            MethodInfo runAppearCoroutine = RequireMethod(
                _newBodyArrivedGuiType,
                "RunAppearCoroutine",
                AllInstance,
                Type.EmptyTypes);

            MethodInfo runAppearMoveNext =
                AccessTools.EnumeratorMoveNext(runAppearCoroutine);

            if (runAppearMoveNext == null)
            {
                throw new MissingMethodException(
                    "NewBodyArrivedGUI.RunAppearCoroutine state-machine MoveNext");
            }

            _harmony.Patch(
                redrawBubble,
                postfix: new HarmonyMethod(
                    typeof(Plugin).GetMethod(
                        nameof(RedrawBubblePostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)));

            _harmony.Patch(
                addDrop,
                postfix: new HarmonyMethod(
                    typeof(Plugin).GetMethod(
                        nameof(DropsListAddPostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)));

            _harmony.Patch(
                destroyLinkedHint,
                postfix: new HarmonyMethod(
                    typeof(Plugin).GetMethod(
                        nameof(DestroyLinkedHintPostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)));

            _harmony.Patch(
                fromGameSave,
                prefix: new HarmonyMethod(
                    typeof(Plugin).GetMethod(
                        nameof(FromGameSavePrefix),
                        BindingFlags.Static | BindingFlags.NonPublic)));

            _harmony.Patch(
                onGameStarted,
                postfix: new HarmonyMethod(
                    typeof(Plugin).GetMethod(
                        nameof(OnGameStartedPlayingPostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)));

            _harmony.Patch(
                hudOpen,
                postfix: new HarmonyMethod(
                    typeof(Plugin).GetMethod(
                        nameof(HudOpenPostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)));

            _harmony.Patch(
                runAppearMoveNext,
                transpiler: new HarmonyMethod(
                    typeof(Plugin).GetMethod(
                        nameof(NewBodyArrivedTimingTranspiler),
                        BindingFlags.Static | BindingFlags.NonPublic)));
        }

        private static IEnumerable<CodeInstruction>
            NewBodyArrivedTimingTranspiler(
                IEnumerable<CodeInstruction> instructions)
        {
            ConstructorInfo waitForSecondsCtor =
                AccessTools.Constructor(
                    typeof(WaitForSeconds),
                    new[] { typeof(float) });

            ConstructorInfo waitForSecondsRealtimeCtor =
                AccessTools.Constructor(
                    typeof(WaitForSecondsRealtime),
                    new[] { typeof(float) });

            if (waitForSecondsCtor == null
                || waitForSecondsRealtimeCtor == null)
            {
                throw new MissingMethodException(
                    "Unity wait instruction constructor required for real-time transient timing.");
            }

            Type tweenSettingsExtensions =
                AccessTools.TypeByName(
                    "DG.Tweening.TweenSettingsExtensions");

            Type tweenerType =
                AccessTools.TypeByName(
                    "DG.Tweening.Tweener");

            if (tweenSettingsExtensions == null
                || tweenerType == null)
            {
                throw new TypeLoadException(
                    "DOTween types required for real-time transient timing are unavailable.");
            }

            MethodInfo setUpdateDefinition =
                tweenSettingsExtensions
                    .GetMethods(AllStatic)
                    .SingleOrDefault(m =>
                    {
                        if (!string.Equals(
                            m.Name,
                            "SetUpdate",
                            StringComparison.Ordinal)
                            || !m.IsGenericMethodDefinition)
                        {
                            return false;
                        }

                        ParameterInfo[] p = m.GetParameters();
                        return p.Length == 2
                            && p[1].ParameterType == typeof(bool);
                    });

            if (setUpdateDefinition == null)
            {
                throw new MissingMethodException(
                    "DG.Tweening.TweenSettingsExtensions.SetUpdate<T>(T, bool)");
            }

            MethodInfo setUpdate =
                setUpdateDefinition.MakeGenericMethod(tweenerType);

            int tweenCount = 0;
            int waitCount = 0;

            foreach (CodeInstruction instruction in instructions)
            {
                MethodInfo calledMethod =
                    instruction.operand as MethodInfo;

                if (calledMethod != null
                    && string.Equals(
                        calledMethod.Name,
                        "DOLocalMoveY",
                        StringComparison.Ordinal)
                    && calledMethod.DeclaringType != null
                    && string.Equals(
                        calledMethod.DeclaringType.FullName,
                        "DG.Tweening.ShortcutExtensions",
                        StringComparison.Ordinal))
                {
                    yield return instruction;
                    yield return new CodeInstruction(
                        OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        setUpdate);
                    tweenCount++;
                    continue;
                }

                if (instruction.opcode == OpCodes.Newobj
                    && Equals(
                        instruction.operand,
                        waitForSecondsCtor))
                {
                    CodeInstruction replacement =
                        new CodeInstruction(instruction);

                    replacement.operand =
                        waitForSecondsRealtimeCtor;

                    yield return replacement;
                    waitCount++;
                    continue;
                }

                yield return instruction;
            }

            if (tweenCount != 2 || waitCount != 1)
            {
                throw new InvalidOperationException(
                    "Unexpected NewBodyArrivedGUI timing shape: DOLocalMoveY="
                    + tweenCount
                    + ", WaitForSeconds="
                    + waitCount
                    + "; expected 2 and 1.");
            }
        }

        private static void RedrawBubblePostfix(object __instance)
        {
            Plugin self = Instance;
            if (self == null || !self._worldReady || __instance == null)
                return;

            try
            {
                string objId = self._wgoObjIdField.GetValue(__instance) as string;
                if (!string.Equals(objId, Confessional1, StringComparison.Ordinal)
                    && !string.Equals(objId, Confessional2, StringComparison.Ordinal))
                {
                    return;
                }

                self.ResyncConfession(true, "RedrawBubble");
            }
            catch (Exception ex)
            {
                self.LogStateFailureOnce("confession RedrawBubble resync", ex);
            }
        }

        private static void DropsListAddPostfix(object __0, bool __result)
        {
            Plugin self = Instance;
            if (self == null || !self._worldReady || !__result || __0 == null)
                return;

            try
            {
                if (!self.IsBodyDrop(__0))
                    return;

                self.ResyncCorpse("DropsList.Add");
            }
            catch (Exception ex)
            {
                self.LogStateFailureOnce("corpse add resync", ex);
            }
        }

        private static void DestroyLinkedHintPostfix(object __instance)
        {
            Plugin self = Instance;
            if (self == null || !self._worldReady || __instance == null)
                return;

            try
            {
                if (!self.IsBodyDrop(__instance)
                    || !Convert.ToBoolean(
                        self._dropCollectedField.GetValue(__instance)))
                {
                    return;
                }

                self.ResyncCorpse("DestroyLinkedHint");
            }
            catch (Exception ex)
            {
                self.LogStateFailureOnce("corpse clear resync", ex);
            }
        }

        private static void FromGameSavePrefix()
        {
            Plugin self = Instance;
            if (self == null)
                return;

            self._presentationArmed = false;
            self._worldReady = false;
        }

        private static void OnGameStartedPlayingPostfix()
        {
            Plugin self = Instance;
            if (self == null)
                return;

            try
            {
                self._worldReady = true;
                self.EnsurePresentation();
                self.ResyncConfession(false, "OnGameStartedPlaying");
                self.ResyncCorpse("OnGameStartedPlaying");
                self._presentationArmed = true;

                self.Logger.LogInfo(
                    "Initial silent resync complete: confession="
                    + self._confessionActive
                    + ", corpse="
                    + self._corpseActive
                    + "; live transition presentation armed.");
            }
            catch (Exception ex)
            {
                self._presentationArmed = false;
                self.LogStateFailureOnce("initial post-load resync", ex);
            }
        }

        private static void HudOpenPostfix()
        {
            Plugin self = Instance;
            if (self == null || !self._worldReady)
                return;

            try
            {
                self.EnsureHudIndicators();
                self.RenderHud();
            }
            catch (Exception ex)
            {
                self.LogPresentationFailureOnce("HUD attach/refresh", ex);
            }
        }

        private void ResyncConfession(bool allowLiveCue, string reason)
        {
            bool next = ComputeConfessionState();
            bool previous = _confessionActive;

            _confessionActive = next;
            RenderHud();

            if (previous == next)
                return;

            Logger.LogInfo(
                "Confession state "
                + previous
                + " -> "
                + next
                + " via "
                + reason
                + ".");

            if (!previous
                && next
                && allowLiveCue
                && _presentationArmed)
            {
                PlayConfessionCue();
                ShowConfessionToast();
            }
        }

        private void ResyncCorpse(string reason)
        {
            bool next = ComputeCorpseState();
            bool previous = _corpseActive;

            _corpseActive = next;
            RenderHud();

            if (previous == next)
                return;

            Logger.LogInfo(
                "Corpse state "
                + previous
                + " -> "
                + next
                + " via "
                + reason
                + ".");
        }

        private bool ComputeConfessionState()
        {
            return AnyConfessionalAvailable(Confessional1)
                || AnyConfessionalAvailable(Confessional2);
        }

        private bool AnyConfessionalAvailable(string objId)
        {
            object result = _getWgosByObjId.Invoke(
                null,
                new object[] { objId });

            IEnumerable sequence = result as IEnumerable;
            if (sequence == null)
                return false;

            foreach (object wgo in sequence)
            {
                if (wgo == null)
                    continue;

                object events =
                    _wgoInteractionEventsField.GetValue(wgo);

                IEnumerable eventSequence = events as IEnumerable;
                if (eventSequence == null)
                    continue;

                foreach (object value in eventSequence)
                {
                    if (string.Equals(
                        value as string,
                        ConfessionEvent,
                        StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ComputeCorpseState()
        {
            object dropsList = _dropsListMeProperty.GetValue(null, null);
            if (IsUnityNull(dropsList))
                return false;

            object dropsValue = _dropsField.GetValue(dropsList);
            IEnumerable drops = dropsValue as IEnumerable;
            if (drops == null)
                return false;

            Vector3 origin;
            bool directionUp;
            if (!TryGetCorpseCorridor(out origin, out directionUp))
                return false;

            foreach (object drop in drops)
            {
                if (drop == null || !IsBodyDrop(drop))
                    continue;

                if (Convert.ToBoolean(_dropCollectedField.GetValue(drop)))
                    continue;

                Component component = drop as Component;
                if (component == null)
                    continue;

                Vector3 delta = component.transform.position - origin;
                float lateral = Mathf.Abs(delta.x);
                float forward = directionUp ? delta.y : -delta.y;

                if (lateral <= CorridorHalfWidth
                    && forward >= -CorridorBackward
                    && forward <= CorridorForward)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetCorpseCorridor(
            out Vector3 origin,
            out bool directionUp)
        {
            origin = Vector3.zero;
            directionUp = true;

            object repairedList = _getWgosByCustomTag.Invoke(
                null,
                new object[] { RepairedDropTag, false });

            IEnumerable repairedCandidates = repairedList as IEnumerable;
            if (repairedCandidates != null)
            {
                foreach (object repaired in repairedCandidates)
                {
                    if (IsUnityNull(repaired))
                        continue;

                    bool disabled = Convert.ToBoolean(
                        _wgoIsDisabled.Invoke(repaired, null));

                    if (disabled)
                        continue;

                    Component repairedComponent = repaired as Component;
                    if (repairedComponent == null)
                        continue;

                    origin = repairedComponent.transform.position;
                    directionUp = false;
                    return true;
                }
            }

            object gdPoint = _getGdPointByTag.Invoke(
                null,
                new object[] { PreRepairDropPoint, false, true });

            if (IsUnityNull(gdPoint))
                return false;

            Component pointComponent = gdPoint as Component;
            if (pointComponent == null)
                return false;

            origin = pointComponent.transform.position;
            directionUp = true;
            return true;
        }

        private bool IsBodyDrop(object drop)
        {
            if (drop == null
                || !_dropResGameObjectType.IsInstanceOfType(drop))
            {
                return false;
            }

            object item = _dropResField.GetValue(drop);
            if (item == null)
                return false;

            object id = GetMemberValue(item, "id");
            if (string.Equals(
                id as string,
                CorpseBodyId,
                StringComparison.Ordinal))
            {
                return true;
            }

            object definition = GetMemberValue(item, "definition");
            object type = GetMemberValue(definition, "type");
            return type != null
                && string.Equals(
                    type.ToString(),
                    "Body",
                    StringComparison.Ordinal);
        }

        private void PlayConfessionCue()
        {
            try
            {
                _playSound.Invoke(
                    null,
                    new object[]
                    {
                        ConfessionSound,
                        null,
                        true,
                        0f
                    });
            }
            catch (Exception ex)
            {
                LogPresentationFailureOnce("bell_single playback", ex);
            }
        }

        private void ShowConfessionToast()
        {
            try
            {
                EnsureConfessionToast();
                if (_confessionToastController == null)
                    return;

                _bodyArrivedDisplay.Invoke(
                    _confessionToastController,
                    null);
            }
            catch (Exception ex)
            {
                LogPresentationFailureOnce("confession transient", ex);
            }
        }

        private void EnsurePresentation()
        {
            EnsureHudIndicators();
            EnsureConfessionToast();
            RenderHud();
        }

        private void EnsureHudIndicators()
        {
            if (!IsUnityNull(_corpseIndicator)
                && !IsUnityNull(_confessionIndicator)
                && !IsUnityNull(_energyBar))
            {
                return;
            }

            object gui = _guiElementsMeProperty.GetValue(null, null);
            if (IsUnityNull(gui))
                throw new InvalidOperationException("GUIElements.me is unavailable.");

            Component hud =
                _guiHudField.GetValue(gui) as Component;

            Component bodyArrival =
                _guiBodyArrivedField.GetValue(gui) as Component;

            if (hud == null || bodyArrival == null)
            {
                throw new InvalidOperationException(
                    "Stock HUD or body-arrival GUI is unavailable.");
            }

            Component energyBar =
                _hudBarEnergyField.GetValue(hud) as Component;

            Transform bodyImage =
                FindDescendant(bodyArrival.transform, "BodyImage");

            Transform plusText =
                FindDescendant(bodyArrival.transform, "PlusText");

            if (energyBar == null
                || bodyImage == null
                || plusText == null)
            {
                throw new InvalidOperationException(
                    "Expected stock energy-bar/body-arrival presentation hierarchy is missing.");
            }

            Component energyWidget =
                energyBar.GetComponent(_uiWidgetType);

            if (energyWidget == null
                || Convert.ToSingle(GetMemberValue(energyWidget, "width")) <= 0f)
            {
                throw new InvalidOperationException(
                    "Stock energy-bar widget geometry is unavailable.");
            }

            _energyBar = energyBar;

            if (IsUnityNull(_corpseIndicator))
            {
                _corpseIndicator =
                    UnityEngine.Object.Instantiate(bodyImage.gameObject);

                _corpseIndicator.name =
                    "KeepersAlerts_CorpseIndicator";

                _corpseIndicator.transform.SetParent(
                    _energyBar.transform,
                    false);

                ClearWidgetAnchors(_corpseIndicator);
                SetWidgetDepth(_corpseIndicator, 80);
                _corpseIndicator.transform.localScale =
                    new Vector3(
                        CorpseIndicatorScale,
                        CorpseIndicatorScale,
                        1f);
                _corpseIndicator.SetActive(false);
            }

            if (IsUnityNull(_confessionIndicator))
            {
                _confessionIndicator =
                    UnityEngine.Object.Instantiate(plusText.gameObject);

                _confessionIndicator.name =
                    "KeepersAlerts_ConfessionIndicator";

                _confessionIndicator.transform.SetParent(
                    _energyBar.transform,
                    false);

                ClearWidgetAnchors(_confessionIndicator);
                SetWidgetDepth(_confessionIndicator, 80);
                SetLabelText(
                    _confessionIndicator,
                    PraySymbol);

                _confessionIndicator.transform.localScale =
                    new Vector3(
                        ConfessionIndicatorScale,
                        ConfessionIndicatorScale,
                        1f);
                _confessionIndicator.SetActive(false);
            }

            LayoutHudIndicators();
        }

        private void LayoutHudIndicators()
        {
            if (IsUnityNull(_energyBar))
                return;

            Component energyWidget =
                _energyBar.GetComponent(_uiWidgetType);

            if (energyWidget == null)
                return;

            float barWidth =
                Convert.ToSingle(
                    GetMemberValue(energyWidget, "width"));

            float x = barWidth + IndicatorFirstSlotOffset;

            if (_corpseActive && !IsUnityNull(_corpseIndicator))
            {
                SetLocalXY(
                    _corpseIndicator.transform,
                    x,
                    CorpseIndicatorY);
                x += IndicatorSlotStep;
            }

            if (_confessionActive && !IsUnityNull(_confessionIndicator))
            {
                SetLocalXY(
                    _confessionIndicator.transform,
                    x,
                    ConfessionIndicatorY);
            }
        }

        private void EnsureConfessionToast()
        {
            if (!IsUnityNull(_confessionToast)
                && _confessionToastController != null)
            {
                return;
            }

            object gui = _guiElementsMeProperty.GetValue(null, null);
            if (IsUnityNull(gui))
                return;

            Component stockController =
                _guiBodyArrivedField.GetValue(gui) as Component;

            if (stockController == null)
                return;

            GameObject stockPanel = stockController.gameObject;
            Transform parent = stockPanel.transform.parent;

            _confessionToast =
                UnityEngine.Object.Instantiate(
                    stockPanel,
                    parent,
                    false);

            _confessionToast.name =
                "KeepersAlerts_ConfessionArrivedPanel";

            Transform bodyImage =
                FindDescendant(
                    _confessionToast.transform,
                    "BodyImage");

            Transform plusText =
                FindDescendant(
                    _confessionToast.transform,
                    "PlusText");

            if (bodyImage != null)
                bodyImage.gameObject.SetActive(false);

            if (plusText != null)
            {
                GameObject prayerIcon =
                    UnityEngine.Object.Instantiate(
                        plusText.gameObject);

                prayerIcon.name = "PrayerIcon";
                prayerIcon.transform.SetParent(
                    _confessionToast.transform,
                    false);

                ClearWidgetAnchors(prayerIcon);
                SetLabelText(prayerIcon, PraySymbol);
                SetWidgetDepth(prayerIcon, 26);

                prayerIcon.transform.localPosition =
                    new Vector3(
                        ConfessionToastIconX,
                        ConfessionToastIconY,
                        0f);
                prayerIcon.transform.localScale =
                    new Vector3(
                        ConfessionToastIconScale,
                        ConfessionToastIconScale,
                        1f);
            }

            _confessionToastController =
                _confessionToast.GetComponent(
                    _newBodyArrivedGuiType);

            if (_confessionToastController == null)
            {
                DestroyOwnedObject(_confessionToast);
                _confessionToast = null;
                throw new InvalidOperationException(
                    "Cloned confession panel lost NewBodyArrivedGUI.");
            }

            _confessionToast.SetActive(false);
        }

        private void RenderHud()
        {
            try
            {
                EnsureHudIndicators();

                if (!IsUnityNull(_corpseIndicator))
                    _corpseIndicator.SetActive(_corpseActive);

                if (!IsUnityNull(_confessionIndicator))
                    _confessionIndicator.SetActive(_confessionActive);

                LayoutHudIndicators();
            }
            catch (Exception ex)
            {
                LogPresentationFailureOnce("persistent HUD render", ex);
            }
        }

        private void ClearWidgetAnchors(GameObject go)
        {
            if (go == null)
                return;

            Component widget = go.GetComponent(_uiWidgetType);
            if (widget == null)
                return;

            string[] anchorNames =
            {
                "leftAnchor",
                "rightAnchor",
                "bottomAnchor",
                "topAnchor"
            };

            foreach (string anchorName in anchorNames)
            {
                object anchor = GetMemberValue(widget, anchorName);
                if (anchor == null)
                    continue;

                SetMemberValue(anchor, "target", null);
            }
        }

        private void SetWidgetDepth(
            GameObject go,
            int depth)
        {
            if (go == null)
                return;

            Component widget = go.GetComponent(_uiWidgetType);
            if (widget == null)
                return;

            SetMemberValue(widget, "depth", depth);
        }

        private static void SetLabelText(
            GameObject go,
            string text)
        {
            if (go == null)
                return;

            Type labelType = AccessTools.TypeByName("UILabel");
            if (labelType == null)
                return;

            Component label = go.GetComponent(labelType);
            if (label == null)
                return;

            SetMemberValue(label, "text", text);
        }

        private static void SetLocalXY(
            Transform transform,
            float x,
            float y)
        {
            if (transform == null)
                return;

            Vector3 p = transform.localPosition;
            p.x = x;
            p.y = y;
            transform.localPosition = p;
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
            Type type = _gameAssembly.GetType(name, false)
                ?? AccessTools.TypeByName(name);

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

        private static object GetMemberValue(
            object instance,
            string name)
        {
            if (instance == null)
                return null;

            Type type = instance.GetType();

            PropertyInfo property =
                type.GetProperty(name, AllInstance);

            if (property != null)
                return property.GetValue(instance, null);

            FieldInfo field =
                type.GetField(name, AllInstance);

            return field == null
                ? null
                : field.GetValue(instance);
        }

        private static void SetMemberValue(
            object instance,
            string name,
            object value)
        {
            if (instance == null)
                return;

            Type type = instance.GetType();

            PropertyInfo property =
                type.GetProperty(name, AllInstance);

            if (property != null && property.CanWrite)
            {
                property.SetValue(instance, value, null);
                return;
            }

            FieldInfo field =
                type.GetField(name, AllInstance);

            if (field != null)
                field.SetValue(instance, value);
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

        private static void DestroyOwnedObject(GameObject go)
        {
            if (go != null)
                UnityEngine.Object.Destroy(go);
        }

        private void LogPresentationFailureOnce(
            string stage,
            Exception ex)
        {
            if (_presentationWarningLogged)
                return;

            _presentationWarningLogged = true;
            Logger.LogError(
                "Keeper's Alerts presentation failure at "
                + stage
                + "; state observation remains active. "
                + ex);
        }

        private void LogStateFailureOnce(
            string stage,
            Exception ex)
        {
            if (_stateWarningLogged)
                return;

            _stateWarningLogged = true;
            Logger.LogError(
                "Keeper's Alerts state observation failure at "
                + stage
                + ". "
                + ex);
        }
    }
}
