using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace KeepersAlerts
{
    internal sealed class HudPresentation
    {
        private const float IndicatorX = 174f;
        private const float FirstIndicatorY = 24f;
        private const float SecondIndicatorY = -8f;

        private readonly Type _ui2dSpriteType;
        private readonly MethodInfo _makePixelPerfect;

        private Component _hud;
        private GameObject _container;
        private GameObject _corpseIndicator;
        private GameObject _confessionIndicator;

        private Sprite _frameSprite;
        private Sprite _bodySprite;
        private Sprite _prayerSprite;

        internal HudPresentation()
        {
            _ui2dSpriteType = ReflectionUtil.FindType("UI2DSprite");
            if (_ui2dSpriteType != null)
            {
                _makePixelPerfect = ReflectionUtil.FindMethod(
                    _ui2dSpriteType,
                    "MakePixelPerfect",
                    0,
                    false);
            }
        }

        internal bool IsHudVisible
        {
            get
            {
                return _hud != null
                    && _hud
                    && _hud.gameObject.activeInHierarchy;
            }
        }

        internal bool EnsureAttached(object hudObject)
        {
            var hud = hudObject as Component;
            if (hud == null || _ui2dSpriteType == null)
                return false;

            if (_hud == hud
                && _container != null
                && _corpseIndicator != null
                && _confessionIndicator != null)
            {
                return true;
            }

            DestroyPrivateObjects();

            var hudLeft = hud.transform.Find("hud left");
            if (hudLeft == null)
                return false;

            if (!EnsureSprites())
                return false;

            _hud = hud;

            _container = new GameObject("KeepersAlerts Indicators");
            _container.layer = hudLeft.gameObject.layer;
            _container.transform.SetParent(hudLeft, false);
            _container.transform.localPosition = Vector3.zero;
            _container.transform.localScale = Vector3.one;

            _corpseIndicator = CreateIndicator(
                _container.transform,
                "Keeper's Alerts — Corpse",
                _bodySprite,
                24,
                24,
                80);

            _confessionIndicator = CreateIndicator(
                _container.transform,
                "Keeper's Alerts — Confession",
                _prayerSprite,
                20,
                23,
                82);

            if (_corpseIndicator == null || _confessionIndicator == null)
            {
                DestroyPrivateObjects();
                return false;
            }

            _corpseIndicator.SetActive(false);
            _confessionIndicator.SetActive(false);
            return true;
        }

        internal void SetStates(bool corpseWaiting, bool confessionAvailable)
        {
            if (_corpseIndicator == null || _confessionIndicator == null)
                return;

            if (corpseWaiting)
            {
                _corpseIndicator.transform.localPosition =
                    new Vector3(IndicatorX, FirstIndicatorY, 0f);

                if (confessionAvailable)
                {
                    _confessionIndicator.transform.localPosition =
                        new Vector3(IndicatorX, SecondIndicatorY, 0f);
                }
            }
            else if (confessionAvailable)
            {
                _confessionIndicator.transform.localPosition =
                    new Vector3(IndicatorX, FirstIndicatorY, 0f);
            }

            if (_corpseIndicator.activeSelf != corpseWaiting)
                _corpseIndicator.SetActive(corpseWaiting);

            if (_confessionIndicator.activeSelf != confessionAvailable)
                _confessionIndicator.SetActive(confessionAvailable);
        }

        internal void Dispose()
        {
            DestroyPrivateObjects();
        }

        private bool EnsureSprites()
        {
            if (_frameSprite == null)
                _frameSprite = ReflectionUtil.FindLoadedSprite("icon_frame_techno");
            if (_bodySprite == null)
                _bodySprite = ReflectionUtil.FindLoadedSprite("body_01");
            if (_prayerSprite == null)
                _prayerSprite = ReflectionUtil.FindLoadedSprite("icon_pray_bubble");

            return _frameSprite != null
                && _bodySprite != null
                && _prayerSprite != null;
        }

        private GameObject CreateIndicator(
            Transform parent,
            string name,
            Sprite iconSprite,
            int iconWidth,
            int iconHeight,
            int baseDepth)
        {
            var root = new GameObject(name);
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one;

            var frame = CreateSpriteChild(
                root.transform,
                "Background",
                _frameSprite,
                34,
                26,
                baseDepth);

            var icon = CreateSpriteChild(
                root.transform,
                "Icon",
                iconSprite,
                iconWidth,
                iconHeight,
                baseDepth + 1);

            if (frame == null || icon == null)
            {
                UnityEngine.Object.Destroy(root);
                return null;
            }

            return root;
        }

        private Component CreateSpriteChild(
            Transform parent,
            string name,
            Sprite sprite,
            int width,
            int height,
            int depth)
        {
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            Component widget;
            try
            {
                widget = go.AddComponent(_ui2dSpriteType);
            }
            catch
            {
                UnityEngine.Object.Destroy(go);
                return null;
            }

            if (widget == null
                || !ReflectionUtil.TryWrite(widget, "sprite2D", sprite)
                || !ReflectionUtil.TryWrite(widget, "width", width)
                || !ReflectionUtil.TryWrite(widget, "height", height)
                || !ReflectionUtil.TryWrite(widget, "depth", depth))
            {
                UnityEngine.Object.Destroy(go);
                return null;
            }

            return widget;
        }

        private void DestroyPrivateObjects()
        {
            if (_container != null)
                UnityEngine.Object.Destroy(_container);

            _container = null;
            _corpseIndicator = null;
            _confessionIndicator = null;
            _hud = null;
        }
    }

    internal sealed class ConfessionTransientPresenter
    {
        private readonly Type _newBodyArrivedGuiType;
        private readonly Type _ui2dSpriteType;
        private readonly MethodInfo _displayMethod;
        private readonly MonoBehaviour _owner;
        private readonly Action<string, Exception> _onFailure;

        private GameObject _stockPanel;
        private GameObject _clonePanel;
        private Component _cloneGui;
        private Coroutine _showRoutine;
        private Sprite _prayerSprite;

        internal ConfessionTransientPresenter(
            MonoBehaviour owner,
            Action<string, Exception> onFailure)
        {
            _owner = owner;
            _onFailure = onFailure;
            _newBodyArrivedGuiType = ReflectionUtil.FindType("NewBodyArrivedGUI");
            _ui2dSpriteType = ReflectionUtil.FindType("UI2DSprite");
            _displayMethod = ReflectionUtil.FindMethod(
                _newBodyArrivedGuiType,
                "Display",
                0,
                false);
        }

        internal void Show()
        {
            if (_owner == null || _showRoutine != null)
                return;

            _showRoutine = _owner.StartCoroutine(ShowWhenFree());
        }

        internal void Dispose()
        {
            if (_showRoutine != null && _owner != null)
            {
                try
                {
                    _owner.StopCoroutine(_showRoutine);
                }
                catch
                {
                }
            }

            _showRoutine = null;

            if (_clonePanel != null)
                UnityEngine.Object.Destroy(_clonePanel);

            _clonePanel = null;
            _cloneGui = null;
            _stockPanel = null;
        }

        private IEnumerator ShowWhenFree()
        {
            if (!EnsurePrepared())
            {
                _showRoutine = null;
                yield break;
            }

            while ((_stockPanel != null && _stockPanel.activeSelf)
                || (_clonePanel != null && _clonePanel.activeSelf))
            {
                yield return null;
            }

            try
            {
                if (_cloneGui != null)
                    _displayMethod.Invoke(_cloneGui, null);
            }
            catch (Exception ex)
            {
                if (_onFailure != null)
                    _onFailure("confession transient", ex);
            }

            _showRoutine = null;
        }

        private bool EnsurePrepared()
        {
            if (_clonePanel != null
                && _cloneGui != null
                && _stockPanel != null)
            {
                return true;
            }

            if (_newBodyArrivedGuiType == null
                || _ui2dSpriteType == null
                || _displayMethod == null)
            {
                return false;
            }

            var stock = FindStockPanel();
            if (stock == null || stock.transform.parent == null)
                return false;

            _prayerSprite = _prayerSprite
                ?? ReflectionUtil.FindLoadedSprite("icon_pray_bubble");
            if (_prayerSprite == null)
                return false;

            var clone = UnityEngine.Object.Instantiate(stock);
            clone.name = "KeepersAlerts_ConfessionArrivedPanel";
            clone.transform.SetParent(stock.transform.parent, false);
            clone.transform.localPosition = stock.transform.localPosition;
            clone.transform.localRotation = stock.transform.localRotation;
            clone.transform.localScale = stock.transform.localScale;

            var cloneGui = clone.GetComponent(_newBodyArrivedGuiType);
            if (cloneGui == null)
            {
                UnityEngine.Object.Destroy(clone);
                return false;
            }

            var bodyImage = clone.transform.Find("BodyImage");
            if (bodyImage != null)
                bodyImage.gameObject.SetActive(false);

            var plusText = clone.transform.Find("PlusText");
            if (plusText != null)
                plusText.gameObject.SetActive(false);

            var prayerGo = new GameObject("PrayerImage");
            prayerGo.layer = clone.layer;
            prayerGo.transform.SetParent(clone.transform, false);
            prayerGo.transform.localPosition = new Vector3(6f, 5f, 0f);
            prayerGo.transform.localScale = Vector3.one;

            Component prayerWidget;
            try
            {
                prayerWidget = prayerGo.AddComponent(_ui2dSpriteType);
            }
            catch
            {
                UnityEngine.Object.Destroy(clone);
                return false;
            }

            if (prayerWidget == null
                || !ReflectionUtil.TryWrite(
                    prayerWidget,
                    "sprite2D",
                    _prayerSprite)
                || !ReflectionUtil.TryWrite(prayerWidget, "width", 26)
                || !ReflectionUtil.TryWrite(prayerWidget, "height", 30)
                || !ReflectionUtil.TryWrite(prayerWidget, "depth", 26))
            {
                UnityEngine.Object.Destroy(clone);
                return false;
            }

            clone.SetActive(false);

            _stockPanel = stock;
            _clonePanel = clone;
            _cloneGui = cloneGui;
            return true;
        }

        private GameObject FindStockPanel()
        {
            UnityEngine.Object[] objects;
            try
            {
                objects = Resources.FindObjectsOfTypeAll(_newBodyArrivedGuiType);
            }
            catch
            {
                return null;
            }

            for (var i = 0; i < objects.Length; i++)
            {
                var component = objects[i] as Component;
                if (component == null)
                    continue;

                var parent = component.transform.parent;
                if (string.Equals(
                        component.gameObject.name,
                        "BodyArrivedPanel",
                        StringComparison.Ordinal)
                    && parent != null
                    && string.Equals(
                        parent.name,
                        "NewBodyArrivedPanel",
                        StringComparison.Ordinal))
                {
                    return component.gameObject;
                }
            }

            return null;
        }
    }
}
