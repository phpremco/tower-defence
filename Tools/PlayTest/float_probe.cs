// Does this runtime keep float intermediates in double precision? Strict 32-bit float math gives 0 and 5.8499994.
float zero = UnityEngine.Time.realtimeSinceStartup * 0f;
float big = zero + 16777216f;
float sum = big + 1f - big;
float a = zero + 0.65f;
float b = zero + 9f;
float product = a * b;
return "big+1-big=" + sum.ToString("R") + " 0.65*9=" + product.ToString("R") + " (float)(0.65*9 double)=" + ((float)(0.65 * 9.0)).ToString("R");
