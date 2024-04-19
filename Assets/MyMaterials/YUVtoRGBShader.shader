Shader "Custom/YUVtoRGBHololens"
{
    Properties
    {
        _YTex ("Y Texture", 2D) = "white" {}
        _UVTex ("UV Texture", 2D) = "black" {}
        _BorderColor ("Border Color", Color) = (0.851, 0.851, 0.851, 1.0) // Color of the border
        _BorderThickness ("Border Thickness", Float) = 0.003
        _FadeMargin ("Fade Margin", Float) = 0.01
        _Brightness ("Brightness", Float) = 0.0 // New property for brightness adjustment
        _Contrast ("Contrast", Float) = 1.0 // New property for contrast adjustment
        _ColorBalance ("Color Balance", Vector) = (1.0, 1.0, 1.0, 1.0) // New property for color balance (assuming uniform adjustment for simplicity)
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
            #include "UnityCG.cginc"

            // Enable instancing support for stereo rendering
            #pragma multi_compile_instancing

            sampler2D _YTex;
            sampler2D _UVTex;
            float4 _BorderColor;
            float _BorderThickness;
            float _FadeMargin;
            float _Brightness;
            float _Contrast;
            float4 _ColorBalance;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 flippedUV = float2(i.uv.x, 1.0 - i.uv.y);
                float y = tex2D(_YTex, flippedUV).r;
                float2 uv = tex2D(_UVTex, flippedUV).rg - 0.5;
                float3 rgb = float3(y + 1.403 * uv.y, y - 0.344 * uv.x - 0.714 * uv.y, y + 1.770 * uv.x);

                // Adjust for brightness and contrast
                rgb = (rgb - 0.5) * _Contrast + 0.5 + _Brightness;

                // Apply color balance adjustment
                rgb *= _ColorBalance.rgb;

                // Calculate distance to the nearest edge and apply fade to black
                float edgeDistX = min(i.uv.x, 1.0 - i.uv.x);
                float edgeDistY = min(i.uv.y, 1.0 - i.uv.y);
                float edgeDist = min(edgeDistX, edgeDistY);
                if (edgeDist < (_BorderThickness + _FadeMargin))
                {
                    if (edgeDist < _BorderThickness)
                    {
                        rgb = _BorderColor.rgb;
                    }
                    else
                    {
                        float fadeFactor = (edgeDist - _BorderThickness) / _FadeMargin;
                        rgb *= fadeFactor;
                    }
                }

                return float4(rgb, 1.0);
            }
            ENDCG
        }
    }
}
