// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Packages/com.meta.utilities.watch-window/LICENSE

using UnityEngine;
using UnityEngine.UIElements;

namespace Meta.Utilities.WatchWindow
{
    internal class VerticalResizer : MouseManipulator
    {
        private Vector2 m_lastPos;
        private bool m_active;
        private VisualElement m_parent;
        private VisualElement m_element;
        private float m_minHeight;

        public VerticalResizer(VisualElement splitView, float minWidth)
        {
            m_minHeight = minWidth;
            m_parent = splitView.parent;
            m_element = splitView;
            m_active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        public void ApplyDelta(float delta)
        {
            var oldDimension = m_element.resolvedStyle.height;
            var newDimension = oldDimension + delta;

            if (newDimension < oldDimension && newDimension < m_minHeight)
                newDimension = m_minHeight;

            var maxLength = m_parent.resolvedStyle.height;
            if (newDimension > oldDimension && newDimension > maxLength)
                newDimension = maxLength;

            m_element.style.height = newDimension;
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (CanStartManipulation(e))
            {
                m_lastPos = e.localMousePosition;

                m_active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_active || !target.HasMouseCapture())
                return;

            var diff = e.localMousePosition - m_lastPos;
            ApplyDelta(diff.y);

            e.StopPropagation();
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!m_active || !target.HasMouseCapture() || !CanStopManipulation(e))
                return;

            m_active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }
    }
}
