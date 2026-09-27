// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace KeepersAlertsResearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class CorpseDropOriginProbe : BaseUnityPlugin
    {
        private const string PluginGuid = "nikichmods.keepersalerts.research.corpsedroporigin";
        private const string PluginName = "Keeper's Alerts Corpse Drop Origin Probe";
        private const string PluginVersion = "0.1.0";
        private static readonly Guid SupportedGameMvid = new Guid("6f50b8e7-156b-49ac-bbe8-7505894b2364");

        private Type _wgoType;
        private Type _dockPointType;
        private MethodInfo _getDropPos;
        private string _reportPath;
        private bool _written;

        private void Awake()
        {
            _reportPath = Path.Combine(Paths.BepInExRootPath, "KeepersAlerts-corpse-drop-origin-probe-0.1.0.txt");

            try
            {
                Assembly game = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));
                if (game == null)
                    throw new InvalidOperationException("Assembly-CSharp is not loaded.");

                Guid mvid = game.ManifestModule.ModuleVersionId;
                if (mvid != SupportedGameMvid)
                    throw new InvalidOperationException("Unsupported Assembly-CSharp MVID " + mvid);

                _wgoType = game.GetType("WorldGameObject", true);
                _dockPointType = game.GetType("DockPoint", true);
                _getDropPos = _wgoType.GetMethod("GetDropPos", BindingFlags.Instance | BindingFlags.NonPublic);
                if (_getDropPos == null || _getDropPos.ReturnType != typeof(Vector3) || _getDropPos.GetParameters().Length != 0)
                    throw new MissingMethodException("Expected private WorldGameObject.GetDropPos() was not found.");

                Logger.LogInfo(PluginName + " " + PluginVersion + " loaded; waiting for donkey and morgue endpoints.");
            }
            catch (Exception ex)
            {
                Logger.LogError(PluginName + " disabled: " + ex);
                enabled = false;
                return;
            }

            StartCoroutine(CaptureWhenReady());
        }

        private IEnumerator CaptureWhenReady()
        {
            while (!_written)
            {
                GameObject donkey = FindWgo("[wgo] donkey");
                GameObject throwIn = FindWgo("[wgo] morgue_throw_in");
                GameObject throwOut = FindWgo("[wgo] morgue_throw_out");

                if (donkey != null && throwIn != null && throwOut != null)
                {
                    WriteReport(donkey, throwIn, throwOut);
                    yield break;
                }

                yield return new WaitForSeconds(1f);
            }
        }

        private static GameObject FindWgo(string exactName)
        {
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go != null && string.Equals(go.name, exactName, StringComparison.Ordinal))
                    return go;
            }

            return null;
        }

        private void WriteReport(GameObject donkey, GameObject throwIn, GameObject throwOut)
        {
            try
            {
                Component donkeyWgo = donkey.GetComponent(_wgoType);
                Component inWgo = throwIn.GetComponent(_wgoType);
                Component outWgo = throwOut.GetComponent(_wgoType);
                if (donkeyWgo == null || inWgo == null || outWgo == null)
                    throw new InvalidOperationException("Expected WorldGameObject component missing.");

                Vector3 dropPos = (Vector3)_getDropPos.Invoke(donkeyWgo, null);
                Component[] docks = donkey.GetComponentsInChildren(_dockPointType, true);

                using (var w = new StreamWriter(_reportPath, false))
                {
                    w.WriteLine("KEEPERS ALERTS — CORPSE DROP ORIGIN PROBE");
                    w.WriteLine("ProbeVersion=0.1.0");
                    w.WriteLine("GeneratedUtc=" + DateTime.UtcNow.ToString("O"));
                    w.WriteLine("ModuleVersionId=" + SupportedGameMvid);
                    w.WriteLine("Contract=READ_ONLY_REFLECTION_NO_HARMONY_NO_GRAPH_EXECUTION_NO_MUTATION_NO_SAVE_WRITE");
                    w.WriteLine("Question=What exact source point does donkey.GetDropPos() return, and what DockPoints contribute to it?");
                    w.WriteLine();

                    WriteVector(w, "donkey.tf.position", donkey.transform.position);
                    WriteVector(w, "donkey.GetDropPos()", dropPos);
                    WriteVector(w, "drop_minus_donkey", dropPos - donkey.transform.position);
                    WriteVector(w, "morgue_throw_in", throwIn.transform.position);
                    WriteVector(w, "morgue_throw_out", throwOut.transform.position);
                    WriteVector(w, "drop_minus_throw_in", dropPos - throwIn.transform.position);
                    w.WriteLine("donkey.dock_count=" + docks.Length);

                    MethodInfo dockGetDropPos = _dockPointType.GetMethod("GetDropPos", BindingFlags.Instance | BindingFlags.Public);
                    MethodInfo dockGetActionDir = _dockPointType.GetMethod("GetActionDir", BindingFlags.Instance | BindingFlags.Public);

                    for (int i = 0; i < docks.Length; i++)
                    {
                        Component dock = docks[i];
                        Transform tf = dock.transform;
                        w.WriteLine();
                        w.WriteLine("DOCK[" + i + "].path=" + GetPath(tf));
                        WriteVector(w, "DOCK[" + i + "].localPosition", tf.localPosition);
                        WriteVector(w, "DOCK[" + i + "].position", tf.position);

                        if (dockGetActionDir != null)
                            w.WriteLine("DOCK[" + i + "].actionDir=" + dockGetActionDir.Invoke(dock, null));

                        if (dockGetDropPos != null)
                            WriteVector(w, "DOCK[" + i + "].GetDropPos()", (Vector3)dockGetDropPos.Invoke(dock, null));
                    }
                }

                _written = true;
                Logger.LogInfo("Corpse drop origin report written: " + _reportPath);
                enabled = false;
            }
            catch (Exception ex)
            {
                Logger.LogError("Corpse drop origin capture failed: " + ex);
                enabled = false;
            }
        }

        private static void WriteVector(StreamWriter w, string key, Vector3 v)
        {
            w.WriteLine(key + "=" + v.x.ToString("R") + "," + v.y.ToString("R") + "," + v.z.ToString("R"));
        }

        private static string GetPath(Transform tf)
        {
            string path = tf.name;
            while (tf.parent != null)
            {
                tf = tf.parent;
                path = tf.name + "/" + path;
            }
            return path;
        }
    }
}
