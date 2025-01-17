#include "main.h"
#include "renderer.h"
#include "model.h"
#include "player.h"

void Player::Init()
{
	m_Position = D3DXVECTOR3( 0.0f, 0.0f, 0.0f );
	m_Rotation = D3DXVECTOR3( 0.0f, 0.0f, 0.0f );
	m_Scale = D3DXVECTOR3( 1.0f, 1.0f, 1.0f );

	Renderer::CreateVertexShader(&m_VertexShader, &m_VertexLayout, "asset/shader/vertexLightingVS.cso");

	Renderer::CreatePixelShader(&m_PixelShader, "asset/shader/vertexLightingPS.cso");

	
}

void Player::Load(const char* FileName)
{

	//m_Model = new Model();
	//m_Model->Load(FileName);

}


void Player::Uninit()
{
	//m_Model->Unload();
	//delete m_Model;

	m_VertexLayout->Release();
	m_VertexShader->Release();
	m_PixelShader->Release();
}


void Player::Update()
{
	m_Rotation.x += 0.1f;
	//m_Rotation.y += 0.1f;
}

void Player::Draw()
{
	// 入力レイアウト設定
	Renderer::GetDeviceContext()->IASetInputLayout(m_VertexLayout);

	// シェーダ設定
	Renderer::GetDeviceContext()->VSSetShader(m_VertexShader, NULL, 0);
	Renderer::GetDeviceContext()->PSSetShader(m_PixelShader, NULL, 0);


	// マトリクス設定
	D3DXMATRIX world, scale, rot, trans;
	D3DXMatrixScaling(&scale, m_Scale.x, m_Scale.y, m_Scale.z);
	D3DXMatrixRotationYawPitchRoll(&rot, m_Rotation.y, m_Rotation.x, m_Rotation.z);
	D3DXMatrixTranslation(&trans, m_Position.x, m_Position.y, m_Position.z);
	world = scale * rot * trans;

	Renderer::SetWorldMatrix(&world);

	m_World = world;

	//m_Model->Draw();
}


//bool Player::IsRayCollideModel(D3DXVECTOR3 ray, D3DXVECTOR3 cameraPos) {
//	return m_Model->IsRayCollide(ray, cameraPos, m_World);
//}