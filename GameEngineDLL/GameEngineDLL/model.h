#pragma once
#include "component.h"


// マテリアル構造体
struct MODEL_MATERIAL
{
	char						Name[256];
	MATERIAL					Material;
	char						TextureName[256];
	ID3D11ShaderResourceView*	Texture;

};


// 描画サブセット構造体
struct SUBSET
{
	unsigned int	StartIndex;
	unsigned int	IndexNum;
	MODEL_MATERIAL	Material;
};


// モデル構造体
struct MODEL
{
	VERTEX_3D		*VertexArray;
	unsigned int	VertexNum;

	unsigned int	*IndexArray;
	unsigned int	IndexNum;

	SUBSET			*SubsetArray;
	unsigned int	SubsetNum;
};





class Model: public Component
{
protected:

	ID3D11VertexShader*     m_VertexShader = NULL;
	ID3D11PixelShader*      m_PixelShader = NULL;
	ID3D11InputLayout*      m_VertexLayout = NULL;

	D3DXMATRIX m_World;

	ID3D11Buffer*	m_VertexBuffer;
	ID3D11Buffer*	m_IndexBuffer;

	SUBSET*	m_SubsetArray;
	unsigned int	m_SubsetNum;

	void LoadObj( const char *FileName, MODEL *Model );
	void LoadMaterial( const char *FileName, MODEL_MATERIAL **MaterialArray, unsigned int *MaterialNum );

	VERTEX_3D* m_VertexArray;
	unsigned int m_VertexNum;
	unsigned int* m_IndexArray;
	unsigned int m_IndexNum;

public:
	virtual void Init();
	void Uninit();
	void Update();
	virtual void Draw();

	void Load( const char *FileName );
	void Unload();

	float IsRayCollide(D3DXVECTOR3 ray, D3DXVECTOR3 cameraPos);
	int GetIndexNum() { return m_IndexNum; }

	bool GetMaterialTextureEnable() { return m_SubsetArray[0].Material.Material.TextureEnable; }
	int GetSubsetNum() { return m_SubsetNum; }
};