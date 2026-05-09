Shader "BlockPuzzle/BlockBlastBlockSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _IsOccupied ("Is Occupied", Float) = 1
        _BevelSize ("Bevel Size", Range(0.02, 0.35)) = 0.22
        _DepthAmount ("Depth Amount", Range(0, 1.5)) = 1.0
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
            float _IsOccupied;
            float _BevelSize;
            float _DepthAmount;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                float occupied = saturate(_IsOccupied);

                float2 uv = IN.texcoord;
                float left = 1.0 - smoothstep(0.0, _BevelSize, uv.x);
                float top = 1.0 - smoothstep(0.0, _BevelSize, 1.0 - uv.y);
                float right = 1.0 - smoothstep(0.0, _BevelSize, 1.0 - uv.x);
                float bottom = 1.0 - smoothstep(0.0, _BevelSize, uv.y);
                float edge = saturate(max(max(left, top), max(right, bottom)));

                float light = saturate(left * 0.8 + top * 1.05);
                float shade = saturate(right * 0.85 + bottom * 1.15);
                float2 centeredUv = abs(uv - 0.5) * 2.0;
                float centerInset = smoothstep(0.15, 0.95, max(centeredUv.x, centeredUv.y));
                float cornerGlow = smoothstep(0.75, 0.0, distance(uv, float2(0.24, 0.78)));
                float cornerShade = smoothstep(0.85, 0.0, distance(uv, float2(0.82, 0.18)));

                fixed3 litColor = lerp(color.rgb, fixed3(1.0, 1.0, 1.0), saturate((light * 0.58 + cornerGlow * 0.18) * _DepthAmount));
                fixed3 shadedColor = lerp(litColor, fixed3(0.0, 0.0, 0.0), saturate((shade * 0.46 + cornerShade * 0.18) * _DepthAmount));
                fixed3 centerColor = lerp(shadedColor * 0.82, shadedColor * 1.08, centerInset);
                fixed3 bevelColor = lerp(centerColor, shadedColor, edge * 0.42);

                color.rgb = lerp(color.rgb, bevelColor, occupied);
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
