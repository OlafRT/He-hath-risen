using System.Linq;
using UniSense;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

[InputControlLayout(
    stateType = typeof(DualSenseBTHIDInputReport),
    displayName = "PS5 Controller (Bluetooth)")]
[Preserve]
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class DualSenseGamepadBTHID : DualSenseGamepadHID
{
    // The marker that uniquely identifies a Bluetooth DualSense in the HID capabilities JSON
    const string BTCapabilityMarker = "\"inputReportSize\":78";

#if UNITY_EDITOR
    static DualSenseGamepadBTHID()
    {
        Initialize();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        // Register with capability matcher as primary mechanism
        InputSystem.RegisterLayout<DualSenseGamepadBTHID>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithManufacturer("Sony.+Entertainment")
                .WithCapability("vendorId", 0x54C)
                .WithCapability("productId", 0xCE6)
                .WithCapability("inputReportSize", 78));

        // Hook the layout-finding callback as a reliable fallback.
        // This fires whenever Unity needs to decide which layout to use for a device,
        // so it catches cases where the matcher alone doesn't win (e.g. already-connected devices).
        InputSystem.onFindLayoutForDevice += FindLayoutForBT;

        // Handle BT DualSenses that were already connected before our code ran.
        // Removing them from Unity's device list causes the native Windows HID backend
        // to re-report them on the next update, at which point our callback above
        // intercepts and selects the correct BT layout.
        var wrongLayoutDevices = InputSystem.devices
            .OfType<DualSenseGamepadHID>()
            .Where(d => !(d is DualSenseGamepadBTHID) && IsBluetooth(d.description.capabilities))
            .ToArray();

        foreach (var device in wrongLayoutDevices)
        {
            Debug.Log("[DualSenseGamepadBTHID] Forcing re-add of BT DualSense to apply correct layout");
            InputSystem.RemoveDevice(device);
        }
    }

    static string FindLayoutForBT(ref InputDeviceDescription description, string matchedLayout, InputDeviceExecuteCommandDelegate executeCommandDelegate)
    {
        if (IsBluetooth(description.capabilities) &&
            (matchedLayout == "DualSenseGamepadHID" ||
             matchedLayout == "DualShockGamepad"    ||
             matchedLayout == "Gamepad"))
        {
            Debug.Log("[DualSenseGamepadBTHID] Overriding to BT layout");
            return "DualSenseGamepadBTHID";
        }
        return null;
    }

    static bool IsBluetooth(string capabilities)
        => !string.IsNullOrEmpty(capabilities) && capabilities.Contains(BTCapabilityMarker);
}
