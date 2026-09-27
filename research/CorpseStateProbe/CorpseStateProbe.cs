// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using UnityEngine;

namespace KeepersAlertsResearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class CorpseStateProbe : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.gyk.keepersalerts.research.corpsestateprobe";
        public const string PluginName = "Keeper's Alerts Corpse State Probe";
        public const string PluginVersion = "0.1.0";

        private static readonly Guid SupportedGameMvid = new Guid("6f50b8e7-156b-49ac-bbe8-7505894b2364");
        private const float ScanInterval = 0.5f;
        private const float SnapshotInterval = 5f;

        private readonly Dictionary<int, DropInfo> _knownBodies = new Dictionary<int, DropInfo>();
        private Type _dropsListType;
        private Type _worldMapType;
        private object _dropsList;
        private float _nextScan;
        private float _nextSnapshot;
        private string _reportPath;
        private bool _ready;

        private void Awake()
        {
            _reportPath = Path.Combine(Paths.BepInExRootPath, "KeepersAlerts-corpse-state-probe-0.1.0.txt");

            try
            {
                Assembly game = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));

                if (game == null)
                    throw new InvalidOperationException("Assembly-CSharp is not loaded.");

                Guid mvid = game.ManifestModule.ModuleVersionId;
                if (mvid != SupportedGameMvid)
                    throw new InvalidOperationException("Unsupported Assembly-CSharp MVID " + mvid);

                _dropsListType = game.GetType("DropsList", false);
                _worldMapType = game.GetType("WorldMap", false);
                if (_dropsListType == null || _worldMapType == null)
                    throw new InvalidOperationException("Required host types were not found.");

                WriteHeader(mvid);
                _ready = true;
                Logger.LogInfo(PluginName + " " + PluginVersion + " loaded. Read-only native drop observer.");
            }
            catch (Exception ex)
            {
                Append("PROBE_ERROR stage=awake error=" + Quote(ex.ToString()));
                Logger.LogError(PluginName + " disabled: " + ex);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_ready || Time.realtimeSinceStartup < _nextScan)
                return;

            _nextScan = Time.realtimeSinceStartup + ScanInterval;

            try
            {
                if (!EnsureDropsList())
                    return;

                Dictionary<int, DropInfo> current = ReadBodyDrops();

                foreach (KeyValuePair<int, DropInfo> pair in current)
                {
                    if (_knownBodies.ContainsKey(pair.Key))
                        continue;

                    Append("BODY_ADDED " + pair.Value.ToFields());
                    AppendAnchorSnapshot("body_added");
                }

                foreach (KeyValuePair<int, DropInfo> pair in _knownBodies)
                {
                    if (current.ContainsKey(pair.Key))
                        continue;

                    Append("BODY_REMOVED " + pair.Value.ToFields());
                }

                _knownBodies.Clear();
                foreach (KeyValuePair<int, DropInfo> pair in current)
                    _knownBodies.Add(pair.Key, pair.Value);

                if (Time.realtimeSinceStartup >= _nextSnapshot)
                {
                    _nextSnapshot = Time.realtimeSinceStartup + SnapshotInterval;
                    AppendBodySnapshot("periodic", current);
                }
            }
            catch (Exception ex)
            {
                Append("PROBE_ERROR stage=update error=" + Quote(ex.ToString()));
                Logger.LogError(PluginName + " update failed: " + ex);
                enabled = false;
            }
        }

        private bool EnsureDropsList()
        {
            UnityEngine.Object current = _dropsList as UnityEngine.Object;
            if (current != null)
                return true;

            _dropsList = UnityEngine.Object.FindObjectOfType(_dropsListType);
            current = _dropsList as UnityEngine.Object;
            if (current == null)
                return false;

            _knownBodies.Clear();
            Append("DROPS_OWNER_READY instance=" + current.GetInstanceID().ToString(CultureInfo.InvariantCulture));
            Dictionary<int, DropInfo> initial = ReadBodyDrops();
            AppendBodySnapshot("owner_ready", initial);
            AppendAnchorSnapshot("owner_ready");

            foreach (KeyValuePair<int, DropInfo> pair in initial)
                _knownBodies[pair.Key] = pair.Value;

            return true;
        }

        private Dictionary<int, DropInfo> ReadBodyDrops()
        {
            Dictionary<int, DropInfo> result = new Dictionary<int, DropInfo>();
            object dropsObject = GetMemberObject(_dropsList, "drops");
            IEnumerable drops = dropsObject as IEnumerable;
            if (drops == null)
                throw new InvalidOperationException("DropsList.drops is not enumerable.");

            foreach (object drop in drops)
            {
                if (drop == null)
                    continue;

                UnityEngine.Object unityDrop = drop as UnityEngine.Object;
                Component component = drop as Component;
                if (unityDrop == null || component == null)
                    continue;

                object res = GetMemberObject(drop, "res");
                object definition = GetMemberObject(res, "definition");
                object itemType = GetMemberObject(definition, "type");
                string typeName = SafeValue(itemType);

                if (!string.Equals(typeName, "Body", StringComparison.Ordinal))
                    continue;

                DropInfo info = new DropInfo();
                info.InstanceId = unityDrop.GetInstanceID();
                info.ItemId = SafeValue(GetMemberObject(res, "id"));
                info.Position = component.transform.position;
                info.ZoneId = SafeValue(GetMemberObject(drop, "zone_id"));
                info.ItemDropZoneId = SafeValue(GetMemberObject(res, "drop_zone_id"));
                info.IsCollected = SafeBool(GetMemberObject(drop, "is_collected"));
                info.UnityName = unityDrop.name ?? string.Empty;
                result[info.InstanceId] = info;
            }

            return result;
        }

        private void AppendBodySnapshot(string reason, Dictionary<int, DropInfo> bodies)
        {
            if (bodies.Count == 0)
            {
                Append("BODY_SNAPSHOT reason=" + reason + " count=0");
                return;
            }

            foreach (DropInfo body in bodies.Values.OrderBy(x => x.InstanceId))
                Append("BODY_SNAPSHOT reason=" + reason + " count=" + bodies.Count.ToString(CultureInfo.InvariantCulture) + " " + body.ToFields());
        }

        private void AppendAnchorSnapshot(string reason)
        {
            Append("ANCHOR_SNAPSHOT reason=" + reason
                + " morgue_throw_out=" + DescribeWorldObject(InvokeWorldMapLookup("GetWorldGameObjectByCustomTag", "morgue_throw_out"))
                + " morgue_throw_in=" + DescribeWorldObject(InvokeWorldMapLookup("GetWorldGameObjectByCustomTag", "morgue_throw_in"))
                + " donkey=" + DescribeWorldObject(InvokeWorldMapLookup("GetWorldGameObjectByObjId", "donkey")));
        }

        private object InvokeWorldMapLookup(string methodName, string id)
        {
            MethodInfo method = _worldMapType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(bool) },
                null);

            if (method == null)
                return null;

            try
            {
                return method.Invoke(null, new object[] { id, true });
            }
            catch
            {
                return null;
            }
        }

        private static string DescribeWorldObject(object obj)
        {
            if (obj == null)
                return "<null>";

            Component component = obj as Component;
            UnityEngine.Object unity = obj as UnityEngine.Object;
            Vector3 pos = component != null ? component.transform.position : Vector3.zero;

            return "{instance=" + (unity != null ? unity.GetInstanceID().ToString(CultureInfo.InvariantCulture) : "?")
                + ",name=" + Quote(unity != null ? unity.name : string.Empty)
                + ",obj_id=" + Quote(SafeValue(GetMemberObject(obj, "obj_id")))
                + ",custom_tag=" + Quote(SafeValue(GetMemberObject(obj, "custom_tag")))
                + ",pos=" + FormatVector(pos)
                + "}";
        }

        private void WriteHeader(Guid mvid)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("KEEPERS ALERTS — CORPSE WAITING STATE PROBE");
            sb.AppendLine("ProbeVersion=" + PluginVersion);
            sb.AppendLine("GeneratedUtc=" + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            sb.AppendLine("AssemblyCSharpMvid=" + mvid);
            sb.AppendLine("Contract=READ_ONLY_DROPSLIST_OBSERVER_NO_HARMONY_NO_MUTATION_NO_SAVE_WRITE");
            sb.AppendLine("Question=What native position/zone identity does an uncollected donkey-delivered corpse have, does that loose-drop identity reconstruct cleanly after save/load, and when does the host remove it?");
            sb.AppendLine("---");
            File.AppendAllText(_reportPath, sb.ToString(), new UTF8Encoding(false));
        }

        private void Append(string line)
        {
            string full = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + " " + line + Environment.NewLine;
            File.AppendAllText(_reportPath, full, new UTF8Encoding(false));
            Logger.LogInfo("CORPSE_STATE_PROBE " + line);
        }

        private static object GetMemberObject(object obj, params string[] names)
        {
            if (obj == null)
                return null;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type type = obj.GetType();

            foreach (string name in names)
            {
                for (Type current = type; current != null; current = current.BaseType)
                {
                    FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                    if (field != null)
                    {
                        try { return field.GetValue(obj); }
                        catch { }
                    }

                    PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                    if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
                    {
                        try { return property.GetValue(obj, null); }
                        catch { }
                    }
                }
            }

            return null;
        }

        private static string SafeValue(object value)
        {
            if (value == null)
                return string.Empty;

            try
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool SafeBool(object value)
        {
            if (value == null)
                return false;

            try
            {
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return false;
            }
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.###", CultureInfo.InvariantCulture)
                + "," + value.y.ToString("0.###", CultureInfo.InvariantCulture)
                + "," + value.z.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Quote(string value)
        {
            if (value == null)
                return "\"\"";

            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private sealed class DropInfo
        {
            public int InstanceId;
            public string ItemId;
            public Vector3 Position;
            public string ZoneId;
            public string ItemDropZoneId;
            public bool IsCollected;
            public string UnityName;

            public string ToFields()
            {
                return "instance=" + InstanceId.ToString(CultureInfo.InvariantCulture)
                    + " item_id=" + Quote(ItemId)
                    + " unity_name=" + Quote(UnityName)
                    + " pos=" + FormatVector(Position)
                    + " zone_id=" + Quote(ZoneId)
                    + " item_drop_zone_id=" + Quote(ItemDropZoneId)
                    + " collected=" + IsCollected.ToString();
            }
        }
    }
}
