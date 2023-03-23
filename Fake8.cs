using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Timers;

/* Tension test sliders
 ; https://github.com/blekenbleu/SimHub-profiles/blob/main/Fake8.shsds
 ; f0:	PWM period (usec / 50) 1 - 40000
 ; f1:  max PWM % 0 - 100
 ; f2:  min PWM % 0 - 10
 ; f3:  predistortion amplitude: % of PWM change 0 - 64
 ; f4:  predistortion count (number of PWM cycles) 0 - 64
 ; f5:  Test period count (number of SimHub Run() invocations 60 - 600
 ; f6:  Test rise count 0 - 64
 ; f7:  Test hold count 0 - 64
 ; f8:  Test fall count 0 - 64
 ; f9:
 */

namespace Fake8plugin
{
	public class Fake8		// handle real Arduino USB COM por with 8-bit data
	{
		private static SerialPort Arduino;
		private Test T;
		static bool ongoing;						// Arduino port statu flag
		static internal string Ini = "Fake7.";		// SimHub's property prefix for this plugin
		private string[] Prop, b4;					// kill duplicated Custom Serial messages
		private byte[] cmd;							// 8-bit bytes to Arduino
		private int col;
		static internal string msg;					// user feedback property:  Msg_Arduino

		/// <summary>
		/// wraps SimHub.Logging.Current.Info() with prefix
		/// </summary>
		private static bool Info(string str)
		{
			SimHub.Logging.Current.Info("Fake8." + str);								// bool Info()
			return true;
		}

		internal bool Recover(SerialPort port)
		{
			if (port.IsOpen)
				return true;
			else
			{
				try
				{
					port.DiscardInBuffer();
				}
				catch {/* ignore */}
				try
				{
					port.DiscardOutBuffer();
				}
				catch {/* ignore */}
				try
				{
					port.Open();
					return true;
				}
				catch { /* ignore */ }
			}

			return false;
		}

		internal void TryWrite(byte[] cmd, byte length)
		{
			try
			{
				Arduino.Write(cmd, 0, length);
			}
			catch (Exception wex)
			{
				bool first = false;

				if (ongoing)
				{
					Info(msg = "TryWrite():  " + wex.Message);
					first = true;
				}
				if (ongoing = Recover(Arduino))
					Info(msg = "TryWrite():  Arduino connection restored");
				else if (first)
					CustomWrite(msg + "\n");
			} 
		}

		/// <summary>
		// Called one time per game data update, contains all normalized game data,
		/// Update run time
		/// </summary>
		bool once;											// avoid flooding log with duplicate messages
		internal void Run(PluginManager pluginManager)
		{
			if (null == Prop)
			{
				if (once)
					Info(msg = "Run(): null Prop[]");
				once = false;
				return;
			}
			once = true;
			Recover(Fake7.CustomSerial);
			if (ongoing = Recover(Arduino))
			{
				try
				{
					string s = Arduino.ReadExisting();
					int l = s.Length;

					if (0 < l)
					{
						col += l;
						if (140 > col)
							s = s.Substring(0, l - 1) + " ";
						else col = 0;
						CustomWrite(s);
					}
				}
				catch (Exception rex)
				{
					Info(msg = "Arduino.ReadExisting():  " + rex.Message );
					CustomWrite(msg + "\n");	// inform Custom Serial of Arduino exception
					ongoing = false;		// recover in TryWrite();
				}
			}
				
			for (byte i = 0; i < Prop.Length; i++)
			{
				string prop = pluginManager.GetPropertyValue(Ini + Prop[i])?.ToString();

				if (null == prop || 0 == prop.Length || (prop.Length == b4[i].Length && prop == b4[i]))
					continue;
				uint value = uint.Parse(prop);

				b4[i] = prop;
//							https://github.com/blekenbleu/Arduino-Blue-Pill/tree/main/PWM_FullConfiguration
				if (0 == i)			// case 7: 16-bit PWM period
				{
					cmd[2] = (byte)(127 & value);
					value >>= 7;
					cmd[1] = (byte)(127 & value);
					value >>= 7;
					cmd[0] = (byte)((7 << 5) | (3 & value));
					TryWrite(cmd, 3);
				}
				else if (1 == i) 	// case 4: 7-bit PWM %
				{
					cmd[1] = (byte)(127 & value);
					cmd[0] = (byte)(4 << 5);
					TryWrite(cmd, 2);
				}
			}
			T.Run();
		}

		/// <summary>
		/// Called by Run()
		/// </summary>
		internal void CustomWrite(string received)
		{
			try
			{
				Fake7.CustomSerial.Write(received);
			}
			catch (Exception e)
			{
				if (Fake7.running)
					Fake7.old = "Custom Serial:  " + e.Message + $" during Fake7.CustomSerial.Write({received})";
				if (Fake7.running = Recover(Fake7.CustomSerial))
					Fake7.old = "Custom Serial connection recovered";
			}
		}

		/// <summary>
		/// Called at plugin manager stop, close/dispose anything needed here!
		/// Plugins are rebuilt at game changes, but NCalc files are not re-read
		/// </summary>
		public void End(Fake7 F7)
		{
			T.End();
			ongoing = false;
			F7.Close(Arduino);
		}

		/// <summary>
		/// Called at SimHub start then after game changes
		/// </summary>
		public Test Init(PluginManager pluginManager, Fake7 F7)
		{
			msg = "[waiting]";
			once = true;
			col = 0;
			Arduino = new SerialPort();
			cmd = new byte[4];
			T = new Test();

// read properties and configure

			string parms = pluginManager.GetPropertyValue(Fake7.Ini + "parms")?.ToString();

			if (null != parms && 0 < parms.Length)
			{
				Prop = parms.Split(',');
				if (5 < Prop.Length)
				{
					b4 = new string[Prop.Length];
					for (byte i = 0; i < Prop.Length; i++)
						b4[i] = "";
				}
				else Info($"Init():  {Fake7.Ini + "parms"}.Length {Prop.Length} < expected 6");
			}
			else Info("Init():  missing " + Fake7.Ini + "parms");

			string pill = pluginManager.GetPropertyValue(Fake7.Ini + "pill")?.ToString();
			if (null != pill && 0 < pill.Length)
			{													// launch serial port
				ongoing = F7.Fopen(Arduino, pill);
				T.Init(F7, this);
			}
			else F7.Sports(Fake7.Ini + (msg = "Custom Serial 'F8pill' missing from F8.ini"));
			return T;
		}																			// Init()
	}
}
