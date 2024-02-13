#pragma once
/////////////////////////////////////
///
///  BoxColliderクラス
///  
///  機能：OBBコライダーを描画する
/// 
/////////////////////////////////////
class BoxCollider : public Model {
protected:

	D3DXVECTOR3 m_Size = D3DXVECTOR3(1.0f, 1.0f, 1.0f);

	D3DXVECTOR3 m_Rotate = D3DXVECTOR3(0.0f, 0.0f, 0.0f);

	D3DXVECTOR3 m_Offset = D3DXVECTOR3(0.0f, 0.0f, 0.0f);

public:
	void Init() override;
	void Draw() override;

	D3DXVECTOR3 GetSize() { return m_Size; }
	void SetSize(D3DXVECTOR3 size) { m_Size = size; }

	D3DXVECTOR3 GetRotate() { return m_Rotate; }
	void SetRotate(D3DXVECTOR3 rotate) { m_Rotate = rotate; }

	D3DXVECTOR3 GetOffset() { return m_Offset; }
	void SetOffset(D3DXVECTOR3 offset) { m_Offset = offset; }
};