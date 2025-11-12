
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SnivysUI
{
    public class LeftRightTextbox : MonoBehaviour
    {
        [SerializeField]
        private Text textbox;
        [SerializeField]
        private Button leftButton;
        [SerializeField]
        private Button rightButton;
        [SerializeField]
        private RectTransform background;
        public Button.ButtonClickedEvent GetOnLeftClick()
        {
            return leftButton.onClick;
        }

        public Button.ButtonClickedEvent GetOnRightClick()
        {
            return rightButton.onClick;
        }

        public void SetText(string text)
        {
            textbox.text = text;
        }

        public void SetWidth(float width)
        {
            background.sizeDelta = new Vector2(width-15.3043f, 27.952f);
            leftButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-width / 2, 0);
            rightButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(width / 2, 0);
            
        }
    }
}
