Shader "Unlit/RimLight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RimColor ("RimColor", color) = (1,0,0,1)
        _RimPower ("RimPower", Range(0.001,3.00)) = 0.2
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

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldNormal : NORMAL;
                float3 worldViewDir : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _RimColor;
            float _RimPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                fixed3 worldpos = mul(unity_ObjectToWorld,v.vertex).xyz;
                o.worldViewDir = _WorldSpaceCameraPos.xyz - worldpos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed3 worldNormal = normalize(i.worldNormal);
                float3 worldViewDir = normalize(i.worldViewDir);
                float rim = 1 - abs(dot(i.worldViewDir, worldNormal));
                fixed3 rimColor = _RimColor * pow(rim, 1 / _RimPower);
                fixed3 finalcol = col.rgb + rimColor;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return fixed4(finalcol,1);
            }
            ENDCG
        }
    }
}
