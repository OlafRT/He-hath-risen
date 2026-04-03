using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuNavigator : MonoBehaviour
{
    [SerializeField] private GamepadInput gamepadInput;

    // Leave empty to auto-discover all Buttons in this GameObject's children.
    // Populate manually if you want a specific order.
    [SerializeField] private Button[] buttons;

    private int _selectedIndex = 0;

    void Start()
    {
        if (buttons == null || buttons.Length == 0)
            buttons = GetComponentsInChildren<Button>();

        SelectButton(0);
    }

    void Update()
    {
        if (gamepadInput == null || buttons == null || buttons.Length == 0) return;

        if (gamepadInput.dpadDown)
        {
            _selectedIndex = (_selectedIndex + 1) % buttons.Length;
            SelectButton(_selectedIndex);
        }
        if (gamepadInput.dpadUp)
        {
            _selectedIndex = (_selectedIndex - 1 + buttons.Length) % buttons.Length;
            SelectButton(_selectedIndex);
        }
        if (gamepadInput.jumpPressed)
        {
            buttons[_selectedIndex].onClick.Invoke();
        }
    }

    private void SelectButton(int index)
    {
        _selectedIndex = index;
        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(buttons[_selectedIndex].gameObject);
    }
}
