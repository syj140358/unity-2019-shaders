Shader "Custom/BigWaveShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ReflectionTex("ReflectionTex", 2D) = "white" {}
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _RefractionRate("Refraction", Range(0,2)) = 0.77
        _Normal("Normal", 2D) = "white" {}
        _Noise("Noise", 2D) = "white" {}
        _ColorA("ColorA", Color) = (1,1,1,1)
        xspeed("xspeed", Range(0,1)) = 0.1
        yspeed("yspeed", Range(0,1)) = 0.05
        _yTile("yTile",Range(0,1)) = 0.1

        //wave
        _WaveHeight("Wave Height", Range(0,10)) = 1

        _RRrate("Reflaction Refraction Rate", Range(0,1)) = 0.5

        //fresnel
        _fresnelColor("fresnel color", Color) = (1,0,0,1)
        _fresnel_offset("fresnel offset", Range(0,1)) = 0.2
        _fresnel_factor("fresnel factor", Range(0,1)) = 0.8

    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Opaque" }
        GrabPass{}
        Cull off 
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert alpha noshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _Normal;
        sampler2D _ReflectionTex;
        sampler2D _Noise;
        sampler2D _GrabTexture;
        sampler2D _CameraDepthTexture;


        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldRefl;
            float3 viewDir;
            float3 worldNormal; 
            INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float xspeed;
        float yspeed;
        float4 _ColorA;
        float _fresnel_offset;
        float _fresnel_factor;
        float4 _fresnelColor;
        float _RRrate;
        float _WaveHeight;
        float _yTile;

        //refraction
        float _RefractionRate;


        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v, out Input o) 
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            float4 wpos = mul(unity_ObjectToWorld,v.vertex);
            float2 offset = float2(_Time.x * xspeed, _Time.x * yspeed);
            v.texcoord.xy += offset;
            v.texcoord.xy *= float2(1,_yTile);
            fixed4 c = tex2Dlod (_MainTex, float4(v.texcoord.xy, 0, 1));
            wpos.y += (c.r-0.5) * _WaveHeight;
            v.vertex = mul(unity_WorldToObject,wpos);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float2 offset = float2(_Time.x*xspeed,_Time.x*yspeed);
            float2 uv = IN.uv_MainTex;
            float2 uv2 = uv;
            uv += offset;
            uv2 -= offset/4;

            //normal blend
            float3 normal1 = UnpackNormal(tex2D(_Normal,uv));
            float3 normal2 = UnpackNormal(tex2D(_Normal,uv2));
            float3 normal = BlendNormals(normal1,normal2);
            o.Normal = normal;


            //calculate reflection
            float4 clipPos = UnityWorldToClipPos(float4(IN.worldPos,1));
            float4 refl=ComputeScreenPos(clipPos);
            refl.y = clipPos.w-refl.y;
            fixed4 reflcol = tex2Dproj(_ReflectionTex, refl);

            //calculate refraction
            float3 temp = IN.worldPos - _WorldSpaceCameraPos;
            normalize(temp);
            

            //calculate angles
            float AngleinCos = dot(temp,float3(0,1,0)); 
            float AngleoutCos = sqrt(1-((1-pow(AngleinCos,2))/pow(_RefractionRate,2)));
            float3 refractionDir=refract(temp, o.Normal, _RefractionRate);

            //calculate depth
            half Depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(ComputeScreenPos(clipPos))));
            float depth = abs(Depth - refl.w);
            
            float dist = depth / AngleoutCos;
            float3 worldRefractionPoint = IN.worldPos + refractionDir * dist;
            float worldy = 0;
            float worldx = lerp(_WorldSpaceCameraPos.x,worldRefractionPoint.x,abs(_WorldSpaceCameraPos.y / (_WorldSpaceCameraPos.y - worldRefractionPoint.y)));
            float worldz = lerp(_WorldSpaceCameraPos.z,worldRefractionPoint.z,abs(_WorldSpaceCameraPos.y / (_WorldSpaceCameraPos.y - worldRefractionPoint.y)));
            float3 RefractionPoint = float3(worldx,worldy,worldz);
            float4 clipPos2 = UnityWorldToClipPos(float4(RefractionPoint,1));
            float4 proj=ComputeScreenPos(clipPos2);
            float4 projcol=tex2Dproj(_GrabTexture,proj);

            //fresnel color
            float fresnel = _fresnel_offset+ _fresnel_factor*(1-abs(dot(normal1,IN.viewDir)));
            float4 c1 = _ColorA;
            float4 c_main = lerp(c1,_fresnelColor,fresnel);

            //RR color
            float4 c_RR = lerp(projcol,reflcol,_RRrate);
            
            float4 finalcol=lerp(projcol,reflcol,saturate(fresnel));
            o.Albedo = c_RR * c_main * 1.5;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
