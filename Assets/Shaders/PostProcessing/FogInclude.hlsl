#ifndef FOG_INCLUDED
#define FOG_INCLUDED

uniform float3 _SunColor;
uniform float3 _SunDirection;

uniform float3 _Extinction;
uniform float3 _Inscattering;
uniform float3 _FogColor;
            
uniform float _MistHeight;
uniform float _MistPow;
uniform float _MistHeightOffset;

float intGetMist(float distance, float3 dir, float cameraHeight)
{
	return (_MistHeight / _MistPow) * exp(-cameraHeight * _MistPow) * (1.0 - exp(-distance * dir.y * _MistPow)) / dir.y;
}

float3 applyFogWithMist(float3 c, float distance, float3 dir, float3 cameraPos)
{
	float sunAmount = 1.0 - (dot(dir, _SunDirection) + 1.0) / 2.0;
	float mist = clamp(intGetMist(distance, dir + 0.0001, cameraPos.y - _MistHeightOffset), 0., 100000);

	float3 extCol = exp(-mist * _Extinction.rgb);
	float3 insCol = exp(-mist * _Inscattering.rgb);
	float3 fogColor = _FogColor + _SunColor * sunAmount * sunAmount;
	return c * extCol + fogColor * (1 - insCol);
}

#endif