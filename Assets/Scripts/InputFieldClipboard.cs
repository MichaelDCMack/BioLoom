using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// On Linux Unity reports Ctrl+key by physical (QWERTY) position, so on a Dvorak
// layout Ctrl+V arrives as Ctrl+Period and TMP_InputField never pastes. This adds
// paste/copy/cut on the physical keys that carry V/C/X on Dvorak, and a PasteInto
// method for a Paste button that works on any layout.
public class InputFieldClipboard : MonoBehaviour
{
    public bool dvorakShortcuts = true;

    public void PasteInto(TMP_InputField field)
    {
        field.text = Filter(field, GUIUtility.systemCopyBuffer);
        field.onEndEdit.Invoke(field.text);
    }

    void OnGUI()
    {
        Handle(Event.current);
    }

    public void Handle(Event e)
    {
        if (!dvorakShortcuts || e.type != EventType.KeyDown)
        {
            return;
        }

        bool ctrlOnly = (e.modifiers & EventModifiers.Control) != 0
                        && (e.modifiers & (EventModifiers.Alt | EventModifiers.Shift)) == 0;
        if (!ctrlOnly)
        {
            return;
        }

        TMP_InputField field = FocusedField();
        if (field == null)
        {
            return;
        }

        switch (e.keyCode)
        {
            case KeyCode.Period: // Dvorak V
            {
                Replace(field, GUIUtility.systemCopyBuffer);
                e.Use();
                break;
            }
            case KeyCode.I: // Dvorak C
            {
                GUIUtility.systemCopyBuffer = Selected(field);
                e.Use();
                break;
            }
            case KeyCode.B: // Dvorak X
            {
                GUIUtility.systemCopyBuffer = Selected(field);
                Replace(field, "");
                e.Use();
                break;
            }
        }
    }

    static TMP_InputField FocusedField()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
        {
            return null;
        }

        TMP_InputField field = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
        return field != null && field.isFocused && !field.readOnly ? field : null;
    }

    static void SelectionRange(TMP_InputField field, out int start, out int end)
    {
        start = Mathf.Min(field.selectionStringAnchorPosition, field.selectionStringFocusPosition);
        end = Mathf.Max(field.selectionStringAnchorPosition, field.selectionStringFocusPosition);
        start = Mathf.Clamp(start, 0, field.text.Length);
        end = Mathf.Clamp(end, 0, field.text.Length);
    }

    static string Selected(TMP_InputField field)
    {
        SelectionRange(field, out int start, out int end);
        return field.text.Substring(start, end - start);
    }

    static void Replace(TMP_InputField field, string insert)
    {
        SelectionRange(field, out int start, out int end);
        string text = field.text.Substring(0, start) + insert + field.text.Substring(end);
        string filtered = Filter(field, text);
        int caret = Mathf.Min(start + Filter(field, insert).Length, filtered.Length);

        field.text = filtered;
        field.stringPosition = caret;
        field.selectionStringAnchorPosition = caret;
        field.selectionStringFocusPosition = caret;
    }

    // keep what the field itself would accept
    static string Filter(TMP_InputField field, string s)
    {
        s = s.Replace("\r", "").Replace("\n", "").Trim();

        if (field.characterValidation == TMP_InputField.CharacterValidation.Integer)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in s)
            {
                if (char.IsDigit(c) || (c == '-' && sb.Length == 0))
                {
                    sb.Append(c);
                }
            }
            s = sb.ToString();
        }

        if (field.characterLimit > 0 && s.Length > field.characterLimit)
        {
            s = s.Substring(0, field.characterLimit);
        }

        return s;
    }
}
