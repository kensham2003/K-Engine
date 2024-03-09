#include "main.h"
#include "manager.h"
#include "renderer.h"
#include "mainCamera.h"
#include "input.h"

void MainCamera::Init()
{
	m_Active = false;
}


void MainCamera::Uninit()
{

}

void MainCamera::Update()
{

}

void MainCamera::Draw()
{
	if (!m_Active) { return; }

	D3DXMATRIX viewMatrix;
	if (m_FocusTarget) {
		//ビューマトリックス設定
		D3DXVECTOR3 up = D3DXVECTOR3(0.0f, 1.0f, 0.0f);
		D3DXVECTOR3 pos = m_GameObject->GetPosition();
		D3DXMatrixLookAtLH(&viewMatrix, &pos, &m_Target, &up);

		Renderer::SetViewMatrix(&viewMatrix);
	}
	else {
		//ビューマトリクス設定
		D3DXMATRIX world, rot, trans;
		D3DXVECTOR3 posVec, rotVec;
		posVec = m_GameObject->GetPosition();
		rotVec = m_GameObject->GetRotation();
		D3DXMatrixRotationYawPitchRoll(&rot, rotVec.y, rotVec.x, rotVec.z);
		D3DXMatrixTranslation(&trans, posVec.x, posVec.y, posVec.z);
		world = rot * trans;
		D3DXMatrixInverse(&viewMatrix, NULL, &world);

		Renderer::SetViewMatrix(&viewMatrix);
	}

	//プロジェクションマトリクス設定
	D3DXMATRIX projectionMatrix;
	D3DXMatrixPerspectiveFovLH(&projectionMatrix, 1.0f,
		(float)Renderer::GetScreenWidth() / Renderer::GetScreenHeight(), 0.1f, 100.0f);

	m_ViewMatrix = viewMatrix;

	Renderer::SetProjectionMatrix(&projectionMatrix);
}

void MainCamera::SetTargetPosition(D3DXVECTOR3 target)
{
	m_Target = target;
}

void MainCamera::SetFocusTarget(bool focus)
{
	m_FocusTarget = focus;
}
