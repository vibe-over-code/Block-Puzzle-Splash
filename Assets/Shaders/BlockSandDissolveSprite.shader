Shader "BlockPuzzle/BlockSandDissolveSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _ScatterAmount ("Scatter Amount", Range(0, 1)) = 0
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
        Blend SrcAlpha OneMinusSrcAlpha

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
            float _DissolveAmount;
            float _ScatterAmount;

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                float2 cell = floor(IN.texcoord * 16.0);
                float grain = hash(cell);
                float2 drift = normalize(float2(hash(cell + 7.1) - 0.5, hash(cell + 19.7) - 0.2));
                float lift = _ScatterAmount * (0.04 + grain * 0.08);
                float4 vertex = IN.vertex;
                vertex.xy += float2(drift.x * _ScatterAmount * 0.08, lift);

                OUT.vertex = UnityObjectToClipPos(vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                float2 grainCell = floor(IN.texcoord * 28.0);
                float grain = hash(grainCell);
                float verticalBreak = smoothstep(0.0, 1.0, _DissolveAmount + (1.0 - IN.texcoord.y) * 0.28);
                float dissolveMask = step(verticalBreak, grain);
                float grainAlpha = step(0.42 + _DissolveAmount * 0.25, hash(grainCell + 13.0));

                color.rgb = lerp(color.rgb, fixed3(0.95, 0.72, 0.34), 0.45 + _ScatterAmount * 0.25);
                color.a *= lerp(dissolveMask, grainAlpha, _ScatterAmount);
                color.a *= 1.0 - smoothstep(0.55, 1.0, _DissolveAmount);
                return color;
            }
            ENDCG
        }
    }
}
