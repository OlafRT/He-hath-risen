using UniSense;
using UnityEngine.InputSystem;

public static class GamepadRumble
{
    // Current motor values — read by ChickController so trigger updates can bundle them in
    public static float CurrentLow  { get; private set; }
    public static float CurrentHigh { get; private set; }

    public static void Set(float low, float high)
    {
        CurrentLow  = low;
        CurrentHigh = high;

        var dualSense = DualSenseGamepadHID.FindCurrent();
        if (dualSense != null)
        {
            dualSense.SetGamepadState(new DualSenseGamepadState
            {
                Motor = new DualSenseMotorSpeed(low, high)
            });
            return;
        }

        var gamepad = Gamepad.current;
        if (gamepad == null) return;
        gamepad.ResumeHaptics();
        gamepad.SetMotorSpeeds(low, high);
    }

    public static void Stop()
    {
        CurrentLow  = 0f;
        CurrentHigh = 0f;

        var dualSense = DualSenseGamepadHID.FindCurrent();
        if (dualSense != null)
        {
            dualSense.SetGamepadState(new DualSenseGamepadState
            {
                Motor = new DualSenseMotorSpeed(0f, 0f)
            });
            return;
        }

        Gamepad.current?.ResetHaptics();
    }
}
