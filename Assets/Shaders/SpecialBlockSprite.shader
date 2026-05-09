Shader "BlockPuzzle/SpecialBlockSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BlockType ("Block Type", Float) = 0
        _FreezeTurnsLeft ("Freeze Turns Left", Float) = 0
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
            float _BlockType;
            float _FreezeTurnsLeft;

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
                float2 uv = IN.texcoord;

                if (_BlockType > 0.5 && _BlockType < 1.5)
                {
                    float stripe = step(0.58, frac((uv.x + uv.y) * 8.0));
                    float core = smoothstep(0.42, 0.18, distance(uv, float2(0.5, 0.5)));
                    fixed3 ember = lerp(fixed3(0.95, 0.18, 0.05), fixed3(1.0, 0.95, 0.25), stripe);
                    color.rgb = lerp(color.rgb, ember, 0.45 + core * 0.35);
                }
                else if (_BlockType > 1.5)
                {
                    float gridX = 1.0 - smoothstep(0.015, 0.045, abs(frac(uv.x * 5.0) - 0.5));
                    float gridY = 1.0 - smoothstep(0.015, 0.045, abs(frac(uv.y * 5.0) - 0.5));
                    float frost = saturate(max(gridX, gridY) + _FreezeTurnsLeft * 0.08);
                    color.rgb = lerp(color.rgb, fixed3(0.7, 0.95, 1.0), 0.35 + frost * 0.45);
                }

                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
