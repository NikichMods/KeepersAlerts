using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace KeepersAlerts
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.keepersalerts";
        public const string PluginName = "Keeper's Alerts";
        public const string PluginVersion = "0.1.0";

        private static readonly Guid SupportedGameMvid =
            new Guid("6f50b8e7-156b-49ac-bbe8-7505894b2364");

        private const float CorridorLateralHalfWidth = 96f;
        private const float CorridorBackwardAllowance = 48f;
        private const float CorridorForwardLength = 544f;

        private static readonly string[] ConfessionalIds =
        {
            "church_budka_1",
            "church_budka_2"
        };

        internal static Plugin Instance;

        private Harmony _harmony;

        private Type _worldGameObjectType;
        private Type _dropsListType;
        private Type _dropResGameObjectType;
        private Type _hudType;
        private Type _worldMapType;
        private Type _soundsType;

        private MethodInfo _getWgoByObjId;
        private MethodInfo _getWgoByCustomTag;
        private MethodInfo _getGdPointByTag;
        private MethodInfo _playSound;

        private HudPresentation _hudPresentation;
        private ConfessionTransientPresenter _transientPresenter;

        private bool _confessionInitialized;
        private bool _confessionAvailable;
        private bool _corpseInitialized;
        private bool _corpseWaiting;
        private bool _cuesArmed;
        private bool _pendingConfessionCue;
        private bool _runtimeFailed;

        private void Awake()
        {
            Instance = this;

            try
            {
                var gameAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(
                        a.GetName().Name,
                        "Assembly-CSharp",
                        StringComparison.Ordinal));

                if (gameAssembly == null)
                    throw new InvalidOperationException(
                        "Assembly-CSharp is not loaded.");

                var mvid = gameAssembly.ManifestModule.ModuleVersionId;
                if (mvid != SupportedGameMvid)
                {
                    throw new InvalidOperationException(
                        "Unsupported Assembly-CSharp MVID " + mvid);
                }

                ResolveHostContract();
                _hudPresentation = new HudPresentation();
                _transientPresenter = new ConfessionTransientPresenter(
                    this,
                    OnPresentationFailure);

                _harmony = new Harmony(PluginGuid);
                InstallPatches();

                Logger.LogInfo(
                    PluginName
                    + " "
                    + PluginVersion
                    + " loaded for Graveyard Keeper 1.407.");
            }
            catch (Exception ex)
            {
                DisableAfterFailure("initialization", ex);
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

            if (_transientPresenter != null)
                _transientPresenter.Dispose();
            if (_hudPresentation != null)
                _hudPresentation.Dispose();

            if (ReferenceEquals(Instance, this))
                Instance = null;
        }

        private void ResolveHostContract()
        {
            _worldGameObjectType = RequireType("WorldGameObject");
            _dropsListType = RequireType("DropsList");
            _dropResGameObjectType = RequireType("DropResGameObject");
            _hudType = RequireType("HUD");
            _worldMapType = RequireType("WorldMap");
            _soundsType = RequireType("Sounds");

            _getWgoByObjId = RequireMethod(
                _worldMapType,
                "GetWorldGameObjectByObjId",
                2,
                true);
            _getWgoByCustomTag = RequireMethod(
                _worldMapType,
                "GetWorldGameObjectByCustomTag",
                2,
                true);
            _getGdPointByTag = RequireMethod(
                _worldMapType,
                "GetGDPointByGDTag",
                3,
                true);
            _playSound = RequireMethod(
                _soundsType,
                "PlaySound",
                4,
                true);
        }

        private void InstallPatches()
        {
            Patch(
                RequireMethod(
                    _worldGameObjectType,
                    "RedrawBubble",
                    1,
                    false),
                null,
                nameof(RedrawBubblePostfix));

            Patch(
                RequireMethod(
                    _dropsListType,
                    "Add",
                    1,
                    false),
                null,
                nameof(DropsAddPostfix));

            Patch(
                RequireMethod(
                    _dropResGameObjectType,
                    "CollectDrop",
                    1,
                    false),
                null,
                nameof(CollectDropPostfix));

            Patch(
                RequireMethod(
                    _dropsListType,
                    "FromGameSave",
                    1,
                    false),
                nameof(FromGameSavePrefix),
                nameof(FromGameSavePostfix));

            Patch(
                RequireMethod(
                    _hudType,
                    "Open",
                    0,
                    false),
                null,
                nameof(HudOpenPostfix));
        }

        private void Patch(
            MethodInfo target,
            string prefixName,
            string postfixName)
        {
            HarmonyMethod prefix = null;
            HarmonyMethod postfix = null;

            if (!string.IsNullOrEmpty(prefixName))
            {
                var method = typeof(Plugin).GetMethod(
                    prefixName,
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (method == null)
                    throw new MissingMethodException(
                        typeof(Plugin).FullName,
                        prefixName);
                prefix = new HarmonyMethod(method);
            }

            if (!string.IsNullOrEmpty(postfixName))
            {
                var method = typeof(Plugin).GetMethod(
                    postfixName,
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (method == null)
                    throw new MissingMethodException(
                        typeof(Plugin).FullName,
                        postfixName);
                postfix = new HarmonyMethod(method);
            }

            _harmony.Patch(target, prefix, postfix);
        }

        private static void RedrawBubblePostfix(object __instance)
        {
            var self = Instance;
            if (self == null || self._runtimeFailed || __instance == null)
                return;

            try
            {
                var objId = ReflectionUtil.ReadString(__instance, "obj_id");
                if (!IsConfessionalId(objId))
                    return;

                self.ResyncConfession(true, "RedrawBubble");
            }
            catch (Exception ex)
            {
                self.DisableAfterFailure("confession state transition", ex);
            }
        }

        private static void DropsAddPostfix(object __0, bool __result)
        {
            var self = Instance;
            if (self == null
                || self._runtimeFailed
                || !__result
                || __0 == null)
            {
                return;
            }

            try
            {
                if (!self.IsBodyDrop(__0))
                    return;

                self.ResyncCorpse("DropsList.Add");
            }
            catch (Exception ex)
            {
                self.DisableAfterFailure("corpse add transition", ex);
            }
        }

        private static void CollectDropPostfix(object __instance)
        {
            var self = Instance;
            if (self == null
                || self._runtimeFailed
                || __instance == null)
            {
                return;
            }

            try
            {
                if (!self.IsBodyDrop(__instance))
                    return;

                self.ResyncCorpse("DropResGameObject.CollectDrop");
            }
            catch (Exception ex)
            {
                self.DisableAfterFailure("corpse pickup transition", ex);
            }
        }

        private static void FromGameSavePrefix()
        {
            var self = Instance;
            if (self == null || self._runtimeFailed)
                return;

            self._cuesArmed = false;
            self._pendingConfessionCue = false;
            self._confessionInitialized = false;
            self._corpseInitialized = false;
        }

        private static void FromGameSavePostfix()
        {
            var self = Instance;
            if (self == null || self._runtimeFailed)
                return;

            try
            {
                self.ResyncCorpse("DropsList.FromGameSave");
                self.ResyncConfession(false, "DropsList.FromGameSave");
            }
            catch (Exception ex)
            {
                self.DisableAfterFailure("save-load resync", ex);
            }
        }

        private static void HudOpenPostfix(object __instance)
        {
            var self = Instance;
            if (self == null
                || self._runtimeFailed
                || __instance == null)
            {
                return;
            }

            try
            {
                self.OnHudOpened(__instance);
            }
            catch (Exception ex)
            {
                self.DisableAfterFailure("HUD lifecycle", ex);
            }
        }

        private void OnHudOpened(object hud)
        {
            var attached = _hudPresentation != null
                && _hudPresentation.EnsureAttached(hud);

            if (!attached)
            {
                Logger.LogWarning(
                    "Keeper's Alerts could not attach its HUD indicators "
                    + "to the verified hud left owner.");
            }

            ResyncCorpse("HUD.Open");
            ResyncConfession(false, "HUD.Open");

            _cuesArmed = true;
            UpdatePersistentPresentation();

            if (_pendingConfessionCue && _confessionAvailable)
                EmitConfessionCue();

            if (attached)
                Logger.LogInfo("Keeper's Alerts HUD presentation attached.");
        }

        private void ResyncConfession(bool allowCue, string source)
        {
            var current = QueryConfessionAvailability();

            if (!_confessionInitialized)
            {
                _confessionInitialized = true;
                _confessionAvailable = current;
                UpdatePersistentPresentation();
                Logger.LogInfo(
                    "Confession state initialized: "
                    + current
                    + " source="
                    + source);
                return;
            }

            if (current == _confessionAvailable)
            {
                UpdatePersistentPresentation();
                return;
            }

            var previous = _confessionAvailable;
            _confessionAvailable = current;

            Logger.LogInfo(
                "Confession state "
                + previous
                + " -> "
                + current
                + " source="
                + source);

            if (!current)
            {
                _pendingConfessionCue = false;
            }
            else if (allowCue && _cuesArmed)
            {
                if (_hudPresentation != null
                    && _hudPresentation.IsHudVisible)
                {
                    EmitConfessionCue();
                }
                else
                {
                    _pendingConfessionCue = true;
                }
            }

            UpdatePersistentPresentation();
        }

        private bool QueryConfessionAvailability()
        {
            for (var i = 0; i < ConfessionalIds.Length; i++)
            {
                var wgo = _getWgoByObjId.Invoke(
                    null,
                    new object[] { ConfessionalIds[i], true });

                if (wgo == null)
                    continue;

                var events = ReflectionUtil.ReadEnumerable(
                    wgo,
                    "custom_interaction_events");
                if (events == null)
                    continue;

                foreach (var item in events)
                {
                    if (string.Equals(
                        item as string,
                        "confession_available",
                        StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void EmitConfessionCue()
        {
            if (!_confessionAvailable)
                return;

            _pendingConfessionCue = false;

            _playSound.Invoke(
                null,
                new object[]
                {
                    "bell_single",
                    null,
                    false,
                    0f
                });

            if (_transientPresenter != null)
                _transientPresenter.Show();

            Logger.LogInfo(
                "Confession notification cue emitted: bell_single.");
        }

        private void ResyncCorpse(string source)
        {
            var current = QueryCorpseWaiting();

            if (!_corpseInitialized)
            {
                _corpseInitialized = true;
                _corpseWaiting = current;
                UpdatePersistentPresentation();
                Logger.LogInfo(
                    "Corpse state initialized: "
                    + current
                    + " source="
                    + source);
                return;
            }

            if (current == _corpseWaiting)
            {
                UpdatePersistentPresentation();
                return;
            }

            var previous = _corpseWaiting;
            _corpseWaiting = current;

            Logger.LogInfo(
                "Corpse state "
                + previous
                + " -> "
                + current
                + " source="
                + source);

            UpdatePersistentPresentation();
        }

        private bool QueryCorpseWaiting()
        {
            object dropsList;
            if (!ReflectionUtil.TryReadStatic(
                    _dropsListType,
                    "me",
                    out dropsList)
                || dropsList == null)
            {
                return false;
            }

            var drops = ReflectionUtil.ReadEnumerable(dropsList, "drops");
            if (drops == null)
                return false;

            Vector3 origin;
            var repaired = TryGetRepairedDropOrigin(out origin);
            if (!repaired && !TryGetPreRepairDropOrigin(out origin))
                return false;

            foreach (var drop in drops)
            {
                if (drop == null
                    || !IsBodyDrop(drop)
                    || ReflectionUtil.ReadBool(drop, "is_collected"))
                {
                    continue;
                }

                var component = drop as Component;
                if (component == null)
                    continue;

                if (IsInsideDeliveryCorridor(
                    component.transform.position,
                    origin,
                    repaired))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetRepairedDropOrigin(out Vector3 origin)
        {
            origin = Vector3.zero;

            var wgo = _getWgoByCustomTag.Invoke(
                null,
                new object[] { "morgue_throw_out", true });
            var component = wgo as Component;
            if (component == null)
                return false;

            origin = component.transform.position;
            return true;
        }

        private bool TryGetPreRepairDropOrigin(out Vector3 origin)
        {
            origin = Vector3.zero;

            var point = _getGdPointByTag.Invoke(
                null,
                new object[]
                {
                    "donkey_cemetery_point",
                    false,
                    true
                });

            var component = point as Component;
            if (component == null)
                return false;

            origin = component.transform.position;
            return true;
        }

        private static bool IsInsideDeliveryCorridor(
            Vector3 position,
            Vector3 origin,
            bool repaired)
        {
            var dx = position.x - origin.x;
            if (Mathf.Abs(dx) > CorridorLateralHalfWidth)
                return false;

            var dy = position.y - origin.y;
            var forward = repaired ? -dy : dy;

            return forward >= -CorridorBackwardAllowance
                && forward <= CorridorForwardLength;
        }

        private bool IsBodyDrop(object drop)
        {
            object item;
            return ReflectionUtil.TryRead(drop, "res", out item)
                && item != null
                && string.Equals(
                    ReflectionUtil.ReadString(item, "id"),
                    "body",
                    StringComparison.Ordinal);
        }

        private void UpdatePersistentPresentation()
        {
            if (_hudPresentation == null)
                return;

            _hudPresentation.SetStates(
                _corpseInitialized && _corpseWaiting,
                _confessionInitialized && _confessionAvailable);
        }

        private void OnPresentationFailure(string stage, Exception ex)
        {
            DisableAfterFailure(stage, ex);
        }

        private void DisableAfterFailure(string stage, Exception ex)
        {
            if (_runtimeFailed)
                return;

            _runtimeFailed = true;

            Logger.LogError(
                "Keeper's Alerts disabled after "
                + stage
                + " failure: "
                + ex.GetType().Name
                + ": "
                + ex.Message);

            if (_transientPresenter != null)
                _transientPresenter.Dispose();
            if (_hudPresentation != null)
                _hudPresentation.Dispose();

            try
            {
                if (_harmony != null)
                    _harmony.UnpatchSelf();
            }
            catch
            {
            }
        }

        private static bool IsConfessionalId(string objId)
        {
            return string.Equals(
                    objId,
                    "church_budka_1",
                    StringComparison.Ordinal)
                || string.Equals(
                    objId,
                    "church_budka_2",
                    StringComparison.Ordinal);
        }

        private static Type RequireType(string name)
        {
            var type = ReflectionUtil.FindType(name);
            if (type == null)
                throw new TypeLoadException(
                    "Required host type was not found: " + name);
            return type;
        }

        private static MethodInfo RequireMethod(
            Type type,
            string name,
            int parameterCount,
            bool isStatic)
        {
            var method = ReflectionUtil.FindMethod(
                type,
                name,
                parameterCount,
                isStatic);

            if (method == null)
            {
                throw new MissingMethodException(
                    type == null ? "<null>" : type.FullName,
                    name + "/" + parameterCount);
            }

            return method;
        }
    }
}
