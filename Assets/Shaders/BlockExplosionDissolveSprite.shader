Shader "BlockPuzzle/BlockExplosionDissolveSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("Progress", Range(0, 1)) = 0
        _ScatterAmount ("Scatter Amount", Range(0, 1)) = 0
        _BlastDirection ("Blast Direction", Vector) = (0,1,0,0)
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
            float _Progress;
            float _ScatterAmount;
            float4 _BlastDirection;

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                float2 cell = floor(IN.texcoord * 14.0);
                float grain = hash(cell);
                float2 radial = normalize(_BlastDirection.xy + float2(0.001, 0.001));
                float2 drift = normalize(radial + float2(hash(cell + 7.1) - 0.5, hash(cell + 19.7) - 0.5) * 0.32);

                float4 vertex = IN.vertex;
                vertex.xy += drift * _ScatterAmount * (0.035 + grain * 0.055);

                OUT.vertex = UnityObjectToClipPos(vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                float2 grainCell = floor(IN.texcoord * 26.0);
                float grain = hash(grainCell);
                float2 radial = normalize(_BlastDirection.xy + float2(0.001, 0.001));
                float outward = dot(IN.texcoord - 0.5, radial);

                float warmEdge = smoothstep(-0.22, 0.34, outward + _Progress * 0.18);
                float dissolve = smoothstep(0.02, 1.0, _Progress + grain * 0.34 + warmEdge * 0.16);
                float grainAlpha = step(dissolve, grain + 0.18);

                color.rgb = lerp(color.rgb, fixed3(0.98, 0.55, 0.16), 0.28 + warmEdge * 0.22);
                color.rgb = lerp(color.rgb, fixed3(0.78, 0.48, 0.25), _Progress * 0.22);
                color.a *= grainAlpha;
                color.a *= 1.0 - smoothstep(0.62, 1.0, _Progress);
                return color;
            }
            ENDCG
        }
    }
}
