#pragma once


#include "component.h"

class Camera : public Component
{
private:
	float m_MoveSpeed = 0.0f;

	D3DXMATRIX m_ViewMatrix;

public:
	void Init();
	void Uninit();
	void Update();
	void Draw();

	void MoveCamera();
	void ResetSpeed() { m_MoveSpeed = 0.0f; }

	D3DXVECTOR3 GetRayFromScreen(float x, float y, float screenHeight, float screenWidth);
};