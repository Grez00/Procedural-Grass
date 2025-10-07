Shader "Custom/UnlitGrass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _WindTex ("Wind Texture", 2D) = "white" {}
        _MainColour ("MainColour", Color) = (0, 0, 0, 0)
        _TipColour ("TipColour", Color) = (0, 0, 0, 0)
        _ColorBlendFactor ("Blend Factor", Range(0.0, 1.0)) = 0.5
        _BendFactor ("Bend Factor", Vector) = (1.0, 1.0, 0.0, 0.0)
        _AAFactor ("Ambient Occlusion", float) = 0.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.1
        _Min ("Minimum", Vector) = (0.0, 0.0, 0.0, 0.0)
        _Max ("Maximum", Vector) = (0.0, 0.0, 0.0, 0.0)
        _WindAmplitude ("Wind Amplitude", float) = 0.1
        _WindFrequency ("Wind Frequency", float) = 0.1
        _WindDirection ("Wind Direction", Vector) = (1.0, 1.0, 0.0, 0.0)

    }
    SubShader
    {
        Cull Off

        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags {
				"LightMode" = "ForwardBase"
			}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // make fog work
            #pragma multi_compile_fog

            #pragma target 3.0

            #include "UnityStandardBRDF.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 worldpos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            sampler2D _WindTex;
            float4 _MainTex_ST;

            fixed4 _MainColour;
            fixed4 _TipColour;
            float _ColorBlendFactor;
            float _Smoothness;

            float2 _BendFactor;
            float _AAFactor;

            float2 _Min;
            float2 _Max;

            float _WindAmplitude;
            float _WindFrequency;
            float2 _WindDirection;

            // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
            // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
            // #pragma instancing_options assumeuniformscaling
            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            uint Hash(uint3 p) {
                return 19u * p.x + 47u * p.y + 101u * p.z + 131u;
            }
            float ClampedHash(uint3 p) {
                uint v = 19u * p.x + 47u * p.y + 101u * p.z + 131u;
                return frac((float)v * 2132.1896231);
            }

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                #ifdef UNITY_INSTANCING_ENABLED
                uint id = UNITY_GET_INSTANCE_ID(v);
                #endif

                v.vertex.x += _BendFactor.x * pow(v.uv.y, 2.0);
                v.vertex.z += _BendFactor.y * pow(v.uv.y, 2.0);
                
                float4 worldpos = mul(unity_ObjectToWorld, v.vertex);

                float2 world_uv = float2((worldpos.x - _Min.x) / (_Max.x - _Min.x), (worldpos.z - _Min.y) / (_Max.y - _Min.y));
                float2 wind_pos = world_uv - _Time.y * _WindFrequency;
                float wind_bend = tex2Dlod(_WindTex, float4(wind_pos, 0, 0)).r;

                worldpos.xz += wind_bend * pow(v.uv.y, 2.0) * _WindDirection * _WindAmplitude;

                o.vertex = mul(UNITY_MATRIX_VP, worldpos);
                o.worldpos = worldpos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                i.normal = normalize(i.normal);
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float3 lightColor = _LightColor0.rgb;
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldpos);
                float3 halfVector = normalize(lightDir + viewDir);

                float4 albedo = tex2D(_MainTex, i.uv) * lerp(_MainColour, _TipColour, clamp(i.uv.y - _ColorBlendFactor, 0.0, 1.0)) * clamp(i.uv.y - _AAFactor, 0.0, 1.0);
                float3 diffuse = albedo.rgb * DotClamped(lightDir, i.normal) * lightColor;
                float3 specular = pow(DotClamped(halfVector, i.normal), _Smoothness * 100) * lightColor * 0.5;

                float3 finalcolor = diffuse + specular;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return float4(finalcolor, 1);
            }
            ENDCG
        }
    }
}
