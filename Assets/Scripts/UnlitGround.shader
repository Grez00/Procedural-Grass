Shader "Custom/UnlitGround"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (0, 0, 0, 0)
        _LowTint ("Low Tint", Color) = (0, 0, 0, 0)
        _Amplitude ("Amplitude", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 objPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NoiseTex;

            fixed4 _Tint;
            fixed4 _LowTint;

            float _Amplitude;

            v2f vert (appdata v)
            {
                v2f o;

                float4 p = v.vertex;
                float4 noise = tex2Dlod(_NoiseTex, float4(v.uv, 0.0, 0.0));
                p.y += noise.r * _Amplitude;

                o.objPos = p;
                o.vertex = UnityObjectToClipPos(p);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                col *= lerp(_LowTint, _Tint, i.objPos.y);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
