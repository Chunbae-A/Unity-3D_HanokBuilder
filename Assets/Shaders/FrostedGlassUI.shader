Shader "Custom/FrostedGlassUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _BlurSize ("Blur Size (texels)", Range(0, 20)) = 9

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 screenPos     : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _BlurSize;

            // URP가 Opaque Texture 옵션 활성화 시 매 프레임 전역으로 바인딩하는 화면 캡처 텍스처
            sampler2D _CameraOpaqueTexture;
            float4 _CameraOpaqueTexture_TexelSize;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 spriteColor = tex2D(_MainTex, IN.texcoord);

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 texel = _CameraOpaqueTexture_TexelSize.xy * _BlurSize;
                float2 texel2 = texel * 2.0;

                fixed3 blurCol = fixed3(0,0,0);
                // 1차 링 (반경 1×)
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(-texel.x, -texel.y)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(       0, -texel.y)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2( texel.x, -texel.y)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(-texel.x,        0)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(       0,        0)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2( texel.x,        0)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(-texel.x,  texel.y)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(       0,  texel.y)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2( texel.x,  texel.y)).rgb;
                // 2차 링 (반경 2× — 더 넓고 부드러운 확산)
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(-texel2.x, -texel2.y)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(        0, -texel2.y)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2( texel2.x, -texel2.y)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(-texel2.x,         0)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(        0,         0)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2( texel2.x,         0)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(-texel2.x,  texel2.y)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2(        0,  texel2.y)).rgb;
                blurCol += tex2D(_CameraOpaqueTexture, screenUV + float2( texel2.x,  texel2.y)).rgb;
                blurCol /= 18.0;

                fixed4 color;
                // 블러된 배경 위에 흰색 글래스 틴트를 IN.color.a 비율로 덧입힘
                color.rgb = lerp(blurCol, IN.color.rgb, IN.color.a);
                // 알파는 9-slice 스프라이트(둥근 모서리) 마스크를 그대로 사용
                color.a = spriteColor.a;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
