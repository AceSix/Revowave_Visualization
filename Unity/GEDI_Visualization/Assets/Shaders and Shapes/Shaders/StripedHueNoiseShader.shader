Shader "Custom/StripedHueNoiseShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.9, 0.9, 0.9, 1)
        _StripeFrequency ("Stripe Frequency (stripes/m)", Float) = 2
        _HueShiftFrequency ("Hue Shift Frequency (m)", Float) = 10
        _StripeBrightnessVariation ("Stripe Brightness Variation", Float) = 0.05
        _NoiseScale ("Noise Scale", Float) = 1
        _NoiseIntensity ("Noise Intensity", Float) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        // Include necessary Unity shader libraries
        #include "UnityCG.cginc"

        struct Input
        {
            float3 worldPos : WORLD;
        };

        fixed4 _BaseColor;
        float _StripeFrequency;
        float _HueShiftFrequency;
        float _StripeBrightnessVariation;
        float _NoiseScale;
        float _NoiseIntensity;

        // Function to convert RGB to HSV
        float3 RGBtoHSV(float3 c)
        {
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
            float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

            float d = q.x - min(q.w, q.y);
            float e = 1.0e-10;
            return float3(abs((q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
        }

        // Function to convert HSV to RGB
        float3 HSVtoRGB(float3 c)
        {
            float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
            return c.z * lerp(K.xxx, saturate(-K.www + p), step(c.y, 1.0));
        }

        // Simple noise function based on world position
        float Noise(float2 st)
        {
            return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Base color in RGB
            float3 baseColor = _BaseColor.rgb;

            // 1. Calculate stripe pattern based on world Y position
            float stripe = sin(IN.worldPos.y * _StripeFrequency * 6.28318530718); // 2π
            stripe = smoothstep(0.4, 0.6, stripe); // Creates sharp transitions for stripes

            // 2. Calculate hue shift based on horizontal distance
            float distance = length(IN.worldPos.xz);
            float hueShift = sin(distance / _HueShiftFrequency * 6.28318530718); // 2π every _HueShiftFrequency meters

            // 3. Apply stripe brightness variation
            float brightnessVariation = _StripeBrightnessVariation * stripe;

            // 4. Apply noise for additional grip
            float2 noiseUV = IN.worldPos.xz * _NoiseScale;
            float noiseValue = Noise(noiseUV);
            float noiseVariation = _NoiseIntensity * (noiseValue - 0.5); // Centers noise around 0

            // 5. Convert base color to HSV
            float3 hsv = RGBtoHSV(baseColor);

            // 6. Modify hue and value based on patterns and noise
            hsv.x += hueShift * 0.05; // Small hue shift
            hsv.z = saturate(hsv.z + brightnessVariation + noiseVariation); // Adjust brightness

            // 7. Convert back to RGB
            float3 finalColor = HSVtoRGB(hsv);

            // Assign final color to Albedo
            o.Albedo = finalColor;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}