using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;
using Keys = System.Windows.Forms.Keys;

namespace High_On_Life_Trainer.UI
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class KeybindWindow : Window
	{
		private readonly Dictionary<string, Button> buttons;
		private readonly Dictionary<string, Keys> keybinds = new Dictionary<string, Keys>();
		private Button selectedButton;
		private Keys selectedOldBinding;

		private readonly MainWindow mw;

		public KeybindWindow(MainWindow mw, Dictionary<string, Keys> keybinds)
		{
			InitializeComponent();
			KeyDown += HandleKeyDown;
			this.mw = mw;
			this.keybinds = keybinds;

			buttons = new Dictionary<string, Button>() {
				{ "god", god },
				{ "noclip", noclip },
				{ "speed", speed },
				{ "store", store },
				{ "teleport", teleport },
				{ "ascend", ascend },
				{ "descend", descend },
			};

			foreach (KeyValuePair<string, Keys> kvp in keybinds)
			{
				buttons[kvp.Key].Content = kvp.Value;
			}
		}

		private void HandleKeyDown(object sender, KeyEventArgs e)
		{
			Keys key = (Keys)KeyInterop.VirtualKeyFromKey(e.Key);
			if (selectedButton != null)
			{
				if (keybinds.ContainsValue(key))
				{
					key = selectedOldBinding;
				}
				selectedButton.Content = key;
				keybinds.Add(selectedButton.Name, key);
				selectedButton = null;
			}
		}

		private void KeyBindBtn_Click(object sender, RoutedEventArgs e)
		{
			if (selectedButton != null)
			{
				keybinds[selectedButton.Name] = selectedOldBinding;
				selectedButton.Content = selectedOldBinding;
			}
			Button button = (Button)sender;
			button.Content = "...";
			selectedButton = button;
			selectedOldBinding = keybinds[button.Name];
			_ = keybinds.Remove(button.Name);
		}

		private void SaveBindsBtn_Click(object sender, RoutedEventArgs e)
		{
			if (selectedButton != null)
			{
				keybinds[selectedButton.Name] = selectedOldBinding;
				selectedButton.Content = selectedOldBinding;
			}

			mw.SetKeybinds(keybinds);
			this.Close();
		}
	}
}
