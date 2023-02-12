Shader "Custom/Field"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        _VoronoiAngleOffset("Voronoi Angle Offset", Range(0, 12.566368)) = 3.141592
        _VoronoiCellDensity("Voronoi Cell Density", Range(0, 4096)) = 1024
        _VoronoiColorStrength("Voronoi Color Strength", Range(0.001, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        /*float2 random2(float2 p)
        {
            return frac(sin(float2(dot(p, float2(117.12, 341.7)), dot(p, float2(269.5, 123.3)))) * 43458.5453);
        }

        float voronoi(float2 uv)
        {
            float2 scalingUV = uv * 6.0;
            float2 iuv = floor(scalingUV);
            float2 fuv = frac(scalingUV);
            float minDist = 1.0;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    float2 neighbour = float2(float(x), float(y));
                    float2 pointv = random2(iuv + neighbour);
                    pointv = 0.5 + 0.5 * sin(_Time.z + 6.2236 * pointv);

                    float2 diff = neighbour + pointv - fuv;
                    float dist = length(diff);
                    minDist = min(minDist, dist);
                }
            }

            return minDist;
        }*/

        float2 unity_voronoi_noise_randomVector(float2 UV, float offset)
        {
            float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
            UV = frac(sin(mul(UV, m)) * 46839.32);
            return float2(sin(UV.y * +offset) * 0.5 + 0.5, cos(UV.x * offset) * 0.5 + 0.5);
        }

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float _VoronoiAngleOffset;
        float _VoronoiCellDensity;
        float _VoronoiColorStrength;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv2 = IN.uv_MainTex;
            //float v = voronoi(uv2);

            float2 g = floor(uv2 * _VoronoiCellDensity);
            float2 f = frac(uv2 * _VoronoiCellDensity);
            float t = 8.0;
            float3 res = float3(8.0, 0.0, 0.0);
            //float Out;
            float Cells;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    float2 lattice = float2(x, y);
                    float2 offset = unity_voronoi_noise_randomVector(lattice + g, _VoronoiAngleOffset);
                    float d = distance(lattice + offset, f);
                    if (d < res.x)
                    {
                        res = float3(d, offset.x, offset.y);
                        //Out = res.x;
                        Cells = res.y;
                    }
                }
            }

            //fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color + pow(v, 2);
            //fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color + Cells * _VoronoiColorStrength;
            fixed4 c = _Color + Cells * _VoronoiColorStrength;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
