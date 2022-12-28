using MemUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace High_On_Life_Trainer
{
	internal class HOLMemory
	{
		public MemoryWatcherList Watchers { get; private set; }
		public bool IsInitialized { get; private set; } = false;

		private Process proc;

		public bool UpdateState()
		{
			if (!IsHooked() || !IsInitialized)
			{
				IsInitialized = false;
				Hook();
				Thread.Sleep(1000);
				return false;
			}

			try
			{
				Watchers.UpdateAll(proc);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}

			return true;
		}

		private bool IsHooked()
		{
			return proc != null && !proc.HasExited;
		}

		private void Hook()
		{
			List<Process> processList = Process.GetProcesses().ToList().FindAll(x => Regex.IsMatch(x.ProcessName, "Oregon.*-Shipping"));
			if (processList.Count == 0)
			{
				proc = null;
				return;
			}
			proc = processList[0];

			if (IsHooked())
			{
				IsInitialized = Initialize();
			}
		}

		private bool Initialize()
		{

			IntPtr localPlayerBase, worldBase;
			try
			{
				SignatureScanner scanner = new SignatureScanner(proc, proc.MainModule.BaseAddress, proc.MainModule.ModuleMemorySize);
				if (!GetLocalPlayerBasePtr(scanner, out localPlayerBase) || !GetWorldBasePtr(scanner, out worldBase))
				{
					return false;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}

			DeepPointer xPosPtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x290, 0x1E0);
			DeepPointer yPosPtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x290, 0x1E4);
			DeepPointer zPosPtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x290, 0x1E8);
			DeepPointer xVelPtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x290, 0x140);
			DeepPointer yVelPtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x290, 0x144);
			DeepPointer zVelPtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x290, 0x148);
			DeepPointer godModePtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x5A);
			DeepPointer movementModePtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x288, 0x170);
			DeepPointer flySpeedPtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x968, 0x160, 0x0, 0x12C);
			DeepPointer accelerationPtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x968, 0x160, 0x0, 0x1BC);
			DeepPointer cheatFlyingPtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x288, 0x3A4);
			DeepPointer collisionEnabledPtr = new DeepPointer(localPlayerBase, 0x30, 0x2C8, 0x5C);
			DeepPointer vLookPtr = new DeepPointer(localPlayerBase, 0x30, 0x2B0);
			DeepPointer hLookPtr = new DeepPointer(localPlayerBase, 0x30, 0x2B4);
            DeepPointer healthPtr = new DeepPointer(localPlayerBase, 0x30, 0x120, 0x58, 0xD0, 0x40, 0x3C);

            DeepPointer gameSpeedPtr = new DeepPointer(worldBase, 0x30, 0x270, 0x2E8);

			Watchers = new MemoryWatcherList() {
				new MemoryWatcher<float>(xPosPtr) { Name = "xPos" },
				new MemoryWatcher<float>(yPosPtr) { Name = "yPos" },
				new MemoryWatcher<float>(zPosPtr) { Name = "zPos" },
				new MemoryWatcher<float>(xVelPtr) { Name = "xVel" },
				new MemoryWatcher<float>(yVelPtr) { Name = "yVel" },
				new MemoryWatcher<float>(zVelPtr) { Name = "zVel" },
				new MemoryWatcher<byte>(godModePtr) { Name = "godMode" },
				new MemoryWatcher<byte>(movementModePtr) { Name = "movementMode" },
				new MemoryWatcher<float>(flySpeedPtr) { Name = "flySpeed" },
				new MemoryWatcher<float>(accelerationPtr) { Name = "acceleration" },
				new MemoryWatcher<byte>(cheatFlyingPtr) { Name = "cheatFlying" },
				new MemoryWatcher<byte>(collisionEnabledPtr) { Name = "collisionEnabled" },
				new MemoryWatcher<float>(vLookPtr) { Name = "vLook" },
				new MemoryWatcher<float>(hLookPtr) { Name = "hLook" },
				new MemoryWatcher<float>(gameSpeedPtr) { Name = "gameSpeed" },
                new MemoryWatcher<float>(healthPtr) { Name = "health" }
            };

			try
			{
				Watchers.UpdateAll(proc);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}

			return true;
		}

		public void Write(string name, byte[] bytes)
		{
			if (!IsHooked() || !IsInitialized || !Watchers[name].DeepPtr.DerefOffsets(proc, out IntPtr addr))
			{
				return;
			}

			try
			{
				_ = proc.WriteBytes(addr, bytes);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public void Write(string name, float fValue)
		{
			Write(name, BitConverter.GetBytes(fValue));
		}

		public void Write(string name, bool boolValue)
		{
			Write(name, BitConverter.GetBytes(boolValue));
		}

		public void Write(string name, byte bValue)
		{
			Write(name, new byte[] { bValue });
		}

		private bool GetLocalPlayerBasePtr(SignatureScanner scanner, out IntPtr result)
		{
			result = IntPtr.Zero;

			var pattern = new SigScanTarget("48 89 3D ?? ?? ?? ?? C7 05 ?? ?? ?? ?? 00 00 B4 42 89 05");
			var instructionOffset = 0x3;
			var location = scanner.Scan(pattern);
			if (location == IntPtr.Zero) return false;
			int offset = proc.ReadValue<int>((IntPtr)location + instructionOffset);
			result = (IntPtr)location + offset + instructionOffset + 0x4;
			Console.WriteLine(result.ToString("X16"));
			return true;
			
		}

		private bool GetWorldBasePtr(SignatureScanner scanner, out IntPtr result)
		{
			result = IntPtr.Zero;

			var pattern = new SigScanTarget("48 8B 1D ?? ?? ?? ?? 48 85 DB 74 33 41 B0 01");
			var instructionOffset = 0x3;
			var location = scanner.Scan(pattern);
			if (location == IntPtr.Zero) return false;
			int offset = proc.ReadValue<int>((IntPtr)location + instructionOffset);
			result = (IntPtr)location + offset + instructionOffset + 0x4;
			Console.WriteLine(result.ToString("X16"));
			return true;
		}
	}
}