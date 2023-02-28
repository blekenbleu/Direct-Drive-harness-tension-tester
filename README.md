# Direct Drive sim racing harness tension test rig
 Arduino sketch and SimHub Custom Serial for [PWM scooter motor torque](https://www.picotech.com/library/application-note/some-power-pwm-drivers-for-electric-dc-motors)

Several Sim racing harness tensioners use either stepper motors or hobby/robot servo motors,  
either choice being IMO suboptimal:
- gears generate noise and kill responsiveness
- neither directly applies tension; they instead simply move some amount.

A few tensioners already use PWM control of DC motors,
where PWM % directly relates to stall torque (tension).  
A stalled electric motor generates no back [EMF](https://en.wikipedia.org/wiki/Counter-electromotive_force),
consequently using much lower than rated motor Voltage.  
[Electric scooter motors](https://www.amazon.com/dp/B09KRGZX3G?tag=racedep-20)
seemingly deliver appropriate torque for direct drive harness tensioning, [as discussed here](https://www.racedepartment.com/threads/2dof-harness-tensionner-with-fly-ptmover.194331/page-9#post-3531954).  
![](https://m.media-amazon.com/images/I/71aZ-9HlhdL._SL1500_.jpg)  
... driven by [BTS7960 43A Motor Drivers](https://electropeak.com/learn/interfacing-bts7960-43a-high-power-motor-driver-module-with-arduino/):
![](https://electropeak.com/learn/wp-content/uploads/2021/01/BTS7960-43A-Driver-Module.jpg)  

Since scooter motors are [not designed](https://support.electricscooterparts.com/support/discussions/topics/1000087804)
for use as [traction motors](https://en.wikipedia.org/wiki/Traction_motor),
![](https://s3.amazonaws.com/cdn.freshdesk.com/data/helpdesk/attachments/production/1061847567/original/ZBdjpUecHVhGhRT2PKtmCvsTbPvkehl3zg.png)  
... some information is lacking to use mostly stalled:
- What PWM frequency minimizes coil whine while not compromising torque control?
- [What range of PWM values is safe and useful](https://www.allaboutcircuits.com/textbook/semiconductors/chpt-11/pulse-width-modulation/)?
- How responsively can tension (motor torque) slew?  
  Substantial changes will involve appreciable shaft rotation, generating back EMF.  
- Are brief predistortion pulses wanted for more responsive torque application and release?
   - If so, how much and for how long?

Testing will employ this waveform, sampled at SimHub 60Hz rate:  
![](test.png)  

At least 10 controls are wanted for testing:
- test period  (60 = 1 second)
- rise time (0 to period/3)
- hold time (0 to period/3)
- fall time (0 to period/3)
- max signal magnitude
- min signal magniitude

Signals to Blue Pill:  
- Testing sample values driving PWM %
- PWM frequency &nbsp; ( ~ 20kHz?)
- predistortion amplitude (% of sample value changes) to compensate slew rate limits:
  ![](predistort.jpg)
- predistortion duration (Blue Pill Arduino loop() cycle count)  
  worst case, scooter motor may want brief negative predistortion to relax tension,  
  consuming both sides of an "H" PWM driver per motor:  
  ![](https://www.allaboutcircuits.com/uploads/articles/simple-H-bridge.jpg)  
- optional reference measurements to echo, along with sample values, for capture and plotting,
  where reference tension measurements may be captured from a sim brake pedal load cell.
  
This wants [**multi-character control for SimHub Custom serial devices**](https://github.com/blekenbleu/blekenbleu.github.io/tree/master/Arduino/blek2char) to handle that many variables,  
or perhaps its somewhat simpler predecessor, [Blue2charServo](https://github.com/blekenbleu/blekenbleu.github.io/tree/master/Arduino/Blue2charServo).
