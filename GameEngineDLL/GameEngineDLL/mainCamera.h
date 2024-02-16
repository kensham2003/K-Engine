#pragma once


#include "component.h"

class MainCamera : public Component
{
private:
	D3DXMATRIX m_ViewMatrix;

public:
	void Init();
	void Uninit();
	void Update();
	void Draw();
};