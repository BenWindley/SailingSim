Shader "Unlit/WaterShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_DisplacementMap("Displacement Map", 2D) = "" {}
		_NormalMap("Normal Map", 2D) = "" {}
		_Amplitude("Amplitude", Float) = 1

		_PeakColour("Peak Colour", Color) = (0,0,0)
		_BaseColour("Base Colour", Color) = (0,0,0)
		_SkyColour("Sky Colour", Color) = (0,0,0)

		_LightColour("Light Colour", Color) = (0, 0, 0)
		_SpecularPower("Specular Power", Float) = 1
		_SpecularIntensity("Specular Intensity", Float) = 1
		_RefractiveIndexAir("Refractive Index Air", Float) = 1.003
		_RefractiveIndexWater("Refractive Index Water", Float) = 1.333

		_RefractionIntensity("Refraction Intensity", Float) = 1
		_ReflectionIntensity("Reflection Intensity", Float) = 1
		_PathStepCount("Path Count", Int) = 10
		_PathStepLength("Path Step Length", Float) = 0.5
		_PathBias("Path Bias", Float) = 0.5

		_Offset("Mesh Offset", Vector) = (0,0,0,0)
	}
	
	SubShader
	{
		Tags {	"RenderType" = "Opaque" 
				"LightMode" = "ForwardBase"
				"PassFlags" = "OnlyDirectional"
				}
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"

			/*******************/
			/* DATA STRUCTURES */
			/*******************/

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2g
			{
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0; 
			};

			struct g2f
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : COLOR;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD1;
			};

			/*************/
			/* VARIABLES */
			/*************/

			sampler2D _MainTex;
			sampler2D _DisplacementMap;
			sampler2D _NormalMap;
			float4 _MainTex_ST;
			float4 _DisplacementMap_ST;
			sampler2D _NormalMap_ST;

			float4 _LightColour;
			float _SpecularPower;
			float _SpecularIntensity;
			float4 _PeakColour;
			float4 _BaseColour;
			float4 _SkyColour;
			float _Amplitude;

			float _RefractiveIndexAir;
			float _RefractiveIndexWater;

			float _RefractionIntensity;
			float _ReflectionIntensity;
			int _PathStepCount;
			float _PathStepLength;
			float _PathBias;

			float4 _Offset;

			/*******************/
			/* SHADER PROGRAMS */
			/*******************/

			float3 Refract(float3 dir, float3 normal, float index)
			{
				float cosi = clamp(-1, 1, dot(dir, normal));
				float etai = 1;
				float etat = index;
				float3 n = normal;

				if (cosi < 0)
				{
					cosi = -cosi;
				}
				else
				{
					float temp;
					temp = etai;
					etai = etat;
					etat = temp;

					n = -normal;
				}

				float eta = etai / etat;
				float k = 1 - eta * eta * (1 - cosi * cosi);

				return k < 0 ? float3(0, 0, 0) : eta * dir + (eta * cosi - sqrt(k)) * n;
			}

			float3 GetDisplacementAtWorldPosition(float3 world_position)
			{
				return _Amplitude * float4(tex2Dlod(_DisplacementMap, float4(world_position.xz / 150.0f, 0, 0)).xyz, 1);
			}

			float3 GetNormalAtWorldPosition(float3 world_position)
			{
				return _Amplitude * float4(tex2Dlod(_NormalMap, float4(world_position.xz / 150.0f, 0, 0)).xyz, 1);
			}

			float RayMarchIntensity(float3 _refracted_initial_dir, float3 _world_pos)
			{
				float3 dir = _refracted_initial_dir;
				float3 world_pos = _world_pos;
				world_pos.y = GetDisplacementAtWorldPosition(world_pos).y - _PathBias;

				float intensity = 0;

				for (int i = 0; i < _PathStepCount; ++i)
				{
					world_pos += dir * float(_PathStepLength);

					float water_height = GetDisplacementAtWorldPosition(world_pos).y;

					//check if outside water
					if (world_pos.y > water_height)
					{
					////get normal at world point
						float3 normal = GetNormalAtWorldPosition(world_pos);
					////refract with air index
						dir = Refract(dir, normal, _RefractiveIndexAir);
						return intensity + float(_PathStepCount - i) / float(_PathStepCount);
					}
					else
					{
					////check distance from light direction and water surface
						intensity += (1 - (water_height - world_pos.y)) / float(_PathStepCount);
					}
				}

				return intensity;
			}

			// VERTEX SHADER //
			v2g vert(appdata v)
			{
				v2g o;

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = v.normal;
				o.vertex = v.vertex;

				return o;
			} 

			[maxvertexcount(3)]
			void geom(triangle v2g input[3], inout TriangleStream<g2f> TriStream)
			{
				g2f output;
				float4 displacement;

				for(int i = 0; i < 3; ++i)
				{
					output.uv = input[i].uv;
					displacement = _Amplitude * float4(GetDisplacementAtWorldPosition(mul(unity_ObjectToWorld, input[i].vertex + _Offset.xyz)).xyz, 1);
					output.normal = GetNormalAtWorldPosition(mul(unity_ObjectToWorld, input[i].vertex + _Offset.xyz)).xyz;
					output.worldPos = mul(unity_ObjectToWorld, input[i].vertex + displacement.xyz);
					output.vertex = float4(output.worldPos,1);
					output.worldPos += _Offset.xyz;
					
					output.vertex = UnityObjectToClipPos(output.vertex);

					TriStream.Append(output);
				}
				TriStream.RestartStrip();
			}

			// FRAGMENT SHADER //
			fixed4 frag(g2f i) : SV_Target
			{
				float3 light_direction = normalize(float3(-_WorldSpaceLightPos0.xyz));
				float3 normal = GetNormalAtWorldPosition(i.worldPos).xyz;

				//// Diffuse Shading
				fixed4 col = lerp(_BaseColour, _PeakColour, clamp((i.worldPos.y + _Amplitude) / (2 * _Amplitude), -1, 1));
				
				float3 cam_pos = _WorldSpaceCameraPos;
				float3 cam_direction = normalize(i.worldPos - cam_pos);
				
				// Reflection and Refraction Shading
				float fresnel = clamp(1.0 - dot(normal, -cam_direction), 0.0, 1.0);
				fresnel = pow(fresnel, 5.0f) * 0.65f;
				
				float3 refraction_vector = Refract(cam_direction, normal, _RefractiveIndexWater / _RefractiveIndexAir);
				float4 reflection_col = float4(DecodeHDR(UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflect(cam_direction, normal)), unity_SpecCube0_HDR), 0);

				float4 refraction_col = RayMarchIntensity(normalize(refraction_vector), i.worldPos) * _SkyColour;
				refraction_col = smoothstep(0, 1.0f, refraction_col);

				// Specular Shading
				float specular_factor = dot(normalize(reflect(light_direction, normal)), -cam_direction );
				float specular_colour = specular_factor > 0 ? pow(specular_factor, _SpecularPower) * _SpecularIntensity : 0;
				
				return _LightColour * specular_colour + 0.8f * col + _RefractionIntensity * lerp(refraction_col, _ReflectionIntensity * reflection_col, fresnel);
			}
			ENDCG
		}
	}
}
