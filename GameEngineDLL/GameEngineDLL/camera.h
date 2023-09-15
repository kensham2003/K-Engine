#pragma once


#include "gameObject.h"

class Camera : public GameObject
{

private:
	float m_MoveSpeed = 0.0f;

public:
	void Init();
	void Uninit();
	void Update();
	void Draw();

	void MoveCamera();
	void ResetSpeed() { m_MoveSpeed = 0.0f; }
};