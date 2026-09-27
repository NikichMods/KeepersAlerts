using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace KeepersAlerts
{
    internal static class ReflectionUtil
    {
        internal const BindingFlags AnyInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        internal const BindingFlags AnyStatic =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullName, false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }

            return null;
        }

        internal static MethodInfo FindMethod(
            Type type,
            string name,
            int parameterCount,
            bool isStatic)
        {
            if (type == null)
                return null;

            var flags = isStatic ? AnyStatic : AnyInstance;
            MethodInfo found = null;

            foreach (var method in type.GetMethods(flags))
            {
                if (!string.Equals(method.Name, name, StringComparison.Ordinal)
                    || method.GetParameters().Length != parameterCount)
                {
                    continue;
                }

                if (found != null)
                    return null;

                found = method;
            }

            return found;
        }

        internal static bool TryRead(object owner, string name, out object value)
        {
            value = null;
            if (owner == null)
                return false;

            for (var type = owner.GetType(); type != null; type = type.BaseType)
            {
                try
                {
                    var field = type.GetField(
                        name,
                        AnyInstance | BindingFlags.DeclaredOnly);
                    if (field != null)
                    {
                        value = field.GetValue(owner);
                        return true;
                    }

                    var prop = type.GetProperty(
                        name,
                        AnyInstance | BindingFlags.DeclaredOnly);
                    if (prop != null
                        && prop.CanRead
                        && prop.GetIndexParameters().Length == 0)
                    {
                        value = prop.GetValue(owner, null);
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        internal static bool TryReadStatic(
            Type type,
            string name,
            out object value)
        {
            value = null;
            if (type == null)
                return false;

            for (var current = type; current != null; current = current.BaseType)
            {
                try
                {
                    var field = current.GetField(
                        name,
                        AnyStatic | BindingFlags.DeclaredOnly);
                    if (field != null)
                    {
                        value = field.GetValue(null);
                        return true;
                    }

                    var prop = current.GetProperty(
                        name,
                        AnyStatic | BindingFlags.DeclaredOnly);
                    if (prop != null
                        && prop.CanRead
                        && prop.GetIndexParameters().Length == 0)
                    {
                        value = prop.GetValue(null, null);
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        internal static bool TryWrite(object owner, string name, object value)
        {
            if (owner == null)
                return false;

            for (var type = owner.GetType(); type != null; type = type.BaseType)
            {
                try
                {
                    var field = type.GetField(
                        name,
                        AnyInstance | BindingFlags.DeclaredOnly);
                    if (field != null)
                    {
                        field.SetValue(owner, value);
                        return true;
                    }

                    var prop = type.GetProperty(
                        name,
                        AnyInstance | BindingFlags.DeclaredOnly);
                    if (prop != null
                        && prop.CanWrite
                        && prop.GetIndexParameters().Length == 0)
                    {
                        prop.SetValue(owner, value, null);
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        internal static string ReadString(object owner, string name)
        {
            object value;
            return TryRead(owner, name, out value)
                ? value as string
                : null;
        }

        internal static bool ReadBool(object owner, string name)
        {
            object value;
            if (!TryRead(owner, name, out value) || !(value is bool))
                return false;

            return (bool)value;
        }

        internal static IEnumerable ReadEnumerable(object owner, string name)
        {
            object value;
            return TryRead(owner, name, out value)
                ? value as IEnumerable
                : null;
        }

        internal static bool IsUnityAlive(object obj)
        {
            if (obj == null)
                return false;

            var unity = obj as UnityEngine.Object;
            if ((object)unity == null)
                return true;

            return unity != null;
        }

        internal static Sprite FindLoadedSprite(string exactName)
        {
            if (string.IsNullOrEmpty(exactName))
                return null;

            Sprite[] sprites;
            try
            {
                sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            }
            catch
            {
                return null;
            }

            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                if (sprite != null
                    && string.Equals(
                        sprite.name,
                        exactName,
                        StringComparison.Ordinal))
                {
                    return sprite;
                }
            }

            return null;
        }
    }
}
