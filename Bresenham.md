
---
Bresenham:&nbsp; rational rise/run substeps and integer cumulative error
---

Tension test PWM modulation period envelope will be straight lines  
with inflection points for `rise`, `hold`, `fall`, `wait` intervals:  
![](https://github.com/blekenbleu/Direct-Drive-harness-tension-tester/raw/main/test.png)  

Too many Bresenham line drawing "explanations" miss the essence:  
![](https://www.cs.helsinki.fi/group/goa/mallinnus/lines/bres1.gif)  
- For lines with `rise <= run`, every X increment will be populated and some Y increments will be zero.
- There is no need to consider fractions, only integer remainders and substeps.
  - each X and Y increment can be considered to have `run` x `run` substeps
- Only possible Y increments are integer multiple of `run`.
- Given current error, the only question is whether `error + rise` is smaller than `run - (error + rise)`.
	- for the above diagram, unit squares have `run` x `run` substeps and *m* == `rise`
	- half of errors will be negative.

```
// run is always positive;  rise can be considered always positive for error calculations

int error = 0, rise = abs(Y_end - Y_start), run = X_end - X_start;

for (X = X_start; X <= X_end; X++)
{
  if (rise > run)
  {
    int dy = error + rise;	// change in Y substeps

    error = dy % run;		// remainder after integer division
    dy /= run;			// integer division for full increments
    if (rise <= (error << 1))
    {
        error -= run;		// more than half-way to next increment
        dy++;
    }
    Y += dy;			// replace with `Y -= dy;` for Y_end < Y_start
  }
  else // rise <= run
  {
    error += rise;
    if (run > (error << 1))	// error more than half way to next full step?
        return;         	// no increment this time
    error -= run;
    Y++;			// replace with `Y--;` for Y_end < Y_start
  }
}
```
