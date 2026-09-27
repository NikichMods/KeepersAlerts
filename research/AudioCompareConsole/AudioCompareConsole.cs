// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace KeepersAlertsResearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class AudioCompareConsole : BaseUnityPlugin
    {
        private const string PluginGuid = "nikichmods.keepersalerts.research.audiocompareconsole";
        private const string PluginName = "Keeper's Alerts Audio Compare Console";
        private const string PluginVersion = "0.1.0";

        private const int WindowId = 0x4B41;
        private static readonly Guid SupportedGameMvid = new Guid("6f50b8e7-156b-49ac-bbe8-7505894b2364");

        private MethodInfo _playSound;
        private Rect _windowRect = new Rect(0f, 0f, 360f, 150f);
        private bool _windowPlaced;
        private bool _visible = true;
        private string _status = "Ready";

        private void Awake()
        {
            try
            {
                Assembly game = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "Assembly-CSharp", StringComparison.Ordinal));

                if (game == null)
                    throw new InvalidOperationException("Assembly-CSharp is not loaded.");

                Guid mvid = game.ManifestModule.ModuleVersionId;
                if (mvid != SupportedGameMvid)
                    throw new InvalidOperationException("Unsupported Assembly-CSharp MVID " + mvid);

                Type soundsType = game.GetType("Sounds", false);
                if (soundsType == null)
                    throw new MissingMemberException("Assembly-CSharp", "Sounds");

                _playSound = soundsType
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .SingleOrDefault(m =>
                    {
                        if (!string.Equals(m.Name, "PlaySound", StringComparison.Ordinal))
                            return false;

                        ParameterInfo[] p = m.GetParameters();
                        return p.Length == 4
                            && p[0].ParameterType == typeof(string)
                            && p[1].ParameterType == typeof(Vector2?)
                            && p[2].ParameterType == typeof(bool)
                            && p[3].ParameterType == typeof(float);
                    });

                if (_playSound == null)
                    throw new MissingMethodException("Expected Sounds.PlaySound(string, Vector2?, bool, float) was not found.");

                Logger.LogInfo(PluginName + " " + PluginVersion + " loaded. F10 toggles the two-button test window.");
            }
            catch (Exception ex)
            {
                _status = "DISABLED: host signature mismatch";
                Logger.LogError(PluginName + " disabled: " + ex);
                enabled = false;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || _playSound == null)
                return;

            if (!_windowPlaced)
            {
                _windowRect.x = Mathf.Max(10f, (Screen.width - _windowRect.width) * 0.5f);
                _windowRect.y = Mathf.Max(10f, (Screen.height - _windowRect.height) * 0.35f);
                _windowPlaced = true;
            }

            _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, "Keeper's Alerts — Sound Test");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Bell Single", GUILayout.Height(44f)))
                Play("bell_single");

            if (GUILayout.Button("Chorus Short", GUILayout.Height(44f)))
                Play("chorus_short");

            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label(_status);
            GUILayout.Label("F10: show / hide");

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void Play(string soundId)
        {
            try
            {
                Logger.LogInfo("AUDIO_TEST before sound=" + soundId);

                object result = _playSound.Invoke(
                    null,
                    new object[]
                    {
                        soundId,
                        null,
                        true,
                        0f
                    });

                _status = "Played: " + soundId;
                Logger.LogInfo("AUDIO_TEST after sound=" + soundId
                    + " result=" + (result == null ? "<null>" : result.GetType().FullName));
            }
            catch (Exception ex)
            {
                _status = "FAILED: " + soundId;
                Logger.LogError("AUDIO_TEST failed sound=" + soundId + " error=" + ex);
            }
        }
    }
}
