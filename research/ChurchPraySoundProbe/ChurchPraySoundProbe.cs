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
    public sealed class ChurchPraySoundProbe : BaseUnityPlugin
    {
        private const string PluginGuid = "nikichmods.keepersalerts.research.churchpraysoundprobe";
        private const string PluginName = "Keeper's Alerts Church Pray Sound Probe";
        private const string PluginVersion = "0.1.1";
        private static readonly Guid SupportedGameMvid = new Guid("6f50b8e7-156b-49ac-bbe8-7505894b2364");

        private const float RetrySeconds = 1f;
        private float _nextTry;
        private bool _completed;
        private Assembly _gameAssembly;
        private string _reportPath;

        private void Awake()
        {
            _reportPath = Path.Combine(Paths.BepInExRootPath, "KeepersAlerts-church-pray-sound-probe-0.1.1.txt");

            try
            {
                _gameAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));

                if (_gameAssembly == null)
                    throw new InvalidOperationException("Assembly-CSharp is not loaded.");

                Guid mvid = _gameAssembly.ManifestModule.ModuleVersionId;
                if (mvid != SupportedGameMvid)
                    throw new InvalidOperationException("Unsupported Assembly-CSharp MVID " + mvid);

                Logger.LogInfo(PluginName + " " + PluginVersion + " loaded. Read-only; waiting for the church pulpit prayer-sound object.");
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

            _nextTry = Time.realtimeSinceStartup + RetrySeconds;

            try
            {
                Transform target = FindPrayerSoundTransform();
                if (target == null)
                    return;

                _completed = true;
                WriteReport(target);
                Logger.LogInfo(PluginName + " complete: " + _reportPath);
                enabled = false;
            }
            catch (Exception ex)
            {
                _completed = true;
                WriteFailure(ex);
                Logger.LogError(PluginName + " failed: " + ex);
                enabled = false;
            }
        }

        private static Transform FindPrayerSoundTransform()
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform tf in transforms)
            {
                if (tf == null || !string.Equals(tf.name, "pray sound", StringComparison.Ordinal))
                    continue;

                string path = GetHierarchyPath(tf);
                if (path.EndsWith("/[wgo] church_pulpit/content/church_pulpit(Clone)/PrayFX/pray sound", StringComparison.Ordinal)
                    || (path.IndexOf("/[wgo] church_pulpit/", StringComparison.Ordinal) >= 0
                        && path.IndexOf("/PrayFX/", StringComparison.Ordinal) >= 0))
                    return tf;
            }

            return null;
        }

        private void WriteReport(Transform target)
        {
            StringBuilder sb = new StringBuilder(192 * 1024);
            sb.AppendLine("KEEPERS ALERTS — CHURCH PRAY SOUND PROBE");
            sb.AppendLine("ProbeVersion=" + PluginVersion);
            sb.AppendLine("GeneratedUtc=" + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            sb.AppendLine("AssemblyCSharpMvid=" + _gameAssembly.ManifestModule.ModuleVersionId);
            sb.AppendLine("Contract=LOAD_ONLY_READ_ONLY_NO_HARMONY_NO_UI_MUTATION_NO_AUDIO_PLAYBACK_NO_GRAPH_EXECUTION_NO_SAVE_WRITE");
            sb.AppendLine("Question=EXACT_CHURCH_PULPIT_PRAY_SOUND_EVENTSOUNDS_CONFIGURATION");
            sb.AppendLine();
            sb.AppendLine("=== TARGET ===");
            sb.AppendLine(DescribeTransform(target));
            sb.AppendLine();

            Component[] components = target.GetComponents<Component>();
            sb.AppendLine("ComponentCount=" + components.Length.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                    continue;

                sb.AppendLine();
                sb.AppendLine("=== COMPONENT[" + i.ToString(CultureInfo.InvariantCulture) + "] " + component.GetType().FullName + " ===");

                if (component.GetType().FullName.IndexOf("EventSounds", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    HashSet<object> visited = new HashSet<object>(ReferenceComparer.Instance);
                    DumpObject(sb, component, "eventsounds", 0, 7, visited);
                }
                else
                {
                    DumpDeclaredMembers(sb, component, "component", 0, 2, new HashSet<object>(ReferenceComparer.Instance));
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== RELATED CHURCH AUDIO OBJECTS ===");
            int related = 0;
            foreach (Transform tf in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tf == null)
                    continue;

                string path = GetHierarchyPath(tf);
                if (path.IndexOf("/[wgo] church_pulpit/", StringComparison.Ordinal) < 0)
                    continue;

                Component[] siblings = tf.GetComponents<Component>();
                bool interesting = tf.name.IndexOf("sound", StringComparison.OrdinalIgnoreCase) >= 0
                    || siblings.Any(c => c != null && c.GetType().FullName.IndexOf("EventSounds", StringComparison.OrdinalIgnoreCase) >= 0);

                if (!interesting)
                    continue;

                sb.AppendLine("RELATED[" + related.ToString(CultureInfo.InvariantCulture) + "] " + DescribeTransform(tf));
                foreach (Component c in siblings)
                {
                    if (c != null)
                        sb.AppendLine("  component=" + c.GetType().FullName);
                }

                related++;
                if (related >= 64)
                    break;
            }
            sb.AppendLine("RelatedCount=" + related.ToString(CultureInfo.InvariantCulture));

            File.WriteAllText(_reportPath, sb.ToString(), new UTF8Encoding(false));
        }

        private static void DumpObject(StringBuilder sb, object value, string label, int depth, int maxDepth, HashSet<object> visited)
        {
            string indent = new string(' ', depth * 2);

            if (value == null)
            {
                sb.AppendLine(indent + label + "=<null>");
                return;
            }

            if (IsSimple(value))
            {
                sb.AppendLine(indent + label + "=" + SafeValue(value));
                return;
            }

            UnityEngine.Object unity = value as UnityEngine.Object;
            if (unity != null && !(value is Component))
            {
                sb.AppendLine(indent + label + "=" + DescribeUnityObject(unity));
                return;
            }

            if (depth > maxDepth)
            {
                sb.AppendLine(indent + label + "=<max-depth> " + value.GetType().FullName);
                return;
            }

            Type type = value.GetType();
            if (!type.IsValueType && !(value is string))
            {
                if (!visited.Add(value))
                {
                    sb.AppendLine(indent + label + "=<already-visited> " + type.FullName);
                    return;
                }
            }

            IDictionary dictionary = value as IDictionary;
            if (dictionary != null)
            {
                sb.AppendLine(indent + label + " type=" + type.FullName + " count=" + dictionary.Count.ToString(CultureInfo.InvariantCulture));
                int i = 0;
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (i >= 128)
                    {
                        sb.AppendLine(indent + "  <dictionary-truncated>");
                        break;
                    }

                    DumpObject(sb, entry.Key, "[" + i.ToString(CultureInfo.InvariantCulture) + "].key", depth + 1, maxDepth, visited);
                    DumpObject(sb, entry.Value, "[" + i.ToString(CultureInfo.InvariantCulture) + "].value", depth + 1, maxDepth, visited);
                    i++;
                }
                return;
            }

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null && !(value is string))
            {
                sb.AppendLine(indent + label + " type=" + type.FullName);
                int i = 0;
                foreach (object item in enumerable)
                {
                    if (i >= 128)
                    {
                        sb.AppendLine(indent + "  <collection-truncated>");
                        break;
                    }
                    DumpObject(sb, item, "[" + i.ToString(CultureInfo.InvariantCulture) + "]", depth + 1, maxDepth, visited);
                    i++;
                }
                sb.AppendLine(indent + "  enumerated=" + i.ToString(CultureInfo.InvariantCulture));
                return;
            }

            Component component = value as Component;
            if (component != null)
                sb.AppendLine(indent + label + " " + DescribeUnityObject(component));
            else
                sb.AppendLine(indent + label + " type=" + type.FullName);

            DumpDeclaredMembers(sb, value, label, depth, maxDepth, visited);
        }

        private static void DumpDeclaredMembers(StringBuilder sb, object value, string label, int depth, int maxDepth, HashSet<object> visited)
        {
            if (value == null || depth > maxDepth)
                return;

            Type rootType = value.GetType();
            for (Type current = rootType; current != null; current = current.BaseType)
            {
                if (current == typeof(MonoBehaviour)
                    || current == typeof(Behaviour)
                    || current == typeof(Component)
                    || current == typeof(UnityEngine.Object)
                    || current == typeof(object))
                    break;

                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

                foreach (FieldInfo field in current.GetFields(flags).OrderBy(f => f.Name, StringComparer.Ordinal))
                {
                    object memberValue;
                    try { memberValue = field.GetValue(value); }
                    catch (Exception ex)
                    {
                        sb.AppendLine(new string(' ', (depth + 1) * 2) + label + "." + field.Name + "=<read-error:" + ex.GetType().Name + ">");
                        continue;
                    }

                    DumpObject(sb, memberValue, label + "." + field.Name, depth + 1, maxDepth, visited);
                }

                foreach (PropertyInfo property in current.GetProperties(flags).OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    if (!property.CanRead || property.GetIndexParameters().Length != 0)
                        continue;

                    object memberValue;
                    try { memberValue = property.GetValue(value, null); }
                    catch
                    {
                        continue;
                    }

                    DumpObject(sb, memberValue, label + "." + property.Name, depth + 1, maxDepth, visited);
                }
            }
        }

        private static bool IsSimple(object value)
        {
            if (value == null)
                return true;

            Type type = value.GetType();
            return value is string
                || type.IsPrimitive
                || type.IsEnum
                || value is decimal
                || value is Guid
                || value is Vector2
                || value is Vector3
                || value is Vector4
                || value is Color
                || value is Rect;
        }

        private static string DescribeTransform(Transform tf)
        {
            return "path=" + Quote(GetHierarchyPath(tf))
                + " activeSelf=" + tf.gameObject.activeSelf
                + " activeInHierarchy=" + tf.gameObject.activeInHierarchy
                + " layer=" + tf.gameObject.layer.ToString(CultureInfo.InvariantCulture)
                + " localPos=" + FormatVector(tf.localPosition)
                + " worldPos=" + FormatVector(tf.position)
                + " localScale=" + FormatVector(tf.localScale)
                + " children=" + tf.childCount.ToString(CultureInfo.InvariantCulture);
        }

        private static string DescribeUnityObject(UnityEngine.Object value)
        {
            Component component = value as Component;
            if (component != null)
                return (value.GetType().FullName ?? value.GetType().Name)
                    + "#" + value.GetInstanceID().ToString(CultureInfo.InvariantCulture)
                    + " name=" + Quote(value.name)
                    + " path=" + Quote(GetHierarchyPath(component.transform));

            return (value.GetType().FullName ?? value.GetType().Name)
                + "#" + value.GetInstanceID().ToString(CultureInfo.InvariantCulture)
                + " name=" + Quote(value.name);
        }

        private static string GetHierarchyPath(Transform tf)
        {
            if (tf == null)
                return "<null>";

            List<string> parts = new List<string>();
            Transform current = tf;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.###", CultureInfo.InvariantCulture) + ","
                + value.y.ToString("0.###", CultureInfo.InvariantCulture) + ","
                + value.z.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string SafeValue(object value)
        {
            if (value == null)
                return "<null>";

            try
            {
                IFormattable formattable = value as IFormattable;
                return formattable != null
                    ? formattable.ToString(null, CultureInfo.InvariantCulture)
                    : value.ToString();
            }
            catch
            {
                return "<unprintable:" + value.GetType().FullName + ">";
            }
        }

        private static string Quote(string value)
        {
            return """ + (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace(""", "\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n") + """;
        }

        private void WriteFailure(Exception ex)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("KEEPERS ALERTS — CHURCH PRAY SOUND PROBE");
                sb.AppendLine("ProbeVersion=" + PluginVersion);
                sb.AppendLine("GeneratedUtc=" + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                sb.AppendLine("FAILED=" + ex);
                File.WriteAllText(_reportPath, sb.ToString(), new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
