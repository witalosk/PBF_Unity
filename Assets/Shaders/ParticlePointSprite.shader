Shader "Custom/ParticlePointSprite"
{
    Properties
    {
		_MainTex("Texture",         2D) = "black" {}
		_ParticleRadius("Particle Radius", Float) = 0.2
		_PointColor("PointColor", Color) = (1, 1, 1, 1)    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "GPUTrails.cginc"

            fixed4 _PointColor;
            float  _ParticleRadius;
            StructuredBuffer<Particle> _particleBuffer;

            struct v2g
            {
                float4 pos : SV_POSITION;
                float2 tex   : TEXCOORD0;
                float4 color : COLOR;
            };

            struct g2f
            {
                float4 pos   : POSITION;
                float2 tex   : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2g vert (uint id : SV_VertexID, uint instanceId : SV_InstanceID)
            {
                v2g o;
                o.pos = float4(_particleBuffer[instanceId].pos, 1);
                o.tex = float2(0, 0);
                o.color = _PointColor;
                return o;
            }

            [maxvertexcount(4)]
            void geom (point v2g input[1], inout TriangleStream<g2f> outStream)
            {
                g2f o;
                float4 pos = input[0].pos;
                float4 color = input[0].color;

                for (int x = 0; x < 2; x++) {
                    for (int y = 0; y < 2; y++) {
                        // ビュー行列から平行移動成分を取り除く
                        float4x4 billboardmatrix = UNITY_MATRIX_V;
                        billboardmatrix._m03 =
                        billboardmatrix._m13 = 
                        billboardmatrix._m23 =
                        billboardmatrix._m33 = 0;

                        // テクスチャ座標
                        o.tex = float2(x, y);

                        // 頂点位置
                        o.pos = pos + mul(float4((o.tex * 2.0 - float2(1, 1)) * _ParticleRadius, 0.0, 1.0), billboardmatrix);
                        o.pos = mul(UNITY_MATRIX_VP, o.pos);

                        // 色
                        o.color = color;

                        outStream.Append(o);
                    }
                }                

                outStream.RestartStrip();
            }

            fixed4 frag (g2f i) : SV_Target
            {
                // sample the texture
                fixed4 color = tex2D(_MainTex, i.tex) * i.color;
                if(color.a < 0.3) discard;    
                return color;
            }
            ENDCG
        }
    }
}
