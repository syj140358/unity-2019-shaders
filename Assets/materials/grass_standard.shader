Shader "Custom/grass_standard"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MAODS ("MAODS", 2D) = "white" {}
        _Normal ("Normal",2D) = "white" {}
        _Amount("Amount", float) = 0.1
        _MaxWidth("Max Displacement Width", Range(0, 5)) = 2 // width of the line around the dissolve
        _Radius("Radius", Range(0,5)) = 1 // width of the line around the dissolve
        _InnerRadius("InnerRadius", Range(0,3)) = 0.5 //inner radius
        _YOffset("Y offset", float) = 0.0// y offset, below this is no animation
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching" = "True" }
        Cull off
        ZWrite off
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard alpha:blend vertex:vert addshadow keepalpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        sampler2D _MAODS;
        sampler2D _Normal;
        half _Amount;
        uniform float3 _Position;
        float _MaxWidth;
        float _Radius;
        float _InnerRadius;
        float _YOffset;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v) 
        {
            //UNITY_INITIALIZE_OUTPUT(Input,o);
            float3 mpos = mul(unity_WorldToObject, float4(_Position,1)).xyz;
            float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
            // interaction radius movement for every position in array
            //float dis =  distance(_Position, wpos); // distance for radius
            float dis = distance(float3(mpos.x, mpos.y, 0), float3(v.vertex.x, v.vertex.y, 0));
            //dis = distance(mpos,v.vertex.xyz);
            float radius = 1-  saturate(dis /_Radius); // in world radius based on objects interaction radius
            float3 sphereDisp = v.vertex.xyz - mpos; // position comparison
            normalize(sphereDisp);
            sphereDisp *= radius*_Amount; // position multiplied by radius for falloff
            //v.vertex.x += clamp(sphereDisp.x, -_MaxWidth,_MaxWidth);
            //v.vertex.y += clamp(sphereDisp.y, -_MaxWidth,_MaxWidth);// vertex movement based on falloff and clamped

            v.vertex.x+=sphereDisp.x;
            v.vertex.y+=sphereDisp.y;
            //v.vertex.z+=sphereDisp.z;
            v.vertex.z = lerp(0,v.vertex.z,saturate(dis-_InnerRadius));
            float wavingX=v.texcoord.g * _Amount/10 * sin(_Time.y)+ v.vertex.x;
            v.vertex.x = lerp(v.vertex.x,wavingX,saturate(dis-_Radius));

        }


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            fixed4 maods = tex2D (_MAODS, IN.uv_MainTex);
            fixed3 normal = tex2D(_Normal, IN.uv_MainTex);
            o.Normal = normal;
            //o.Albedo = normalize(_Position);
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = maods.r;
            o.Smoothness = maods.g;
            o.Occlusion = maods.a;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
