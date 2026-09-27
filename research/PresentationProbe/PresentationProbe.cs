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
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx;
using UnityEngine;

namespace KeepersAlertsResearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class PresentationProbe : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.gyk.keepersalerts.research.presentationprobe";
        public const string PluginName = "Keeper's Alerts Presentation Probe";
        public const string PluginVersion = "0.1.0";

        private static readonly Guid SupportedGameMvid = new Guid("6f50b8e7-156b-49ac-bbe8-7505894b2364");
        private const float RetryInterval = 1f;
        private const float ReadyGraceSeconds = 3f;
        private const float MaxReadyWaitSeconds = 30f;

        private float _nextTry;
        private float _guiReadyAt = -1f;
        private bool _completed;
        private string _reportPath;
        private Assembly _gameAssembly;
        private Type _guiElementsType;
        private Type _worldMapType;
        private Type _mainGameType;

        private void Awake()
        {
            _reportPath = Path.Combine(Paths.BepInExRootPath, "KeepersAlerts-presentation-probe-0.1.0.txt");

            try
            {
                _gameAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));

                if (_gameAssembly == null)
                    throw new InvalidOperationException("Assembly-CSharp is not loaded.");

                Guid mvid = _gameAssembly.ManifestModule.ModuleVersionId;
                if (mvid != SupportedGameMvid)
                    throw new InvalidOperationException("Unsupported Assembly-CSharp MVID " + mvid);

                _guiElementsType = _gameAssembly.GetType("GUIElements", false);
                _worldMapType = _gameAssembly.GetType("WorldMap", false);
                _mainGameType = _gameAssembly.GetType("MainGame", false);

                if (_guiElementsType == null || _worldMapType == null || _mainGameType == null)
                    throw new InvalidOperationException("Required host types were not found.");

                Logger.LogInfo(PluginName + " " + PluginVersion + " loaded. Read-only hierarchy/audio inspection; waiting for loaded gameplay.");
            }
            catch (Exception ex)
            {
                WriteFailure(ex);
                Logger.LogError(PluginName + " disabled: " + ex);
                enabled = false;
            }
        }

        private void Update()
        {
            if (_completed || Time.realtimeSinceStartup < _nextTry)
                return;

            _nextTry = Time.realtimeSinceStartup + RetryInterval;

            try
            {
                object gui = UnityEngine.Object.FindObjectOfType(_guiElementsType);
                if (gui == null || !IsGameStarted())
                    return;

                if (_guiReadyAt < 0f)
                    _guiReadyAt = Time.realtimeSinceStartup;

                object bodyArrived = GetMemberObject(gui, "body_arrived_gui");
                object hud = GetMemberObject(gui, "hud");
                object interactionBubble = GetMemberObject(gui, "interaction_bubble");

                bool coreReady = IsLiveUnityObject(bodyArrived) && IsLiveUnityObject(hud) && IsLiveUnityObject(interactionBubble);
                if (!coreReady)
                    return;

                float readyFor = Time.realtimeSinceStartup - _guiReadyAt;
                Transform soundTf = FindTransformByPathSuffix("/church_pulpit/content/church_pulpit(Clone)/PrayFX/pray sound");
                object outWgo = InvokeWorldMapLookup("GetWorldGameObjectByCustomTag", "morgue_throw_out");
                object inWgo = InvokeWorldMapLookup("GetWorldGameObjectByCustomTag", "morgue_throw_in");

                bool optionalReady = soundTf != null && IsLiveUnityObject(outWgo) && IsLiveUnityObject(inWgo);
                if (readyFor < ReadyGraceSeconds || (!optionalReady && readyFor < MaxReadyWaitSeconds))
                    return;

                _completed = true;
                WriteReport(gui, bodyArrived, hud, interactionBubble, soundTf, outWgo, inWgo);
                Logger.LogInfo(PluginName + " complete: " + _reportPath);
            }
            catch (Exception ex)
            {
                _completed = true;
                WriteFailure(ex);
                Logger.LogError(PluginName + " failed: " + ex);
            }
        }

        private bool IsGameStarted()
        {
            object value = GetStaticMemberObject(_mainGameType, "game_started");
            if (value is bool)
                return (bool)value;

            return false;
        }

        private void WriteReport(object gui, object bodyArrived, object hud, object interactionBubble, Transform soundTf, object outWgo, object inWgo)
        {
            StringBuilder sb = new StringBuilder(256 * 1024);
            sb.AppendLine("KEEPERS ALERTS — PRESENTATION SEAM PROBE");
            sb.AppendLine("ProbeVersion=" + PluginVersion);
            sb.AppendLine("GeneratedUtc=" + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            sb.AppendLine("AssemblyCSharpMvid=" + _gameAssembly.ManifestModule.ModuleVersionId);
            sb.AppendLine("Contract=LOAD_ONLY_READ_ONLY_NO_HARMONY_NO_UI_MUTATION_NO_AUDIO_PLAYBACK_NO_GRAPH_EXECUTION_NO_SAVE_WRITE");
            sb.AppendLine("Questions=BODY_ARRIVAL_HIERARCHY;HUD_ANCHORS;PRAY_BUBBLE_FONT_SYMBOL;CHURCH_PRAY_SOUND_EVENTS;MORGUE_ENDPOINT_GEOMETRY");
            sb.AppendLine();

            sb.AppendLine("=== GUI ELEMENTS ===");
            sb.AppendLine(DescribeUnityObject(gui));
            sb.AppendLine();

            sb.AppendLine("=== BODY ARRIVAL GUI ===");
            DumpNamedMembers(sb, bodyArrived, "body_arrived_gui",
                "_parent_object", "_visible_point_y", "_appear_time", "_display_time", "_hide_time");
            DumpAncestors(sb, AsComponent(bodyArrived), 8);
            DumpHierarchy(sb, AsComponent(bodyArrived) != null ? AsComponent(bodyArrived).transform : null, 0, 6, "BODY_ARRIVAL");
            sb.AppendLine();

            sb.AppendLine("=== HUD ===");
            DumpNamedMembers(sb, hud, "hud", "panel", "zone_name", "zone_descr", "zone_descr_object", "sins_circle", "tech_points_bar", "toolbar");
            DumpAncestors(sb, AsComponent(hud), 6);
            DumpHierarchy(sb, AsComponent(hud) != null ? AsComponent(hud).transform : null, 0, 4, "HUD");
            sb.AppendLine();

            sb.AppendLine("=== INTERACTION BUBBLE PREFAB ===");
            DumpAncestors(sb, AsComponent(interactionBubble), 8);
            DumpHierarchy(sb, AsComponent(interactionBubble) != null ? AsComponent(interactionBubble).transform : null, 0, 6, "INTERACTION_BUBBLE");
            DumpPrayerSymbolCandidates(sb, AsComponent(interactionBubble));
            sb.AppendLine();

            sb.AppendLine("=== CHURCH PRAY SOUND ===");
            if (soundTf == null)
            {
                sb.AppendLine("sound_transform=<not found>");
            }
            else
            {
                sb.AppendLine("sound_transform=" + DescribeTransform(soundTf));
                Component[] components = soundTf.GetComponents<Component>();
                foreach (Component component in components)
                {
                    if (component == null) continue;
                    sb.AppendLine("SOUND_COMPONENT type=" + component.GetType().FullName);
                    if (component.GetType().FullName.IndexOf("EventSounds", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        HashSet<object> visited = new HashSet<object>(ReferenceComparer.Instance);
                        DumpInterestingObjectGraph(sb, component, "  eventsounds", 0, 4, visited);
                    }
                    else
                    {
                        DumpSelectedUIComponent(sb, component, "  ");
                    }
                }
            }
            sb.AppendLine();

            sb.AppendLine("=== MORGUE ENDPOINTS ===");
            DumpEndpoint(sb, "morgue_throw_out", outWgo);
            DumpEndpoint(sb, "morgue_throw_in", inWgo);
            object donkey = InvokeWorldMapLookup("GetWorldGameObjectByObjId", "donkey");
            DumpEndpoint(sb, "donkey", donkey);
            sb.AppendLine();

            File.WriteAllText(_reportPath, sb.ToString(), new UTF8Encoding(false));
        }

        private void DumpEndpoint(StringBuilder sb, string label, object wgo)
        {
            if (!IsLiveUnityObject(wgo))
            {
                sb.AppendLine("ENDPOINT " + label + "=<not found>");
                return;
            }

            Component component = AsComponent(wgo);
            sb.AppendLine("ENDPOINT " + label + " " + DescribeUnityObject(wgo));
            if (component != null)
            {
                sb.AppendLine("  transform=" + DescribeTransform(component.transform));
                DumpHierarchy(sb, component.transform, 0, 3, "ENDPOINT_" + label.ToUpperInvariant());
            }

            object dockPoints = GetMemberObject(wgo, "_dock_points", "dock_points");
            IEnumerable enumerable = dockPoints as IEnumerable;
            if (enumerable == null)
            {
                sb.AppendLine("  dock_points=" + SafeValue(dockPoints));
                return;
            }

            int index = 0;
            foreach (object dock in enumerable)
            {
                if (index >= 16) break;
                Transform tf = null;
                Component dc = dock as Component;
                if (dc != null) tf = dc.transform;
                if (tf == null) tf = GetMemberObject(dock, "tf", "transform") as Transform;
                sb.AppendLine("  dock[" + index.ToString(CultureInfo.InvariantCulture) + "]=" + DescribeObject(dock)
                    + (tf != null ? " " + DescribeTransform(tf) : ""));
                index++;
            }
        }

        private void DumpPrayerSymbolCandidates(StringBuilder sb, Component root)
        {
            sb.AppendLine("--- PRAY SYMBOL CANDIDATES ---");
            if (root == null)
            {
                sb.AppendLine("interaction_bubble_root=<null>");
                return;
            }

            Component[] components = root.GetComponentsInChildren<Component>(true);
            HashSet<int> fonts = new HashSet<int>();

            foreach (Component component in components)
            {
                if (component == null || !string.Equals(component.GetType().Name, "UILabel", StringComparison.Ordinal))
                    continue;

                object bitmapFont = GetMemberObject(component, "bitmapFont");
                UnityEngine.Object fontUnity = bitmapFont as UnityEngine.Object;
                sb.AppendLine("LABEL path=" + Quote(GetHierarchyPath(component.transform))
                    + " text=" + Quote(SafeValue(GetMemberObject(component, "text")))
                    + " bitmapFont=" + DescribeObject(bitmapFont)
                    + " trueTypeFont=" + DescribeObject(GetMemberObject(component, "trueTypeFont"))
                    + " symbolStyle=" + SafeValue(GetMemberObject(component, "symbolStyle")));

                if (fontUnity == null || !fonts.Add(fontUnity.GetInstanceID()))
                    continue;

                DumpFontSymbols(sb, bitmapFont);
            }
        }

        private void DumpFontSymbols(StringBuilder sb, object font)
        {
            if (font == null) return;

            sb.AppendLine("FONT_SYMBOL_SCAN font=" + DescribeObject(font));
            object symbols = GetMemberObject(font, "symbols", "mSymbols", "_symbols");
            IEnumerable enumerable = symbols as IEnumerable;
            if (enumerable == null)
            {
                sb.AppendLine("  symbols=<not enumerable> " + DescribeObject(symbols));
                return;
            }

            int count = 0;
            int matching = 0;
            foreach (object symbol in enumerable)
            {
                count++;
                string sequence = SafeValue(GetMemberObject(symbol, "sequence", "mSequence", "_sequence"));
                string spriteName = SafeValue(GetMemberObject(symbol, "spriteName", "mSpriteName", "_spriteName"));
                if (sequence.IndexOf("pray", StringComparison.OrdinalIgnoreCase) < 0
                    && sequence.IndexOf("bubble", StringComparison.OrdinalIgnoreCase) < 0
                    && spriteName.IndexOf("pray", StringComparison.OrdinalIgnoreCase) < 0
                    && spriteName.IndexOf("bubble", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                matching++;
                sb.AppendLine("  SYMBOL sequence=" + Quote(sequence)
                    + " spriteName=" + Quote(spriteName)
                    + " type=" + (symbol == null ? "<null>" : symbol.GetType().FullName));
                DumpSimpleMembers(sb, symbol, "    ", 32);
            }

            sb.AppendLine("  symbol_count=" + count.ToString(CultureInfo.InvariantCulture)
                + " matching_pray_or_bubble=" + matching.ToString(CultureInfo.InvariantCulture));
        }

        private void DumpHierarchy(StringBuilder sb, Transform tf, int depth, int maxDepth, string role)
        {
            if (tf == null || depth > maxDepth)
                return;

            string indent = new string(' ', depth * 2);
            sb.AppendLine(indent + "NODE role=" + role + " depth=" + depth.ToString(CultureInfo.InvariantCulture)
                + " " + DescribeTransform(tf));

            Component[] components = tf.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null) continue;
                sb.AppendLine(indent + "  COMPONENT type=" + component.GetType().FullName);
                DumpSelectedUIComponent(sb, component, indent + "    ");
            }

            if (depth == maxDepth)
            {
                if (tf.childCount > 0)
                    sb.AppendLine(indent + "  CHILDREN_TRUNCATED count=" + tf.childCount.ToString(CultureInfo.InvariantCulture));
                return;
            }

            for (int i = 0; i < tf.childCount; i++)
                DumpHierarchy(sb, tf.GetChild(i), depth + 1, maxDepth, role);
        }

        private void DumpSelectedUIComponent(StringBuilder sb, object component, string indent)
        {
            if (component == null) return;

            string name = component.GetType().Name;
            if (name == "Transform" || name == "RectTransform")
                return;

            string[] common = {
                "enabled", "depth", "alpha", "width", "height", "pivot", "color",
                "clipping", "baseClipRegion", "clipOffset",
                "text", "fontSize", "bitmapFont", "trueTypeFont", "alignment",
                "overflowMethod", "overflowWidth", "spacingX", "spacingY", "symbolStyle",
                "sprite2D", "spriteName", "atlas",
                "side", "relativeOffset", "pixelOffset", "container",
                "manualHeight", "activeHeight", "scalingStyle"
            };

            foreach (string memberName in common)
            {
                object value = GetMemberObject(component, memberName);
                if (value == null) continue;
                sb.AppendLine(indent + memberName + "=" + DescribeObject(value));
            }

            foreach (string anchorName in new[] { "leftAnchor", "rightAnchor", "bottomAnchor", "topAnchor" })
            {
                object anchor = GetMemberObject(component, anchorName);
                if (anchor == null) continue;
                sb.AppendLine(indent + anchorName + "=" + DescribeAnchor(anchor));
            }
        }

        private string DescribeAnchor(object anchor)
        {
            object target = GetMemberObject(anchor, "target");
            object relative = GetMemberObject(anchor, "relative");
            object absolute = GetMemberObject(anchor, "absolute");
            return "{target=" + DescribeObject(target)
                + ",relative=" + SafeValue(relative)
                + ",absolute=" + SafeValue(absolute) + "}";
        }

        private void DumpAncestors(StringBuilder sb, Component component, int max)
        {
            sb.AppendLine("--- ANCESTORS ---");
            if (component == null)
            {
                sb.AppendLine("<null>");
                return;
            }

            Transform current = component.transform;
            int n = 0;
            while (current != null && n < max)
            {
                sb.AppendLine("ancestor[" + n.ToString(CultureInfo.InvariantCulture) + "]=" + DescribeTransform(current));
                Component[] components = current.GetComponents<Component>();
                foreach (Component c in components)
                {
                    if (c == null) continue;
                    if (c.GetType().Name == "UIPanel" || c.GetType().Name == "UIRoot" || c.GetType().Name == "UIAnchor")
                    {
                        sb.AppendLine("  component=" + c.GetType().FullName);
                        DumpSelectedUIComponent(sb, c, "    ");
                    }
                }
                current = current.parent;
                n++;
            }
        }

        private void DumpNamedMembers(StringBuilder sb, object obj, string label, params string[] names)
        {
            sb.AppendLine(label + "=" + DescribeUnityObject(obj));
            foreach (string name in names)
            {
                object value = GetMemberObject(obj, name);
                sb.AppendLine("  " + name + "=" + DescribeObject(value));
            }
        }

        private void DumpInterestingObjectGraph(StringBuilder sb, object obj, string label, int depth, int maxDepth, HashSet<object> visited)
        {
            if (obj == null)
            {
                sb.AppendLine(label + "=<null>");
                return;
            }

            if (depth > maxDepth)
            {
                sb.AppendLine(label + "=<depth-limit>");
                return;
            }

            Type type = obj.GetType();
            if (IsSimple(obj))
            {
                sb.AppendLine(label + "=" + SafeValue(obj));
                return;
            }

            UnityEngine.Object unity = obj as UnityEngine.Object;
            if (unity != null && depth > 0 && !(obj is Component))
            {
                sb.AppendLine(label + "=" + DescribeObject(obj));
                return;
            }

            if (!type.IsValueType && !visited.Add(obj))
            {
                sb.AppendLine(label + "=<cycle " + type.FullName + ">");
                return;
            }

            IEnumerable enumerable = obj as IEnumerable;
            if (enumerable != null && !(obj is string))
            {
                sb.AppendLine(label + " type=" + type.FullName + " enumerable");
                int i = 0;
                foreach (object item in enumerable)
                {
                    if (i >= 64)
                    {
                        sb.AppendLine(label + "[...] = <truncated>");
                        break;
                    }
                    DumpInterestingObjectGraph(sb, item, label + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", depth + 1, maxDepth, visited);
                    i++;
                }
                return;
            }

            sb.AppendLine(label + " type=" + type.FullName + " value=" + DescribeObject(obj));
            foreach (MemberInfo member in EnumerateMembers(type))
            {
                if (!IsInterestingAudioMember(member.Name))
                    continue;

                object value = ReadMemberValue(obj, member);
                if (value == null)
                {
                    sb.AppendLine(label + "." + member.Name + "=<null>");
                    continue;
                }

                if (IsSimple(value) || value is UnityEngine.Object)
                    sb.AppendLine(label + "." + member.Name + "=" + DescribeObject(value));
                else
                    DumpInterestingObjectGraph(sb, value, label + "." + member.Name, depth + 1, maxDepth, visited);
            }
        }

        private static bool IsInterestingAudioMember(string name)
        {
            string[] needles = { "sound", "event", "group", "clip", "audio", "variation", "volume", "pitch", "delay", "name", "type", "source" };
            return needles.Any(n => name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void DumpSimpleMembers(StringBuilder sb, object obj, string indent, int max)
        {
            if (obj == null) return;
            int count = 0;
            foreach (MemberInfo member in EnumerateMembers(obj.GetType()))
            {
                object value = ReadMemberValue(obj, member);
                if (!IsSimple(value) && !(value is UnityEngine.Object))
                    continue;
                sb.AppendLine(indent + member.Name + "=" + DescribeObject(value));
                count++;
                if (count >= max) break;
            }
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

            try { return method.Invoke(null, new object[] { id, true }); }
            catch { return null; }
        }

        private static Transform FindTransformByPathSuffix(string suffix)
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform tf in transforms)
            {
                if (tf == null) continue;
                string path = GetHierarchyPath(tf);
                if (path.EndsWith(suffix, StringComparison.Ordinal))
                    return tf;
            }
            return null;
        }

        private static Component AsComponent(object obj)
        {
            return obj as Component;
        }

        private static bool IsLiveUnityObject(object obj)
        {
            UnityEngine.Object unity = obj as UnityEngine.Object;
            return unity != null;
        }

        private static string DescribeTransform(Transform tf)
        {
            if (tf == null) return "<null>";
            return "path=" + Quote(GetHierarchyPath(tf))
                + " activeSelf=" + tf.gameObject.activeSelf
                + " activeInHierarchy=" + tf.gameObject.activeInHierarchy
                + " layer=" + tf.gameObject.layer.ToString(CultureInfo.InvariantCulture)
                + " localPos=" + FormatVector(tf.localPosition)
                + " worldPos=" + FormatVector(tf.position)
                + " localScale=" + FormatVector(tf.localScale)
                + " children=" + tf.childCount.ToString(CultureInfo.InvariantCulture);
        }

        private static string DescribeUnityObject(object obj)
        {
            if (obj == null) return "<null>";
            UnityEngine.Object unity = obj as UnityEngine.Object;
            Component component = obj as Component;
            return (obj.GetType().FullName ?? obj.GetType().Name)
                + (unity != null ? "#" + unity.GetInstanceID().ToString(CultureInfo.InvariantCulture) + " name=" + Quote(unity.name) : "")
                + (component != null ? " path=" + Quote(GetHierarchyPath(component.transform)) : "");
        }

        private static string DescribeObject(object value)
        {
            if (value == null) return "<null>";
            if (IsSimple(value)) return SafeValue(value);

            Transform tf = value as Transform;
            if (tf != null) return DescribeTransform(tf);

            Component component = value as Component;
            if (component != null) return DescribeUnityObject(component);

            GameObject go = value as GameObject;
            if (go != null) return "GameObject#" + go.GetInstanceID().ToString(CultureInfo.InvariantCulture) + " path=" + Quote(GetHierarchyPath(go.transform));

            UnityEngine.Object unity = value as UnityEngine.Object;
            if (unity != null) return (value.GetType().FullName ?? value.GetType().Name) + "#" + unity.GetInstanceID().ToString(CultureInfo.InvariantCulture) + " name=" + Quote(unity.name);

            return (value.GetType().FullName ?? value.GetType().Name) + " " + SafeValue(value);
        }

        private static string DescribeObjectSimple(object value)
        {
            return DescribeObject(value);
        }

        private static object GetStaticMemberObject(Type type, params string[] names)
        {
            if (type == null) return null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            foreach (string name in names)
            {
                for (Type current = type; current != null; current = current.BaseType)
                {
                    FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                    if (field != null)
                    {
                        try { return field.GetValue(null); } catch { }
                    }

                    PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                    if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
                    {
                        try { return property.GetValue(null, null); } catch { }
                    }
                }
            }
            return null;
        }

        private static object GetMemberObject(object obj, params string[] names)
        {
            if (obj == null) return null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (string name in names)
            {
                for (Type current = obj.GetType(); current != null; current = current.BaseType)
                {
                    FieldInfo field = current.GetField(name, flags | BindingFlags.DeclaredOnly);
                    if (field != null)
                    {
                        try { return field.GetValue(obj); } catch { }
                    }

                    PropertyInfo property = current.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                    if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
                    {
                        try { return property.GetValue(obj, null); } catch { }
                    }
                }
            }

            return null;
        }

        private static IEnumerable<MemberInfo> EnumerateMembers(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            HashSet<string> yielded = new HashSet<string>(StringComparer.Ordinal);

            for (Type current = type; current != null; current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(flags))
                {
                    if (yielded.Add("F:" + field.Name)) yield return field;
                }

                foreach (PropertyInfo property in current.GetProperties(flags))
                {
                    if (property.CanRead && property.GetIndexParameters().Length == 0 && yielded.Add("P:" + property.Name))
                        yield return property;
                }
            }
        }

        private static object ReadMemberValue(object obj, MemberInfo member)
        {
            if (obj == null || member == null) return null;
            try
            {
                FieldInfo field = member as FieldInfo;
                if (field != null) return field.GetValue(obj);
                PropertyInfo property = member as PropertyInfo;
                if (property != null) return property.GetValue(obj, null);
            }
            catch { }
            return null;
        }

        private static bool IsSimple(object value)
        {
            if (value == null) return true;
            Type type = value.GetType();
            return value is string || type.IsPrimitive || type.IsEnum || value is decimal || value is Guid
                || value is Vector2 || value is Vector3 || value is Vector4 || value is Color || value is Rect;
        }

        private static string SafeValue(object value)
        {
            if (value == null) return "<null>";
            try
            {
                if (value is string) return Quote((string)value);
                if (value is Vector2)
                {
                    Vector2 v = (Vector2)value;
                    return FormatFloat(v.x) + "," + FormatFloat(v.y);
                }
                if (value is Vector3) return FormatVector((Vector3)value);
                if (value is Vector4)
                {
                    Vector4 v = (Vector4)value;
                    return FormatFloat(v.x) + "," + FormatFloat(v.y) + "," + FormatFloat(v.z) + "," + FormatFloat(v.w);
                }
                if (value is Color)
                {
                    Color v = (Color)value;
                    return FormatFloat(v.r) + "," + FormatFloat(v.g) + "," + FormatFloat(v.b) + "," + FormatFloat(v.a);
                }
                if (value is Rect)
                {
                    Rect v = (Rect)value;
                    return FormatFloat(v.x) + "," + FormatFloat(v.y) + "," + FormatFloat(v.width) + "," + FormatFloat(v.height);
                }
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null-string>";
            }
            catch { return "<unprintable>"; }
        }

        private static string GetHierarchyPath(Transform tf)
        {
            if (tf == null) return string.Empty;
            List<string> parts = new List<string>();
            for (Transform current = tf; current != null; current = current.parent)
                parts.Add(current.name);
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static string FormatVector(Vector3 value)
        {
            return FormatFloat(value.x) + "," + FormatFloat(value.y) + "," + FormatFloat(value.z);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Quote(string value)
        {
            if (value == null) return "\"\"";
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private void WriteFailure(Exception ex)
        {
            try
            {
                string text = "KEEPERS ALERTS — PRESENTATION SEAM PROBE" + Environment.NewLine
                    + "ProbeVersion=" + PluginVersion + Environment.NewLine
                    + "ERROR=" + ex + Environment.NewLine;
                File.WriteAllText(_reportPath, text, new UTF8Encoding(false));
            }
            catch { }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
            public int GetHashCode(object obj) { return RuntimeHelpers.GetHashCode(obj); }
        }
    }
}
