#pragma once
/////////////////////////////////////
///
///  SphereCollider�N���X
///  
///  �@�\�F�X�t�B�A�R���C�_�[��`�悷��
/// 
/////////////////////////////////////
class SphereCollider : public Model {
protected:

	float m_Size = 1.0f;

	D3DXVECTOR3 m_Offset = D3DXVECTOR3(0.0f, 0.0f, 0.0f);

public:
	void Init() override;
	void Draw() override;

	float GetSize() { return m_Size; }
	void SetSize(float size) { m_Size = size; }

	D3DXVECTOR3 GetOffset() { return m_Offset; }
	void SetOffset(D3DXVECTOR3 offset) { m_Offset = offset; }
};