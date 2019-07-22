Shader "Unlit/dissolving"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Threshold ("Threshold", Range(0,1)) = 0 
        _NormalMap ("Normal", 2D) = "white" {}
        _Occlusion ("AO", 2D) = "white" {}
        _Noise ("Noise", 2D) = "white" {}
        _DissolveColorA ("Color1", color) = (0,0,0,0)
        _DissolveColorB ("Color2", color) = (0,0,0,0)
        _DissolveEdgeA("DissolveEdgeA", Range(0,1)) = 0.7  
        _DissolveEdgeB("DissolveEdgeB", Range(0,1)) = 0.8
        _FlyFactor("Flyfactor", Range(0,1)) = 0.2
        _FlyThreshold("FlyThreshold", Range(0,1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode" = "ForwardBase"}
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 tangent : TANGENT; 
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float3 lightDir : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Threshold;
            sampler2D _NormalMap;
            sampler2D _Occlusion;
            sampler2D _Noise;
            float4 _DissolveColorA;
            float4 _DissolveColorB;
            float _DissolveEdgeA;
            float _DissolveEdgeB;
            float _FlyThreshold;
            float _FlyFactor;

            v2f vert (appdata v)
            {
                v2f o;
                v.vertex.xyz += v.normal * saturate(_Threshold - _FlyThreshold) * _FlyFactor; 
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                TANGENT_SPACE_ROTATION;
                o.lightDir = mul(rotation, ObjSpaceLightDir(v.vertex));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 noise = tex2D(_Noise, i.uv);

                if (noise.r<=_Threshold) discard;
                
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb;
                // sample the texture
                float4 col = tex2D(_MainTex, i.uv);
                float4 normalColor = tex2D(_NormalMap, i.uv);
                float3 tangentNormal = UnpackNormal(normalColor);
                fixed3 lightDir = normalize(i.lightDir);

                fixed3 diffuse = _LightColor0 * max(0, dot(tangentNormal, lightDir)) * col;
                fixed4 ao = tex2D(_Occlusion, i.uv);
                fixed3 tempColor = diffuse + ambient * col * ao;
                
                float percentage = _Threshold / noise.r;
                float3 edgecolor = lerp(tempColor.rgb, _DissolveColorA.rgb,step(_DissolveEdgeA,percentage));
                float3 edgecolor2 = lerp(edgecolor.rgb, _DissolveColorB.rgb, step(_DissolveEdgeB,percentage));

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return fixed4(edgecolor2,1);
            }
            ENDCG
        }
    }
}
