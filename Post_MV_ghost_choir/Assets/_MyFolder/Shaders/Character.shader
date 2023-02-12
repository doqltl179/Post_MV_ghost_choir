Shader "Custom/Character"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        _Alpha ("Alpha", Range(0.0, 1.0)) = 0.3

        [Header(Body Volume)]
        _BodyVolumeCount ("Body Volume Count", Range(0, 36)) = 9
        _BodyVolumeSize ("Body Volume Size", Range(1.0, 2.0)) = 1.1
        _BodyVolumeGetDown ("Body Volume Down Size", Range(0.0, 0.1)) = 0.05

        [Header(Body Jiggle)]
        _BodyJiggleDistance("Body Jiggle Distance", Range(0.001, 1)) = 0.5
        _BodyJiggleSpeed("Body Jiggle Speed", Range(0, 30)) = 0.2
        _BodyJiggleFrequency("Body Jiggle Frequency", Range(1.0, 50)) = 10
        _BodyJiggleUVHeight("Body Jiggle UV Height", Range(0.0, 1.0)) = 0.8
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 200

        //Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        int _BodyVolumeCount;
        float _BodyVolumeSize;
        float _BodyVolumeGetDown;

        float _BodyJiggleDistance;
        float _BodyJiggleSpeed;
        float _BodyJiggleFrequency;
        float _BodyJiggleUVHeight;

        float _Alpha;

        void vert(inout appdata_full v) {
            const float PI = 3.141592;

            float angle = lerp(0, PI * 2, v.texcoord.x) * _BodyVolumeCount;
            // 0 ~ 1
            float sinValue = (sin(angle) + 1) * 0.5;
            sinValue = pow(sinValue, 2) * cos(sinValue);
            v.vertex.xz *= lerp(1, _BodyVolumeSize, sinValue * (1 - v.texcoord.y));
            v.vertex.y -= lerp(0.0, _BodyVolumeGetDown, sinValue) * (1 - v.texcoord.y);

            v.vertex.xyz += v.normal * sin(_Time.y * _BodyJiggleSpeed + v.texcoord.y * _BodyJiggleFrequency) * _BodyJiggleDistance * clamp((_BodyJiggleUVHeight - v.texcoord.y / 1), 0, 1);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _Alpha;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
