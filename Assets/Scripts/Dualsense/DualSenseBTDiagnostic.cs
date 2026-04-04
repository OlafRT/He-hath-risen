// Temporary diagnostic — drop on any GameObject, connect BT DualSense, press F1.
// Logs the first 25 raw bytes of the HID report so we can read the correct offsets.
// REQUIRES: Project Settings → Player → Other Settings → Allow 'unsafe' Code: ON
// DELETE this script once offsets are confirmed.

using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UniSense;

public class DualSenseBTDiagnostic : MonoBehaviour
{
    bool _requested;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            _requested = true;
    }

    void OnEnable()  => InputSystem.onEvent += OnEvent;
    void OnDisable() => InputSystem.onEvent -= OnEvent;

    unsafe void OnEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!_requested) return;
        if (!(device is DualSenseGamepadHID)) return;
        if (eventPtr.type != StateEvent.Type) return;

        _requested = false;

        StateEvent* se   = StateEvent.From(eventPtr);
        byte*       data = (byte*)se->state;
        int         size = (int)se->stateSizeInBytes;

        var sb = new StringBuilder($"[DualSense BT raw state — {size} bytes]\n");
        for (int i = 0; i < Math.Min(size, 25); i++)
            sb.AppendLine($"  byte[{i:00}] = 0x{data[i]:X2}  ({data[i],3})");

        Debug.Log(sb.ToString());
    }
}
