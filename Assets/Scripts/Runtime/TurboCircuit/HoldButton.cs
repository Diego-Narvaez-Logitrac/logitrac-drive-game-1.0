using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LogitracDrive.TurboCircuit
{
    /// <summary>Press-and-hold button. Tracks each pointer separately so two thumbs (one per button) work together.</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        readonly HashSet<int> _pointers = new HashSet<int>();

        public bool IsHeld => _pointers.Count > 0;
        public event Action<bool> HeldChanged;

        public void OnPointerDown(PointerEventData e)
        {
            bool was = IsHeld;
            _pointers.Add(e.pointerId);
            if (!was) HeldChanged?.Invoke(true);
        }

        // Pointer-up is delivered to the pressed object even if the finger slid off it (like pointer capture).
        public void OnPointerUp(PointerEventData e)
        {
            if (_pointers.Remove(e.pointerId) && !IsHeld) HeldChanged?.Invoke(false);
        }

        public void Release()
        {
            if (!IsHeld) return;
            _pointers.Clear();
            HeldChanged?.Invoke(false);
        }

        void OnDisable() => Release();
        void OnApplicationFocus(bool focus) { if (!focus) Release(); }
    }
}
