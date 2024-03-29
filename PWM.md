## PWM: digital dither for efficient (*but noisy*) analog control
Pulse density modulation is one alternative to [Pulse Width Modulation](https://www.picotech.com/library/application-note/some-power-pwm-drivers-for-electric-dc-motors),  
with the advantage of being more easily lossless,  
since presence or absense of fixed width pulses at fixed intervals is more robustly detected,  
but for driving typical relatively slowly reponding loads,  
PWM offers finer granularity at lower implementation cost, and consequently gets used for
* hobby servos
* illumination control
* motor control
  - many think of PWM motor control in terms of speed, but PWM more directly controls DC motor torque.
* class D audio amplifiers

Many low cost microcontrollers implement PWM in firmware,  
but both STM32 and ESP32 support PWM with full hardware control.  
Consequently, instead of using the
 [generic Arduino PWM](https://docs.arduino.cc/tutorials/generic/secrets-of-arduino-pwm) library,  
this implementation will investigate so-called *Fast PWM*, which exploits that hardware.  
As [noted](https://github.com/khoih-prog/STM32_PWM#why-do-we-need-this-STM32_PWM-library),
these libraries have similar functions, simplifying porting among platforms: 
- [ESP32_FastPWM](https://reference.arduino.cc/reference/en/libraries/esp32_fastpwm/)
	- [on GitHub](https://github.com/khoih-prog/ESP32_FastPWM)
- [STM32_PWM](https://github.com/khoih-prog/STM32_PWM)
	- [on GitHub](https://github.com/khoih-prog/STM32_PWM)
	- It has an undocumented dependency on [jfturcot/SimpleTimer](https://github.com/jfturcot/SimpleTimer),  
		.. which points to [an Arduino Playground entry](https://playground.arduino.cc/Code/SimpleTimer/)
        that is actually for another version by Marcello Romani,  
		from which it is derived.&nbsp;
	  Marcello has a considerably updated (likely incompatible) version [also on GitHub](https://github.com/marcelloromani/Arduino-SimpleTimer).  

FWIW, both **SimpleTimer** in [Arduino Library Manager](https://docs.arduino.cc/learn/starting-guide/software-libraries)
 seem totally different and incompatible;&nbsp; go figure..  

### 16 Mar 2023:&nbsp; *working STM32 hardware* [PWM_FullConfiguration](https://github.com/blekenbleu/Arduino-Blue-Pill/tree/main/PWM_FullConfiguration) 
drives Blue Pill LED, lacks predistortion controls, 
should be ported to [Blue_ASCII_PWM_LED](https://github.com/blekenbleu/Arduino-Blue-Pill/tree/main/Blue_ASCII_PWM_LED)
