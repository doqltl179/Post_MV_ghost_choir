
//
// 2-D tiling simplex noise with fixed gradients,
// without the analytical derivative.
// This function is implemented as a wrapper to "psrnoise",
// at the minimal cost of three extra additions.
//
float psnoise(float2 pos, float2 per) {
	return psrnoise(pos, per, 0.0);
}


// GLSL mod
float modx(float3 x, float y) {
	return x - floor(x * (1.0 / y)) * y;
}
float modx(float x, float y) {
	return x - floor(x * (1.0 / y)) * y;
}

// Modulo 289, optimizes to code without divisions
float3 mod289(float3 x) {
	return x - floor(x * (1.0 / 289.0)) * 289.0;
}
float mod289(float x) {
	return x - floor(x * (1.0 / 289.0)) * 289.0;
}

// Permutation polynomial (ring size 289 = 17*17)
float3 permute(float3 x) {
	return mod289(((x * 34.0) + 1.0) * x);
}
float permute(float x) {
	return mod289(((x * 34.0) + 1.0) * x);
}

// Hashed 2-D gradients with an extra rotation.
// (The constant 0.0243902439 is 1/41)
float2 rgrad2(float2 p, float rot) {
#if 0
	// Map from a line to a diamond such that a shift maps to a rotation.
	float u = permute(permute(p.x) + p.y) * 0.0243902439 + rot; // Rotate by shift
	u = 4.0 * frac(u) - 2.0;
	// (This vector could be normalized, exactly or approximately.)
	return float2(abs(u) - 1.0, abs(abs(u + 1.0) - 2.0) - 1.0);
#else
	// For more isotropic gradients, sin/cos can be used instead.
	float u = permute(permute(p.x) + p.y) * 0.0243902439 + rot; // Rotate by shift
	u = frac(u) * 6.28318530718; // 2*pi
	return float2(cos(u), sin(u));
#endif
}


//
// 2-D tiling simplex noise with rotating gradients,
// but without the analytical derivative.
//
float psrnoise(float2 pos, float2 per, float rot) {
	// Offset y slightly to hide some rare artifacts
	pos.y += 0.001;
	// Skew to hexagonal grid
	float2 uv = float2(pos.x + pos.y * 0.5, pos.y);

	float2 i0 = floor(uv);
	float2 f0 = frac(uv);
	// Traversal order
	float2 i1 = (f0.x > f0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);

	// Unskewed grid points in (x,y) space
	float2 p0 = float2(i0.x - i0.y * 0.5, i0.y);
	float2 p1 = float2(p0.x + i1.x - i1.y * 0.5, p0.y + i1.y);
	float2 p2 = float2(p0.x + 0.5, p0.y + 1.0);

	// Integer grid point indices in (u,v) space
	i1 = i0 + i1;
	float2 i2 = i0 + float2(1.0, 1.0);

	// Vectors in unskewed (x,y) coordinates from
	// each of the simplex corners to the evaluation point
	float2 d0 = pos - p0;
	float2 d1 = pos - p1;
	float2 d2 = pos - p2;

	// Wrap i0, i1 and i2 to the desired period before gradient hashing:
	// wrap points in (x,y), map to (u,v)
	float3 xw = modx(float3(p0.x, p1.x, p2.x), per.x);
	float3 yw = modx(float3(p0.y, p1.y, p2.y), per.y);
	float3 iuw = xw + 0.5 * yw;
	float3 ivw = yw;

	// Create gradients from indices
	float2 g0 = rgrad2(float2(iuw.x, ivw.x), rot);
	float2 g1 = rgrad2(float2(iuw.y, ivw.y), rot);
	float2 g2 = rgrad2(float2(iuw.z, ivw.z), rot);

	// Gradients dot vectors to corresponding corners
	// (The derivatives of this are simply the gradients)
	float3 w = float3(dot(g0, d0), dot(g1, d1), dot(g2, d2));

	// Radial weights from corners
	// 0.8 is the square of 2/sqrt(5), the distance from
	// a grid point to the nearest simplex boundary
	float3 t = 0.8 - float3(dot(d0, d0), dot(d1, d1), dot(d2, d2));

	// Set influence of each surflet to zero outside radius sqrt(0.8)
	t = max(t, 0.0);

	// Fourth power of t
	float3 t2 = t * t;
	float3 t4 = t2 * t2;

	// Final noise value is:
	// sum of ((radial weights) times (gradient dot vector from corner))
	float n = dot(t4, w);

	// Rescale to cover the range [-1,1] reasonably well
	return 11.0 * n;
}