#pragma once


#include "gameObject.h"

class Player : public GameObject
{

private:

	//Model* m_Model;

	ID3D11VertexShader*     m_VertexShader = NULL;
	ID3D11PixelShader*      m_PixelShader = NULL;
	ID3D11InputLayout*      m_VertexLayout = NULL;

	D3DXMATRIX m_World;

public:
	Player() : GameObject() {}
	Player(std::string name) : GameObject(name) {}

	void Init();
	void Load(const char* FileName);
	void Uninit();
	void Update();
	void Draw();

	//bool IsRayCollideModel(D3DXVECTOR3 ray, D3DXVECTOR3 cameraPos);
	//std::string GetModelIndexNum() { return std::to_string(m_Model->GetIndexNum()); }
};