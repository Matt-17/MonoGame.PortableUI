using System;
using System.Collections.Generic;
using System.Linq;
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

        public static int GetGroupValue(string group)
        {
            if (!RadioButtonDictionary.ContainsKey(@group))
            {
                throw new ArgumentOutOfRangeException();
            }
            var radioButtons = RadioButtonDictionary[@group];
            return radioButtons.Select((b, i) => new { RadioButton = b, Index = i }).Single(x => x.RadioButton.IsChecked).Index;
        }

        private static void AddToList(string radioGroup, RadioButton radioButton)
        {
            if (string.IsNullOrEmpty(radioGroup))
                return;
            if (!RadioButtonDictionary.ContainsKey(radioGroup))
            {
                RadioButtonDictionary.Add(radioGroup, new List<RadioButton>());
                radioButton.IsChecked = true;
            }

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
        private bool _isSettingGroup = false;
        private static void SetGroupChecked(string radioGroup, RadioButton radioButton)
        {
            if (!RadioButtonDictionary.ContainsKey(radioGroup))
                return;
            var list = RadioButtonDictionary[radioGroup];
            foreach (var button in list)
            {
                button._isSettingGroup = true;
                button.IsChecked = button == radioButton;
                button._isSettingGroup = false;
            }
        }

        protected override void OnChecked(bool e)
        {
            if (!_isSettingGroup)
                RadioButton.SetGroupChecked(RadioGroup, this);
            base.OnChecked(e);
        }
    }
}