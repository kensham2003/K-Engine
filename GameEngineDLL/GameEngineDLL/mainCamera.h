#pragma once


#include "component.h"

class MainCamera : public Component
{
private:
	D3DXMATRIX m_ViewMatrix;

	D3DXVECTOR3 m_Target = D3DXVECTOR3(0.0f, 0.0f, 0.0f);

	bool m_FocusTarget = false;

public:
	void Init();
	void Uninit();
	void Update();
	void Draw();

	void SetTargetPosition(D3DXVECTOR3 target);
	void SetFocusTarget(bool focus);
};