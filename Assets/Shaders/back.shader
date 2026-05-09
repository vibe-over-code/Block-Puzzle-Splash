Shader "Custom/3DVortexTunnel"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.02, 0.02, 0.03, 1)
        _GlowColor ("Glow Color", Color) = (0.1, 0.12, 0.15, 1)
        _TunnelSpeed ("Tunnel Speed", Float) = 0.5
        _RotationSpeed ("Rotation Speed", Float) = 0.2
        _LayerScale ("Complexity", Float) = 8.0
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

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _BaseColor;
            fixed4 _GlowColor;
            float _TunnelSpeed;
            float _RotationSpeed;
            float _LayerScale;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Центрируем UV-координаты (от -0.5 до 0.5)
                float2 uv = i.uv - 0.5;
                
                // 1. Переходим в полярные координаты
                float r = length(uv);        // Расстояние от центра
                float angle = atan2(uv.y, uv.x); // Угол

                // 2. Создаем эффект 3D-перспективы (Z-axis)
                // Чем ближе к центру (r -> 0), тем "дальше" точка в туннеле
                float z = 1.0 / (r + 0.01); 
                
                // 3. Анимируем движение "вперед" и вращение
                float2 tunnelUV = float2(z + _Time.y * _TunnelSpeed, angle / 3.14159);
                
                // Добавляем небольшое вращение по мере продвижения вглубь
                tunnelUV.y += z * 0.1 + _Time.y * _RotationSpeed;

                // 4. Генерируем процедурный узор (сетку/блоки)
                float2 grid = frac(tunnelUV * _LayerScale);
                float pattern = smoothstep(0.4, 0.5, grid.x) * smoothstep(0.4, 0.5, grid.y);
                
                // 5. Создаем затухание в центре (эффект глубины/тьмы)
                float fog = smoothstep(0.0, 0.4, r);
                
                // Смешиваем цвета
                fixed4 finalColor = lerp(_BaseColor, _GlowColor, pattern * fog);
                
                // Добавляем мягкое виньетирование по краям для концентрации на поле
                finalColor *= smoothstep(1.2, 0.2, r);

                return finalColor;
            }
            ENDCG
        }
    }
}