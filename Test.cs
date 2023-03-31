using GameReaderCommon;
using SimHub.Plugins;
using System;

/* Tension test sliders
 ; https://github.com/blekenbleu/SimHub-profiles/blob/main/Fake8.shsds
 ; f0:	PWM period (usec / 50) 1 - 10000
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
	public class Test		// modulate PWM percent
	{
		bool start;
		byte state;										// 0=reset, 1=rise, 2=hold, 3=fall, 4=wait
		byte max, min;
		internal byte verbosity;
		byte[] cmd, buffer;								// cmd[0] is Arduino PWM command, cmd[1] is PWM 7-bit value
		ushort rise, hold, fall;
		ushort count, period, granularity, damping, attenuation;
		int error, range;
		Fake8 F8;
		Fake7 F7;

		/// <summary>
		/// SimHub.Logging.Current.Info() with prefix
		/// </summary>
		private bool Info(string str)
		{
			SimHub.Logging.Current.Info(F7.old = "Test." + str);						// bool Info()
			return true;
		}

		/// <summary>
        /// Apply high-pass IIR to PWM
        /// </summary>
		private bool PWM(int c)
		{
			int d = c - buffer[0];
			int lp = c + (d * granularity + ((granularity + damping) >>1)) / (granularity + damping);
			buffer[0] = (byte)(1 > lp ? 1 : lp);
			d = c + (d * granularity + ((granularity + attenuation) >> 1)) / (granularity + attenuation);
			d = (byte)(1 > d ? 1 : (100 < d) ? 100 : d);
			if (d != cmd[1])
			{
				cmd[1] = (byte)d;
				if (2 == verbosity)
					F8.CustomWrite($"{c}\t");	// e.g. gnuplot unfiltered vs filtered
				F8.TryWrite(cmd, 2);
				cmd[1] = (byte)c;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Custom Serial settings from strings; prepare Run() state machine;  update Arduino PWM
		/// https://github.com/blekenbleu/Arduino-Blue-Pill/tree/main/PWM_FullConfiguration
		/// </summary>
		internal bool State(byte index)
		{
			bool ret = true;

			if (0 == index)									// f0: PWM period: 1 = 500 Usec
			{
				uint value = Default(index);
				byte[] ard = new byte[3];

				ard[2] = (byte)(127 & value);
				value >>= 7;
				ard[1] = (byte)(127 & value);
				value >>= 7;
				ard[0] = (byte)((7 << 5) | (3 & value));	// Ardiono case 7: 3-byte 16-bit PWM period
				return F8.TryWrite(ard, 3);
			}
			if (1 == index)									 // f1: max PWM %
			{
				max = (byte)Default(index);
				if (min > max && min > 1)
				{
					Info($"State(min) reduced from {min} to 1");
					min = 1;
				}
				if (max <= min)
				{
					Fake8.msg = $"State(max) increased from {max} to {min+1}";
					F8.CustomWrite(Fake8.msg + "\n");
					max = min;
					max++;
				}
				range = max - min;					// think positive
				if (cmd[1] < max)
					state = 1;
				else if (cmd[1] > min)
					state = 3;
			}
			else if (2 == index)
			{
				min = (byte)Default(index);
				if (min > max && min > 1)
				{
					Info($"State(min) reduced from {min} to 1");
					min = 1;
				}
				if (cmd[1] < min)
					state = 1;
				range = max - min;					// think positive
			}
			else if (3 == index)
				granularity = Default(index);
			else if (4 == index)
				damping = Default(index);
			else if (10 == index)
				attenuation = Default(index);
			else if (9 == index)
			{
				if (verbosity != Default(index))
				{
					byte[] v = { (byte)(1 | cmd[0]), verbosity = (byte)Default(index) };
					F8.TryWrite(v, 2);
				}
			}
			else if (5 == index)
			{
				int pulse = (rise + hold + fall) << 1;

				period = Default(index);
				if (period < pulse)
				{
					Fake8.msg = $"State(period) increased from {period} to {pulse}";
					F8.CustomWrite(Fake8.msg + "\n");
					period = (ushort)pulse;
				}
				if (count > period)
					state = 1;
			}
			else if (6 == index)
			 	rise = Default(index);
			else if (7 == index)
			 	hold = Default(index);
			else if (8 == index)
			 	fall = Default(index);
			else ret = false;
/*
			// for looping
			if (ret)
			{
				if (count < rise + hold)
					state = 2;
				else if (count < rise + hold + fall)
					state = 3;
				else if (count < period)
					state = 4;
				else state = 1;
			}

			if (1 == state)
				count = 0;
 */

//	single cycle, instead of looping

			count = (ushort)(period - fall);
			start = true;
			state = 4;
			for (int i = 0; i < buffer.Length; i++)		// initialize for IIR
				buffer[i] = min;
			return ret;
		}												// State()

		private UInt16 Default(byte i)
		{
			try
			{
				return UInt16.Parse(F7.Settings.Prop[i]);
			}
			catch
			{
				UInt16[] array = { 1146, 57, 1, 23, 23, 22, 28, 28, 29, 1, 0, 0 };

				if (i >= F7.Settings.Prop.Length)
				{
					Info($"Default({i}): invalid index");
					return 1;
				}
				Info($"Default({i}): invalid '{F7.Settings.Prop[i]}'");
				return array[i];
			}
		}

		/// <summary>
		/// [re]initialize state machine settings and restart
		/// </summary>
		internal bool Reset(byte index)
		{
			error = count = state = 0;
			max = (byte)Default(1);
			min = (byte)Default(2);
			granularity = Default(3);
			damping = Default(4);
			attenuation = Default(10);
			period = Default(5);
			rise = Default(6);
			hold = Default(7);
			fall = Default(8);
			verbosity = (byte)Default(9);
			if (period < (range = ((rise + hold + fall) << 1)))
			{
				Info(Fake8.msg  = $"Reset(period) increased from {period} to {range}");
				period = (ushort)range;
			}
			else Fake8.msg = $"Test.Reset({index}) complete.";
			range = max - min;
			return State(index);
		}

		/// <summary>
		/// Test state machine; constant max, min in states 2, 4
		/// Bresenham rise and fall in states 1 and 3
		/// invoke PWM() for each invocation to process high-pass steps
		/// </summary>
		internal bool Run()
		{
			int dy;

			if (0 == state)
				return PWM(cmd[1]);		// continue any high-pass filtering

			count++;
			if ( count >= period)
			{
				if (!start)
				{
					state = 0;
					return PWM(cmd[1]);	// continue any high-pass filtering
				}	
				state = 1;
				error = count = 0;
			}
			if (1 == state)
			{
				if (count > rise)
					state++;
				else {
					start = false;
					if (0 == rise)
						return PWM(max);

					dy = error + range;
					error = dy % rise;
					dy /= rise;
					if (range < (error << 1))
					{
						error -= rise;
						dy++;
					}
					return PWM(cmd[1] + dy);		
				}
			}	
			if (2 == state)
			{
				error = 0;
				if (count > rise + hold)
					state++;
				else return PWM(max);		// F8.TryWrite(cmd, 2);
			}	
			if (3 == state)
			{
				if (count > rise + hold + fall)
					state++;
				else if (0 == fall)
					return PWM(min);
				else
				{
					dy = error + range;
					error = dy % fall;
					dy /= fall;
					if (range < (error << 1))
					{
						error -= fall;
						dy++;
					}
					return PWM(cmd[1] - dy);		
				}
			}	
			return PWM((4 == state) ? min : cmd[1]);		// F8.TryWrite(cmd, 2);
			// could check for invalid state here...
		}

		/// <summary>
		/// Called at plugin manager stop, close/dispose anything needed here!
		/// Plugins are rebuilt at game changes, but NCalc files are not re-read
		/// </summary>
		public void End()
		{
			state = 0;
			byte[] reset = { 0xBF };
			F8.TryWrite(reset, 1);
		}

		/// <summary>
		/// Called at SimHub start then after game changes
		/// </summary>
		public void Init(Fake7 f7, Fake8 f8)
		{
			start = false;
			cmd = new byte[] { (4<<5), 1 };		// Arduino case 4:  7-bit (PWM %)
			buffer = new byte[8];
			F7 = f7;
			F8 = f8;
			Reset(1);		// rise
		}																	// Init()
	}
}
