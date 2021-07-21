#ifndef GPUTRAILS_INCLUDED
#define GPUTRAILS_INCLUDED

struct Particle
{
	int index;			// ID
    float3 pos;			// 位置
	float3 vel;			// 速度
	float dens;			// 密度
	float3 force;		// 力
	float3 prevPos;		// 前ステップの位置
	float scalingFactor;	// スケーリングファクタ
	float3 deltaPos;		// 位置修正量
	int hash;			// ハッシュ値
	int type;			// パーティクルタイプ
};

struct Trail
{
	int currentNodeIdx;
};

struct Node
{
	float time;
    float3 pos;
	float alpha;
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