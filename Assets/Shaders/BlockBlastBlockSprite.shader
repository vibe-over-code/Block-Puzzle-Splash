Shader "BlockPuzzle/BlockBlastBlockSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _IsOccupied ("Is Occupied", Float) = 1
        _BlockType ("Block Type", Float) = 0
        _FreezeTurnsLeft ("Freeze Turns Left", Float) = 0

        _BevelSize ("Bevel Size", Range(0.01, 0.25)) = 0.16
        _DepthAmount ("Depth Amount", Range(0,3)) = 1.0
        _Gloss ("Gloss", Range(1,64)) = 42

        _LightDir1 ("Key Light Dir", Vector) = (-0.55, 0.85, 0.75, 0)
        _LightColor1 ("Key Light Color", Color) = (1,1,1,1)
        _LightIntensity1 ("Key Intensity", Range(0,8)) = 2.0

        _LightDir2 ("Fill Light Dir", Vector) = (0.85, 0.25, 0.45, 0)
        _LightColor2 ("Fill Light Color", Color) = (0.65,0.82,1,1)
        _LightIntensity2 ("Fill Intensity", Range(0,8)) = 0.45

        _LightDir3 ("Rim Light Dir", Vector) = (0.15,-1.0,0.7,0)
        _LightColor3 ("Rim Light Color", Color) = (1,0.58,0.38,1)
        _LightIntensity3 ("Rim Intensity", Range(0,8)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _BaseColor;

            float _IsOccupied;
            float _BlockType;
            float _FreezeTurnsLeft;
            float _BevelSize;
            float _DepthAmount;
            float _Gloss;

            float4 _LightDir1;
            fixed4 _LightColor1;
            float _LightIntensity1;
            float4 _LightDir2;
            fixed4 _LightColor2;
            float _LightIntensity2;
            float4 _LightDir3;
            fixed4 _LightColor3;
            float _LightIntensity3;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float3 SaturateBlockColor(float3 color)
            {
                float luminance = dot(color, float3(0.299, 0.587, 0.114));
                return saturate(lerp(luminance.xxx, color, 1.28));
            }

            float RoundedBoxMask(float2 uv, float radius, float softness)
            {
                float2 p = abs(uv - 0.5) - (0.5 - radius);
                float dist = length(max(p, 0.0)) + min(max(p.x, p.y), 0.0) - radius;
                return 1.0 - smoothstep(0.0, softness, dist);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord);
                float2 uv = IN.texcoord;
                float occupied = saturate(_IsOccupied);

                float3 tintColor = tex.rgb * IN.color.rgb;
                float hasBaseColor = step(0.001, dot(_BaseColor.rgb, float3(1.0, 1.0, 1.0)));
                float3 baseTint = SaturateBlockColor(lerp(tintColor, _BaseColor.rgb, hasBaseColor));

                float outerMask = RoundedBoxMask(uv, 0.145, 0.018);
                float innerMask = RoundedBoxMask(uv, 0.11, 0.035);
                float rimMask = saturate(outerMask - innerMask);

                float left = 1.0 - smoothstep(0.0, _BevelSize, uv.x);
                float right = 1.0 - smoothstep(0.0, _BevelSize, 1.0 - uv.x);
                float top = 1.0 - smoothstep(0.0, _BevelSize, 1.0 - uv.y);
                float bottom = 1.0 - smoothstep(0.0, _BevelSize, uv.y);

                float2 p = uv - 0.5;
                float dome = 1.0 - saturate(dot(p * 1.65, p * 1.65));
                float diagonal = saturate((1.0 - uv.x) * 0.55 + uv.y * 0.45);
                float shade = 0.74 + dome * 0.26 + diagonal * 0.18;
                shade += top * 0.35 + left * 0.12;
                shade -= bottom * 0.34 * _DepthAmount + right * 0.25 * _DepthAmount;

                float3 finalRGB = baseTint * shade;

                float edgeDistance = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                float bevelLine = smoothstep(0.025, 0.11, edgeDistance);
                finalRGB = lerp(baseTint * 0.52, finalRGB, bevelLine);

                float highlight = smoothstep(0.95, 0.2, distance(uv, float2(0.3, 0.78)));
                highlight *= smoothstep(0.06, 0.24, uv.x) * smoothstep(0.06, 0.2, 1.0 - uv.y);
                finalRGB += highlight * 0.34;

                float topSheen = smoothstep(0.18, 0.88, uv.x) * smoothstep(0.74, 0.96, uv.y) * (1.0 - smoothstep(0.84, 1.0, uv.y));
                finalRGB += topSheen * 0.18;
                finalRGB += rimMask * (top + left) * 0.22;
                finalRGB -= rimMask * (bottom + right) * 0.16;

                float isDynamite = 1.0 - saturate(abs(_BlockType - 1.0));
                float isFreeze = 1.0 - saturate(abs(_BlockType - 2.0));
                float stripe = step(0.5, frac((uv.x + uv.y) * 6.0));
                finalRGB = lerp(finalRGB, finalRGB * lerp(0.86, 1.18, stripe) + float3(0.55, 0.12, 0.02) * 0.18, isDynamite * 0.55);
                finalRGB = lerp(finalRGB, lerp(finalRGB, float3(0.62, 0.92, 1.0), 0.42) + highlight * 0.18, isFreeze * 0.7);

                finalRGB = lerp(float3(0.105, 0.125, 0.16) + rimMask * 0.035, finalRGB, occupied);

                fixed4 color;
                color.rgb = saturate(finalRGB);
                color.a = tex.a * IN.color.a * lerp(0.62, 1.0, occupied) * outerMask;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
