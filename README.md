# Direct Drive harness tension tester
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

Since [not designed](https://support.electricscooterparts.com/support/discussions/topics/1000087804)
for use as [traction motors](https://en.wikipedia.org/wiki/Traction_motor),
![](https://s3.amazonaws.com/cdn.freshdesk.com/data/helpdesk/attachments/production/1061847567/original/ZBdjpUecHVhGhRT2PKtmCvsTbPvkehl3zg.png)  
... some information is lacking for use stalled:
- What PWM frequency minimizes coil whine while not compromising torque control?
- [What range of PWM values is safe and useful](https://www.allaboutcircuits.com/textbook/semiconductors/chpt-11/pulse-width-modulation/)?
- How quickly can motor torque slew?  
  Substantial changes will involve appreciable shaft rotation, generating back EMF.  
- Are brief predistortion pulses wanted for more responsive torque application and release?
   - If so, how much and for how long?

At least 8 slider controls are wanted:
- test cycle frequency (1/period)
- torque rise time (% of period)
- torque fall time (% of period)
- PWM frequency    ( ~ 20kHz)
- PWM min %
- PWM max %
- predistortion amplitude (% of (maz-min) change)
- predistortion duration (PWM cycles)
