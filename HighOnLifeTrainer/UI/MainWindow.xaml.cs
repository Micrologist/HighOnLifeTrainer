using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using High_On_Life_Trainer.UI;
using Button = System.Windows.Controls.Button;

namespace High_On_Life_Trainer
{
	public partial class MainWindow : Window
	{
		private readonly HOLTrainer trainer;
		private GlobalKeyboardHook kbHook;
		private readonly float[] gameSpeeds = new float[4] { 1f, 2f, 4f, 0.5f };
		private bool shouldAcceptKeystrokes = true;
		private readonly Dictionary<string, Keys> defaultKeybinds = new Dictionary<string, Keys>()
		{
			{ "god", Keys.F1 },
			{ "noclip", Keys.F2 },
			{ "speed", Keys.F3 },
			{ "store", Keys.F5 },
			{ "teleport", Keys.F6 },
			{ "ascend", Keys.Space },
			{ "descend", Keys.C }
		};
		private Dictionary<string, Keys> keybinds = new Dictionary<string, Keys>();
		private Dictionary<Keys, Action> keybindActions;
		private Keys ascendKey;
		private Keys descendKey;


		public MainWindow()
		{
			InitializeComponent();
			InitializeKeyboardHook();
			trainer = new HOLTrainer();
			DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = new TimeSpan(10 * 10000) };
			timer.Tick += UIUpdateTick;
			timer.Start();
		}

		public void SetKeybinds(Dictionary<string, Keys> newKeybinds)
		{
			kbHook.HookedKeys.Clear();
			keybindActions.Clear();
			string keybindStore = "";

			foreach (KeyValuePair<string, Keys> keybind in newKeybinds)
			{
				switch (keybind.Key)
				{
					case "god":
						keybindActions.Add(keybind.Value, () => godBtn_Click(null, null));
						SetKeybindText(godBtn, keybind.Value);
						keybindStore += "god,";
						break;
					case "noclip":
						keybindActions.Add(keybind.Value, () => noclipBtn_Click(null, null));
						SetKeybindText(noclipBtn, keybind.Value);
						keybindStore += "noclip,";
						break;
					case "speed":
						keybindActions.Add(keybind.Value, () => gameSpeedBtn_Click(null, null));
						SetKeybindText(gameSpeedBtn, keybind.Value);
						keybindStore += "speed,";
						break;
					case "store":
						keybindActions.Add(keybind.Value, () => saveBtn_Click(null, null));
						SetKeybindText(saveBtn, keybind.Value);
						keybindStore += "store,";
						break;
					case "teleport":
						keybindActions.Add(keybind.Value, () => teleBtn_Click(null, null));
						SetKeybindText(teleBtn, keybind.Value);
						keybindStore += "teleport,";
						break;
					case "ascend":
						ascendKey = keybind.Value;
						keybindStore += "ascend,";
						break;
					case "descend":
						descendKey = keybind.Value;
						keybindStore += "descend,";
						break;
					default:
						break;
				}
				kbHook.HookedKeys.Add(keybind.Value);
				keybindStore += (int)keybind.Value + ",";
			}
			keybindStore = keybindStore.Substring(0, keybindStore.LastIndexOf(","));
			keybinds = newKeybinds;
			try
			{
				File.WriteAllText("HOLTrainer_Keybinds.cfg", keybindStore);
			}
			catch (UnauthorizedAccessException)
			{
				_ = System.Windows.MessageBox.Show("Keybindings could not be saved.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		private void InitializeKeyboardHook()
		{
			kbHook = new GlobalKeyboardHook();
			kbHook.KeyDown += HandleKeyDown;
			kbHook.KeyUp += HandleKeyUp;
			keybindActions = new Dictionary<Keys, Action>();
			string keybindStore = "";
			if (File.Exists("HOLTrainer_Keybinds.cfg"))
			{
				try
				{
					keybindStore = File.ReadAllText("HOLTrainer_Keybinds.cfg");
				}
				catch (Exception)
				{
					System.Windows.MessageBox.Show("Keybindings could not be read.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				}

				string[] keybindArray = keybindStore.Split(',');

				if (keybindArray.Length == defaultKeybinds.Count * 2)
				{
					Dictionary<string, Keys> savedKeybinds = new Dictionary<string, Keys>();
					for (int i = 0; i < defaultKeybinds.Count * 2; i += 2)
					{
						savedKeybinds.Add(keybindArray[i], (Keys)int.Parse(keybindArray[i + 1]));
					}
					SetKeybinds(savedKeybinds);
					return;
				}
			}
			SetKeybinds(defaultKeybinds);
		}

		private void HandleKeyDown(object sender, KeyEventArgs e)
		{
			if (!shouldAcceptKeystrokes)
			{
				return;
			}
			if (keybindActions.ContainsKey(e.KeyCode))
			{
				keybindActions[e.KeyCode]();
			}
			else if (e.KeyCode == ascendKey)
			{
				trainer.ShouldAscend = true;
			}
			else if (e.KeyCode == descendKey)
			{
				trainer.ShouldDescend = true;
			}
		}
	

		private void HandleKeyUp(object sender, KeyEventArgs e)
		{
			if (!shouldAcceptKeystrokes)
			{
				return;
			}

			if (e.KeyCode == ascendKey)
			{
				trainer.ShouldAscend = false;
				Console.WriteLine("stopascend");
			}
			else if (e.KeyCode == descendKey)
			{
				trainer.ShouldDescend = false;
			}
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			trainer.ShouldAbort = true;
			kbHook.unhook();
		}

		private void UIUpdateTick(object sender, EventArgs e)
		{
			positionBlock.Text =
				  (trainer.XPos / 100).ToString("0.00") + "\n"
				+ (trainer.YPos / 100).ToString("0.00") + "\n"
				+ (trainer.ZPos / 100).ToString("0.00");
			speedBlock.Text = trainer.Vel.ToString("0.00") + " m/s";
			SetLabel(trainer.ShouldGod, godLabel);
			SetLabel(trainer.ShouldNoclip, noclipLabel);
			SetLabel(trainer.ShouldAmmo, ammoLabel);
			gameSpeedLabel.Content = trainer.SelectedGameSpeed.ToString("0.0") + "x";
            healthBlock.Text = trainer.Health + " hp";
        }

		private void SetLabel(bool state, System.Windows.Controls.Label label)
		{
			label.Content = state ? "ON" : "OFF";
			label.Foreground = state ? Brushes.Green : Brushes.Red;
		}

		private void SetKeybindText(Button button, Keys key)
		{
			string text = (string)button.Content;
			string keyName = key.ToString();
			if ((int)key >= 48 && (int)key <= 57)
			{
				keyName = keyName.Replace("D", "");
			}
			button.Content = Regex.Replace(text, "\\[.*\\]", "[" + keyName + "]");
		}

		private void teleBtn_Click(object sender, RoutedEventArgs e)
		{
			trainer.ShouldTeleport = true;
		}

		private void saveBtn_Click(object sender, RoutedEventArgs e)
		{
			trainer.ShouldStore = true;
		}

		private void noclipBtn_Click(object sender, RoutedEventArgs e)
		{
			trainer.ShouldNoclip = !trainer.ShouldNoclip;
		}

		private void godBtn_Click(object sender, RoutedEventArgs e)
		{
			trainer.ShouldGod = !trainer.ShouldGod;
		}

		private void ammoBtn_Click(object sender, RoutedEventArgs e)
		{
			trainer.ShouldAmmo = !trainer.ShouldAmmo;
		}

		private void gameSpeedBtn_Click(object sender, RoutedEventArgs e)
		{
			float old = trainer.SelectedGameSpeed;
			trainer.SelectedGameSpeed = gameSpeeds[(Array.IndexOf(gameSpeeds, old) + 1) % gameSpeeds.Length];
		}

		private void editKeybindBtn_Click(object sender, RoutedEventArgs e)
		{
			shouldAcceptKeystrokes = false;
			KeybindWindow kbWindow = new KeybindWindow(this, keybinds);
			_ = kbWindow.ShowDialog();
			shouldAcceptKeystrokes = true;
		}
	}
}
