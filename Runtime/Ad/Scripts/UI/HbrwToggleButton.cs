using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Highbrow.UI
{
    public class HbrwToggleButton : MonoBehaviour
    {
        [Header("Button Settings")]
        public Button button;
        public Image buttonImage;

        [Header("State 1 Settings")]
        public Sprite state1Texture;
        public UnityEvent state1Callback;

        [Header("State 2 Settings")]
        public Sprite state2Texture;
        public UnityEvent state2Callback;

        private bool isState1 = true;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
            }

            UpdateButtonState(false);

            if (button != null)
            {
                button.onClick.AddListener(ToggleState);
            }
        }

        public void ToggleState()
        {
            isState1 = !isState1;
            UpdateButtonState(true);
        }

        public void SetState(bool state1, bool triggerCallback = true)
        {
            isState1 = state1;
            UpdateButtonState(triggerCallback);
        }

        private void UpdateButtonState(bool triggerCallback = true)
        {
            if (isState1)
            {
                if (buttonImage != null && state1Texture != null)
                    buttonImage.sprite = state1Texture;
                if (triggerCallback)
                    state1Callback?.Invoke();
            }
            else
            {
                if (buttonImage != null && state2Texture != null)
                    buttonImage.sprite = state2Texture;
                if (triggerCallback)
                    state2Callback?.Invoke();
            }
        }
    }
}
