using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class RadioButton : ToggleButton
    {
        private static readonly Dictionary<string, List<RadioButton>> RadioButtonDictionary = new Dictionary<string, List<RadioButton>>();
        private string _radioGroup;

        public string RadioGroup
        {
            get { return _radioGroup; }
            set
            {
                RadioButton.RemoveFromList(_radioGroup, this);
                _radioGroup = value;
                RadioButton.AddToList(_radioGroup, this);
            }
        }

        private static void AddToList(string radioGroup, RadioButton radioButton)
        {
            if (string.IsNullOrEmpty(radioGroup))
                return;
            if (!RadioButtonDictionary.ContainsKey(radioGroup))
                RadioButtonDictionary.Add(radioGroup, new List<RadioButton>());

            var list = RadioButtonDictionary[radioGroup];
            list.Add(radioButton);
        }

        private static void RemoveFromList(string radioGroup, RadioButton radioButton)
        {
            if (string.IsNullOrEmpty(radioGroup))
                return;
            if (!RadioButtonDictionary.ContainsKey(radioGroup))
                return;
            var list = RadioButtonDictionary[radioGroup];
            list.Remove(radioButton);
            if (list.Count == 0)
                RadioButtonDictionary.Remove(radioGroup);
        }

        private static void SetGroupChecked(string radioGroup, RadioButton radioButton)
        {
            if (!RadioButtonDictionary.ContainsKey(radioGroup))
                return;
            var list = RadioButtonDictionary[radioGroup];
            foreach (var button in list)
            {
                button.IsChecked = button == radioButton;
            }
        }

        protected override void OnChecked(bool e)
        {
            base.OnChecked(e);
            RadioButton.SetGroupChecked(RadioGroup, this);
        }
    }
}