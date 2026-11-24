Shader "ToyGarden/Painted Toys"
{
    Properties { _Color ("Paint", Color) = (1,1,1,1)
        _Visibility ("Camera visibility", Range(0,1)) = 1 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            fixed4 _Color;
            half _Visibility;
            struct v2f
            {
                float4 pos : SV_POSITION;
                half3 normal : TEXCOORD0;
                SHADOW_COORDS(1)
                UNITY_FOG_COORDS(2)
            };
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal);
                TRANSFER_SHADOW(o)
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                if (_Visibility < .999h)
                {
                    float threshold=frac(dot(floor(i.pos.xy),float2(.75487766,.56984029)));
                    clip(_Visibility-threshold-.001);
                }
                half light=saturate(dot(normalize(i.normal),normalize(_WorldSpaceLightPos0.xyz)));
                half shade=.70h+.30h*light*SHADOW_ATTENUATION(i);
                fixed4 col=fixed4(_Color.rgb*shade,1);
                UNITY_APPLY_FOG(i.fogCoord,col);
                return col;
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    Fallback "VertexLit"
}
