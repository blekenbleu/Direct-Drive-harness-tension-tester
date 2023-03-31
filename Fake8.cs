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
		private Fake7 F7;
		bool ongoing;						// Arduino port statu flag
		static internal string Ini = "F7.";		// SimHub's property prefix for this plugin
		private string[] Prop, b4;					// kill duplicated Custom Serial messages
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

		/// <summary>
		/// Called by Run(), TryWrite()
		/// </summary>
		internal bool CustomWrite(string received)
		{
			try
			{
				F7.CustomSerial.Write(received);
				return true;
			}
			catch (Exception e)
			{
				if (F7.running)
					F7.old = "Custom Serial:  " + e.Message + $" during Write({received})";
				if (F7.running = Recover(F7.CustomSerial))
					try
					{
						F7.CustomSerial.Write(received);
						F7.old = "Custom Serial connection recovered";
						return true;
					}
					catch { F7.running = false; }
			}
			return false;
		}

		internal void Newline()
		{
			if (2 == T.verbosity || 0 < col && CustomWrite("\n"))
				col = 0;
		}

		internal bool TryWrite(byte[] cmd, byte length)
		{
			try
			{
				Arduino.Write(cmd, 0, length);
				return true;
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
				{
					try
					{
						Arduino.Write(cmd, 0, length);
						Info(msg = "TryWrite():  Arduino connection restored");
						return true;
					}
					catch
					{
						ongoing = false;
						CustomWrite(msg + "\n");
					}
				}
				else if (first)
					CustomWrite(msg + "\n");
			}
			return false;
		}					// TryWrite()

		/// <summary>
		// Called one time per game data update, contains all normalized game data,
		/// Update run time
		/// </summary>
		bool once;											// avoid flooding log with duplicate messages
		internal void Run()
		{
			if (null == Prop)
			{
				if (once)
					Info(msg = "Run(): null Prop[]");
				once = false;
				return;
			}

			once = true;
			Recover(F7.CustomSerial);
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
					ongoing = false;			// recover in TryWrite();
					Info(msg = "Arduino.ReadExisting():  " + rex.Message );
					CustomWrite(msg + "\n");	// inform Custom Serial of Arduino exception
				}
			}
				
			T.Run();
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
		public Test Init(PluginManager pluginManager, Fake7 f7)
		{
			msg = "[waiting]";
			once = true;
			col = 0;
			Arduino = new SerialPort();
			F7 = f7;
			T = new Test();

// read properties and configure

			string parms = pluginManager.GetPropertyValue(F7.Ini + "parms")?.ToString();

			if (null != parms && 0 < parms.Length)
			{
				Prop = parms.Split(',');
				if (5 < Prop.Length)
				{
					b4 = new string[Prop.Length];
					for (byte i = 0; i < Prop.Length; i++)
						b4[i] = "";
				}
				else Info($"Init():  {F7.Ini + "parms"}.Length {Prop.Length} < expected 6");
			}
			else Info("Init():  missing " + F7.Ini + "parms");

			string pill = pluginManager.GetPropertyValue(F7.Ini + "pill")?.ToString();
			if (null != pill && 0 < pill.Length)
			{													// launch serial port
				ongoing = F7.Fopen(Arduino, pill);
				T.Init(F7, this);
			}
			else F7.Sports(F7.Ini + (msg = "Custom Serial 'F8pill' missing from F8.ini"));
			return T;
		}																			// Init()
	}
}
