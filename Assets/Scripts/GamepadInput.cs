using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The entire gamepad handler. expose publically what you are interested in
/// </summary>
public class GamepadInput : MonoBehaviour
{
    //expose what we want
    public Vector2 leftStick;
    public Vector2 rightStick;
    public bool leftTrigger;
    public bool rightTrigger;
    public bool anyDpad;
    public bool dpadUp;
    public bool dpadDown;

    // --- Button presses (true for exactly one frame) ---
    public bool jumpPressed;    // Cross / A  (buttonSouth)
    public bool startPressed;   // Options / Menu  (startButton)

    // Update is called once per frame
    void Update()
    {
        // Reset single-frame booleans first
        jumpPressed  = false;
        startPressed = false;
        dpadUp       = false;
        dpadDown     = false;

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && gamepad.enabled)
        {
            ReadGamepad(gamepad);
            return;
        }

        // Fallback for generic controllers that Unity detects as a Joystick rather than a Gamepad.
        // Uses the legacy Input system which handles unrecognised HID devices more broadly.
        ReadLegacyFallback();
    }

    void ReadGamepad(Gamepad gamepad)
    {
        if (gamepad.aButton.wasPressedThisFrame)
            Debug.Log("A button pressed!");
        if (gamepad.bButton.wasPressedThisFrame)
            Debug.Log("B button pressed!");
        if (gamepad.xButton.wasPressedThisFrame)
            Debug.Log("X button pressed!");
        if (gamepad.yButton.wasPressedThisFrame)
            Debug.Log("Y button pressed!");
        if (gamepad.buttonNorth.wasPressedThisFrame)
            Debug.Log("North button pressed!");
        if (gamepad.buttonSouth.wasPressedThisFrame)
        {
            Debug.Log("South button pressed!");
            jumpPressed = true;
        }
        if (gamepad.buttonEast.wasPressedThisFrame)
            Debug.Log("East button pressed!");
        if (gamepad.buttonWest.wasPressedThisFrame)
            Debug.Log("West button pressed!");
        if (gamepad.startButton.wasPressedThisFrame)
        {
            Debug.Log("Start button pressed!");
            startPressed = true;
        }
        if (gamepad.circleButton.wasPressedThisFrame)
            Debug.Log("Circle button pressed!");
        if (gamepad.crossButton.wasPressedThisFrame)
            Debug.Log("Cross button pressed!");
        if (gamepad.squareButton.wasPressedThisFrame)
            Debug.Log("Square button pressed!");
        if (gamepad.triangleButton.wasPressedThisFrame)
            Debug.Log("Triangle button pressed!");
        if (gamepad.selectButton.wasPressedThisFrame)
            Debug.Log("Select button pressed!");

        // dpad
        Vector2 dpad = gamepad.dpad.value;
        

        anyDpad  = dpad.magnitude > 0;
        dpadUp   = gamepad.dpad.up.wasPressedThisFrame;
        dpadDown = gamepad.dpad.down.wasPressedThisFrame;

        // left stick
        Vector2 stickInputL = gamepad.leftStick.ReadValue();
        leftStick = stickInputL;
        
        if (gamepad.leftStickButton.wasPressedThisFrame)
            Debug.Log("Left stick button pressed!");

        // right stick
        Vector2 stickInputR = gamepad.rightStick.ReadValue();
        rightStick = stickInputR;
        
        if (gamepad.rightStickButton.wasPressedThisFrame)
            Debug.Log("Right stick button pressed!");

        // triggers
        rightTrigger = gamepad.rightTrigger.isPressed;
        leftTrigger  = gamepad.leftTrigger.isPressed;

        if (rightTrigger) Debug.Log("Right Trigger held down.");
        if (leftTrigger)  Debug.Log("Left Trigger held down.");
        if (gamepad.leftShoulder.isPressed)  Debug.Log("Left Shoulder held down.");
        if (gamepad.rightShoulder.isPressed) Debug.Log("Right Shoulder held down.");
    }

    void ReadLegacyFallback()
    {
        // Legacy Input system maps generic gamepads/joysticks automatically.
        // "joystick button 0" is almost always the bottom face button (A / Cross).
        // "joystick button 7" is commonly Start / Options on most generic pads.
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown("joystick button 0"))
        {
            jumpPressed = true;
            Debug.Log("Jump pressed (legacy fallback)");
        }

        if (Input.GetKeyDown("joystick button 7") || Input.GetKeyDown("joystick button 9"))
        {
            startPressed = true;
            Debug.Log("Start pressed (legacy fallback)");
        }

        // Left stick via legacy axes
        leftStick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // Right trigger — legacy axis 3 on most generic pads (varies by driver)
        rightTrigger = Input.GetAxis("Axis 3") > 0.5f || Input.GetKey("joystick button 5");
        leftTrigger  = Input.GetAxis("Axis 3") < -0.5f || Input.GetKey("joystick button 4");

        // Dpad — legacy
        float dpadH = Input.GetAxis("Axis 6");
        float dpadV = Input.GetAxis("Axis 7");
        anyDpad  = Mathf.Abs(dpadH) > 0.5f || Mathf.Abs(dpadV) > 0.5f;
        dpadUp   = dpadV >  0.5f;
        dpadDown = dpadV < -0.5f;
    }
}
