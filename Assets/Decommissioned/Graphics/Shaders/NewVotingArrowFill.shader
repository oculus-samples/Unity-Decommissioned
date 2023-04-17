Shader "Unlit/NewVotingArrowFill"
{
    Properties
    {
        _FillAmount("FillAmount", Float) = 0
        _FillColor("FillColor", Color) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            CBUFFER_START(UnityPerMaterial)
                float _FillAmount;
                float4 _FillColor;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 normal : NORMAL;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                o.viewDir = ObjSpaceViewDir(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                clip(_FillAmount + i.uv.y - 1);
                float edge = saturate(1 - dot(normalize(i.viewDir), normalize(i.normal)));
                return _FillColor + float4(0.5.xxx, 0) * pow(edge, 1.5);
            }
            ENDCG
        }
    }
}
