#pragma once


class BoxCollider : public Model {
protected:

	D3DXVECTOR3 m_Size = D3DXVECTOR3(1.0f, 1.0f, 1.0f);

public:
	void Init() override;
	void Draw() override;

	D3DXVECTOR3 GetSize() { return m_Size; }
	void SetSize(D3DXVECTOR3 size) { m_Size = size; }
};