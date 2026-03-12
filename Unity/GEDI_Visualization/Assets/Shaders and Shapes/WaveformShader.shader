Shader "Custom/WaveformShaderWithText"
{
    Properties
    {
        _NoiseTex ("Noise Texture 1", 2D) = "white" {}
        _NoiseIntensity ("Noise Intensity 1", Range(0, 5)) = 2.5
        _NoiseScale ("Noise Scale 1", Range(0.1, 10.0)) = 0.1
        _NoiseOffset ("Noise Offset 1", Vector) = (0,0,0,0)

        _NoiseTex2 ("Noise Texture 2", 2D) = "black" {}
        _NoiseIntensity2 ("Noise Intensity 2", Range(0, 2)) = 1.0
        _NoiseScale2 ("Noise Scale 2", Range(0.05, 0.5)) = 0.2
        _NoiseOffset2 ("Noise Offset 2", Vector) = (0,0,0,0)

        _StripeFrequency ("Stripe Frequency", Range(0.0, 300)) = 100.0
        _StripeMin("Stripe Min Brightness", Range(0.0, 1)) = 0.65
        _StripeMax("Stripe Max Brightness", Range(0.0, 1)) = 0.85
        _AmbientLight("Ambient Light", Float) = 1
        _DiffuseLight("Diffuse Strength", Float) = 0
        _Shininess("Diffuse Exponent", Float) = 8
        
        // Text properties
        _TextTex ("Text Texture", 2D) = "black" {}
        _TextColor ("Text Color", Color) = (1,1,1,1)
        _TextVMin ("Text V Min", Range(0, 1)) = 0.4
        _TextVMax ("Text V Max", Range(0, 1)) = 0.6
        
        // Text positioning - these control angular position in the cylinder
        _TextAngleStart ("Text Start Angle (Degrees)", Range(0, 360)) = 120
        _TextAngleEnd ("Text End Angle (Degrees)", Range(0, 360)) = 240
        
        // Flip text options
        [Toggle] _FlipTextHorizontal ("Flip Text Horizontally", Float) = 1
        [Toggle] _FlipTextVertical ("Flip Text Vertically", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            float _NoiseIntensity;
            float _NoiseScale;
            float4 _NoiseOffset;

            sampler2D _NoiseTex2;
            float _NoiseIntensity2;
            float _NoiseScale2;
            float4 _NoiseOffset2;

            float _StripeFrequency;
            float _StripeMin;
            float _StripeMax;
            float _AmbientLight;
            float _DiffuseLight;
            float _Shininess;
            
            // Text properties
            sampler2D _TextTex;
            float4 _TextColor;
            float _TextVMin;
            float _TextVMax;
            float _TextAngleStart;
            float _TextAngleEnd;
            float _FlipTextHorizontal;
            float _FlipTextVertical;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1; // heights
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 localPosXZ : TEXCOORD2;
                float2 uv2 : TEXCOORD3; // heights
                float4 objectPos : TEXCOORD4; // object space position
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                o.uv = v.uv;
                o.localPosXZ = v.vertex.xz;
                o.uv2 = v.uv2; // heights
                o.objectPos = v.vertex; // Store object space position
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // total height in meters from UV2
                float totalHeightMeters = i.uv2.x;
                
                // i.uv.y represents the physical height ratio directly
                float physicalHeightRatio = i.uv.y; 
                
                // calc absolute height in meters
                float currentHeight = totalHeightMeters;
                // float currentHeight = physicalHeightRatio * totalHeightMeters;
                
                // height boundaries in real meters
                float height1 = 0.0; 
                float height2 = 7.0;
                float height3 = 14.0;
                float height4 = 21.0;

                // colors
                fixed3 color1 = fixed3(1.0, 0.0, 0.0);        // Red
                fixed3 color2 = fixed3(0.97, 0.98, 0.157);    // Yellow
                fixed3 color3 = fixed3(0.3, 1.0, 0.3);        // Green
                fixed3 color4 = fixed3(0.3, 0.3, 1.0);        // Blue
                fixed3 color5 = fixed3(1.0, 0.45, 0.925);     // Pink

                fixed3 baseColor;

                if (currentHeight < height1)
                {
                    baseColor = color1;
                }
                else if (currentHeight < height2)
                {
                    float t = (currentHeight - height1) / (height2 - height1);
                    baseColor = lerp(color1, color2, t);
                }
                else if (currentHeight < height3)
                {
                    float t = (currentHeight - height2) / (height3 - height2);
                    baseColor = lerp(color2, color3, t);
                }
                else if (currentHeight < height4)
                {
                    float t = (currentHeight - height3) / (height4 - height3);
                    baseColor = lerp(color3, color4, t);
                }
                else
                {
                    float t = (currentHeight - height4) / (height4 - height3);
                    baseColor = lerp(color4, color5, t);
                }

                // Calculate stripe based on absolute height
                float stripePhase = floor(currentHeight * _StripeFrequency);
                float stripe = fmod(stripePhase, 2.0) < 1.0 ? _StripeMax : _StripeMin;

                // More deterministic noise sampling by using fixed offsets
                float2 noiseUV1 = i.localPosXZ * _NoiseScale + _NoiseOffset.xy;
                float noiseValue1 = tex2D(_NoiseTex, noiseUV1).r;
                float noise1 = noiseValue1 * _NoiseIntensity;

                float2 noiseUV2 = i.localPosXZ * _NoiseScale2 + _NoiseOffset2.xy;
                float noiseValue2 = tex2D(_NoiseTex2, noiseUV2).r;
                float noise2 = noiseValue2 * _NoiseIntensity2;

                // Combine noises
                float combinedNoise = noise1 + noise2;
                float3 N = normalize(i.normal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);

                // Final color with stripes and noise
                fixed3 finalColor = baseColor * stripe + combinedNoise;
                finalColor = saturate(finalColor);

                // Diffuse lighting
                float NdotL = saturate(dot(N, L));
                float ambient = _AmbientLight;
                float diffuse = _DiffuseLight*pow(NdotL, _Shininess);

                fixed3 litColor = finalColor * (ambient + diffuse);
                fixed3 emission = finalColor * 0.3;   // adjustable
                finalColor = litColor + emission;

                float rim = 1.0 - saturate(dot(V, N));
                rim = pow(rim, 4.0);
                finalColor[2] += rim * 1.2;
                finalColor[1] += rim * 0.6;
                
                // ---------- TEXT RENDERING SECTION - FIXED WITH FLIP OPTIONS ----------
                
                // Calculate the angle of the current point in object space
                float angle = degrees(atan2(i.objectPos.z, i.objectPos.x));
                // Normalize to 0-360 range
                angle = fmod(angle + 360.0, 360.0);
                
                // Check if we're in the text display arc
                bool inTextArc = false;
                float textU = 0;
                
                if (_TextAngleStart < _TextAngleEnd) {
                    // Normal case - text doesn't cross 0/360 boundary
                    if (angle >= _TextAngleStart && angle <= _TextAngleEnd) {
                        inTextArc = true;
                        textU = (angle - _TextAngleStart) / (_TextAngleEnd - _TextAngleStart);
                    }
                } else {
                    // Text crosses 0/360 boundary (e.g., 300 to 60 degrees)
                    if (angle >= _TextAngleStart || angle <= _TextAngleEnd) {
                        inTextArc = true;
                        if (angle >= _TextAngleStart) {
                            textU = (angle - _TextAngleStart) / (360.0 - _TextAngleStart + _TextAngleEnd);
                        } else {
                            textU = (angle + 360.0 - _TextAngleStart) / (360.0 - _TextAngleStart + _TextAngleEnd);
                        }
                    }
                }
                
                if (inTextArc) {
                    // Apply horizontal flip if enabled - IMPORTANT FIX
                    float mappedU = _FlipTextHorizontal > 0.5 ? 1.0 - textU : textU;
                    
                    // Map vertical UV
                    float mappedV = (i.uv.y - _TextVMin) / (_TextVMax - _TextVMin);
                    
                    // Apply vertical flip if enabled
                    mappedV = _FlipTextVertical > 0.5 ? mappedV : 1.0 - mappedV;
                    
                    // Map from polar coordinates to texture UV
                    float2 textUV = float2(mappedU, mappedV);
                    
                    // Only render text if UV is within valid range
                    if (textUV.y >= 0 && textUV.y <= 1) {
                        float4 textColor = tex2D(_TextTex, textUV);
                        finalColor = lerp(finalColor, _TextColor.rgb, textColor.a * _TextColor.a);
                    }
                }

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
