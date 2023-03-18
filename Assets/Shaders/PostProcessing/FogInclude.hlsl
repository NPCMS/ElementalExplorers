#ifndef FOG_INCLUDED
#define FOG_INCLUDED

uniform float3 _SunColor;
uniform float3 _SunDirection;

uniform float3 _Extinction;
uniform float3 _Inscattering;
uniform float3 _FogColor;
            
half _MistHeight;
half _MistPow;

float intGetMist(float distance, half3 dir, float h)
{
	return (_MistHeight / _MistPow) * exp(-h * _MistPow) * (1.0 - exp(-distance * dir.y * _MistHeight)) / dir.y;
}

float3 applyFogWithMist(half3 c, float distance, half3 dir, float3 cameraPos)
{
	float sunAmount = 1 - (dot(dir, _SunDirection) + 1) / 2;
	float height = dir.y * distance + cameraPos.y;
	float mist = clamp(intGetMist(distance, dir, height), 0, 100000);
	// mist = distance;

	float3 extCol = exp(-mist * _Extinction.rgb);
	float3 insCol = exp(-mist * _Inscattering.rgb);
	float3 fogColor = _FogColor + _SunColor * sunAmount * sunAmount;
	return c * extCol + fogColor * (1 - insCol);
}

#endif