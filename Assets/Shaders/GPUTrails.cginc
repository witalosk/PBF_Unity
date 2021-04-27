#ifndef GPUTRAILS_INCLUDED
#define GPUTRAILS_INCLUDED

struct Particle
{
    float3 pos;			// 位置
	float3 vel;			// 速度
	float3 acc;			// 加速度
	float dens;			// 密度
	float3 force;		// 力
	float3 prevPos;		// 前ステップの位置
	float3 prevVel;		// 前ステップの速度
	float scalingFactor;	// スケーリングファクタ
	float3 deltaPos;		// 位置修正量
	float deltaDens;	// 密度変動量
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