Shader "Custom/TrailsShader"
{
    Properties {
        _width("Width", Float) = 0.1
        _startColor("StartColor", Color) = (1,1,1,1)
        _endColor("EndColor", Color) = (0,0,0,1)
    }
    
    SubShader {
        Pass{
            Cull Off Fog { Mode Off } ZWrite Off 
            Blend One One

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "GPUTrails.cginc"

            float _width;
            float _life;
            float4 _startColor;
            float4 _endColor;
            float _time;
            StructuredBuffer<Trail> _trailBuffer;
            StructuredBuffer<Node> _nodeBuffer;

            Node GetNode(int trailIdx, int nodeIdx)
            {
                return _nodeBuffer[ToNodeBufIdx(trailIdx, nodeIdx)];
            }

            struct v2g {
                float4 pos : POSITION0;
                float3 dir : TANGENT0;
                float4 col : COLOR0;
                float4 posNext: POSITION1;
                float3 dirNext : TANGENT1;
                float4 colNext : COLOR1;
            };

            struct g2f {
                float4 pos : SV_POSITION;
                float4 col : COLOR;
            };

            v2g vert (uint id : SV_VertexID, uint instanceId : SV_InstanceID)
            {
                v2g o;
                Trail trail = _trailBuffer[instanceId];
                int currentNodeIdx = trail.currentNodeIdx; // 今見ているノードIndex

                Node node0 = GetNode(instanceId, id-1);
                Node node1 = GetNode(instanceId, id); // 今見ているノード
                Node node2 = GetNode(instanceId, id+1);
                Node node3 = GetNode(instanceId, id+2);
                Node node4 = GetNode(instanceId, id+3);

                bool isLatestNode = (currentNodeIdx == (int)id);

                // 最新のノードであるかノードが未初期化の場合は同じノードとする(折り畳んでいる)
                if ( isLatestNode || !IsValid(node1))
                {
                    node0 = node1 = node2 = node3 = GetNode(instanceId, currentNodeIdx);
                }
                
                float3 pos1 = node1.pos;
                float3 pos0 = IsValid(node0) ? node0.pos : pos1;
                float3 pos2 = IsValid(node2) ? node2.pos : pos1;
                float3 pos3 = IsValid(node3) ? node3.pos : pos2;

                o.pos = float4(pos1, 1);
                o.posNext = float4(pos2, 1);

                // そのノードが向いている方向を計算
                o.dir = normalize(pos2 - pos0);
                o.dirNext = normalize(pos3 - pos1);

                // 色の計算
                float ageRate = saturate((_time - node1.time) / _life);
                float ageRateNext = saturate((_time - node2.time) / _life);
                o.col = lerp(_startColor, _endColor, ageRate) * node1.alpha;
                o.colNext = lerp(_startColor, _endColor, ageRateNext) * node1.alpha;

                return o;
            }

            [maxvertexcount(4)]
            void geom (point v2g input[1], inout TriangleStream<g2f> outStream)
            {
                g2f output0, output1, output2, output3;
                float3 pos = input[0].pos; 
                float3 dir = input[0].dir;
                float3 posNext = input[0].posNext; 
                float3 dirNext = input[0].dirNext;

                // 点の場所からカメラへの方向ベクトルと接戦ベクトルの外積から
                // ラインの幅を広げる方向を計算している
                float3 camPos = _WorldSpaceCameraPos;
                float3 toCamDir = normalize(camPos - pos);
                float3 sideDir = normalize(cross(toCamDir, dir));

                float3 toCamDirNext = normalize(camPos - posNext);
                float3 sideDirNext = normalize(cross(toCamDirNext, dirNext));
                float width = _width * 0.5;

                output0.pos = UnityWorldToClipPos(pos + (sideDir * width));
                output1.pos = UnityWorldToClipPos(pos - (sideDir * width));
                output2.pos = UnityWorldToClipPos(posNext + (sideDirNext * width));
                output3.pos = UnityWorldToClipPos(posNext - (sideDirNext * width));

                output0.col =
                output1.col = input[0].col;
                output2.col =
                output3.col = input[0].colNext;

                outStream.Append (output0);
                outStream.Append (output1);
                outStream.Append (output2);
                outStream.Append (output3);
            
                outStream.RestartStrip();
            }

            fixed4 frag (g2f In) : COLOR
            {
                return In.col;
            }

            ENDCG
        
        }
    }

}
