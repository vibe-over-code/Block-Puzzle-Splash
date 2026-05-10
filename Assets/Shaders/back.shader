Shader "Custom/3DVortexTunnel"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.11, 0.14, 0.2, 1)
        _GlowColor ("Glow Color", Color) = (0.2, 0.28, 0.42, 1)
        _TunnelSpeed ("Tunnel Speed", Float) = 0.5
        _RotationSpeed ("Rotation Speed", Float) = 0.2
        _LayerScale ("Complexity", Float) = 8.0
        _ShapeColor ("Falling Shape Color", Color) = (0.16, 0.34, 0.62, 1)
        _ShapeGlowColor ("Falling Shape Glow", Color) = (0.36, 0.72, 1.0, 1)
        _ShapeDensity ("Falling Shape Density", Range(2, 14)) = 7
        _ShapeScale ("Falling Shape Scale", Range(0.25, 0.9)) = 0.55
        _ShapeSpeed ("Falling Shape Speed", Range(0, 2)) = 0.12
        _ShapeAlpha ("Falling Shape Alpha", Range(0, 1)) = 0.5
        
        // Параметры маски
        _MaskWidth ("Mask Width", Range(0, 2)) = 0.8
        _MaskHeight ("Mask Height", Range(0, 2)) = 0.6
        _MaskSoftness ("Mask Softness", Range(0.01, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _BaseColor;
            fixed4 _GlowColor;
            fixed4 _ShapeColor;
            fixed4 _ShapeGlowColor;
            float _TunnelSpeed;
            float _RotationSpeed;
            float _LayerScale;
            float _ShapeDensity;
            float _ShapeScale;
            float _ShapeSpeed;
            float _ShapeAlpha;
            float _MaskWidth;
            float _MaskHeight;
            float _MaskSoftness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float cellEq(float a, float b)
            {
                return 1.0 - step(0.5, abs(a - b));
            }

            float inRange(float value, float low, float high)
            {
                return step(low, value) * step(value, high);
            }

            float shapeCell(float2 cell, float id)
            {
                float x = cell.x;
                float y = cell.y;
                float i = floor(frac(id) * 7.0);

                float squareShape = inRange(x, 1.0, 2.0) * inRange(y, 1.0, 2.0);
                float lineShape = cellEq(x, 1.0) * inRange(y, 0.0, 3.0);
                float tShape = inRange(x, 0.0, 2.0) * cellEq(y, 1.0) + cellEq(x, 1.0) * cellEq(y, 2.0);
                float lShape = cellEq(x, 1.0) * inRange(y, 0.0, 2.0) + inRange(x, 1.0, 2.0) * cellEq(y, 0.0);
                float zShape = inRange(x, 0.0, 1.0) * cellEq(y, 1.0) + inRange(x, 1.0, 2.0) * cellEq(y, 2.0);
                float shortLine = cellEq(x, 1.0) * inRange(y, 1.0, 2.0);
                float corner = inRange(x, 1.0, 2.0) * cellEq(y, 1.0) + cellEq(x, 1.0) * cellEq(y, 2.0);

                float s = 0.0;
                s += squareShape * cellEq(i, 0.0);
                s += lineShape * cellEq(i, 1.0);
                s += tShape * cellEq(i, 2.0);
                s += lShape * cellEq(i, 3.0);
                s += zShape * cellEq(i, 4.0);
                s += shortLine * cellEq(i, 5.0);
                s += corner * cellEq(i, 6.0);
                
                return saturate(s); 
            }

            float fallingShapes(float2 uv)
            {
                float2 flow = uv;
                flow.y += _Time.y * _ShapeSpeed;
                float colId = floor(flow.x * _ShapeDensity);
                flow.y += hash21(float2(colId, 12.34)) * 5.0; 
                
                float2 lane = floor(flow * _ShapeDensity);
                float rnd = hash21(lane);
                float rndY = hash21(lane + float2(1.23, 4.56));
                
                float2 cellUV = frac(flow * _ShapeDensity);
                float size = max(0.05, _ShapeScale);
                float maxShift = max(0.0, 1.0 - size) * 0.9; 
                
                cellUV.x += (rnd - 0.5) * maxShift;
                cellUV.y += (rndY - 0.5) * maxShift;
                
                float2 local = (cellUV - 0.5) / size + 0.5;
                float inBox = step(0.0, local.x) * step(local.x, 1.0) * step(0.0, local.y) * step(local.y, 1.0);

                float2 grid = local * 4.0;
                float2 shapeGrid = floor(grid);
                float2 miniCell = frac(grid);
                
                float block = shapeCell(shapeGrid, rnd);
                float roundedCell = 1.0 - smoothstep(0.34, 0.49, length(miniCell - 0.5));
                float glow = 1.0 - smoothstep(0.36, 0.7, length(miniCell - 0.5));
                
                float fade = smoothstep(0.0, 0.05, cellUV.y) * (1.0 - smoothstep(0.95, 1.0, cellUV.y));
                fade *= smoothstep(0.0, 0.05, cellUV.x) * (1.0 - smoothstep(0.95, 1.0, cellUV.x));

                return saturate((block * roundedCell + block * glow * 0.35) * inBox * fade);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv - 0.5;
                float r = length(centered);

                // --- ЛОГИКА МЯГКОЙ МАСКИ ---
                float2 d = abs(centered);
                // Плавный переход для каждой оси
                float maskX = smoothstep(_MaskWidth * 0.5 - _MaskSoftness, _MaskWidth * 0.5, d.x);
                float maskY = smoothstep(_MaskHeight * 0.5 - _MaskSoftness, _MaskHeight * 0.5, d.y);
                // Объединяем: если пиксель снаружи прямоугольника по ЛЮБОЙ оси, рисуем фигуры
                float finalMask = max(maskX, maskY);
                // ---------------------------

                float verticalGlow = smoothstep(0.0, 1.0, i.uv.y);
                float centerGlow = 1.0 - smoothstep(0.0, 0.72, r);
                
                float2 slowGrid = frac((i.uv + float2(0.0, _Time.y * 0.015)) * _LayerScale);
                float gridLine = 1.0 - min(
                    smoothstep(0.0, 0.045, slowGrid.x) * smoothstep(0.0, 0.045, 1.0 - slowGrid.x),
                    smoothstep(0.0, 0.045, slowGrid.y) * smoothstep(0.0, 0.045, 1.0 - slowGrid.y)
                );
                gridLine *= 0.12;

                fixed4 finalColor = lerp(_BaseColor, _GlowColor, verticalGlow * 0.45 + centerGlow * 0.35);
                finalColor.rgb += _GlowColor.rgb * gridLine;

                // Применяем мягкую маску
                float shapes = fallingShapes(i.uv) * finalMask;

                finalColor.rgb = lerp(finalColor.rgb, _ShapeColor.rgb, shapes * _ShapeAlpha);
                finalColor.rgb += _ShapeGlowColor.rgb * smoothstep(0.02, 0.6, shapes) * _ShapeAlpha * 0.35;

                finalColor.rgb *= lerp(0.86, 1.0, centerGlow);
                return finalColor;
            }
            ENDCG
        }
    }
}