#ifndef GPUTRAILS_INCLUDED
#define GPUTRAILS_INCLUDED

struct Particle
{
	int index;			// ID
    float2 pos;			// 位置
	float2 vel;			// 速度
	float2 acc;			// 加速度
	float dens;			// 密度
	float2 force;		// 力
	float2 prevPos;		// 前ステップの位置
	float scalingFactor;	// スケーリングファクタ
	float2 deltaPos;		// 位置修正量
	int hash;			// ハッシュ値
};

struct Trail
{
	int currentNodeIdx;
};

struct Node
{
	float time;
    float3 pos;
};


uint _nodeNumPerTrail;

int ToNodeBufIdx(int trailIdx, int nodeIdx)
{
	nodeIdx %= _nodeNumPerTrail;
	return trailIdx * _nodeNumPerTrail + nodeIdx;
}

bool IsValid(Node node)
{
	return node.time >= 0;
}

#endif