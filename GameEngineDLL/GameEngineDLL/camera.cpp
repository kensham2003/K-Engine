#include "main.h"
#include "manager.h"
#include "renderer.h"
#include "camera.h"
#include "input.h"

void Camera::Init()
{
	m_GameObject->SetPosition(D3DXVECTOR3(0.0f, 2.0f, -5.0f));
	m_GameObject->SetRotation(D3DXVECTOR3(0.0f, 0.0f, 0.0f));
}


void Camera::Uninit()
{

}

void Camera::Update()
{

}

void Camera::Draw()
{
	if (!m_Active) { return; }

	//ビューマトリクス設定
	D3DXMATRIX viewMatrix;
	D3DXMATRIX world, rot, trans;
	D3DXVECTOR3 posVec, rotVec;
	posVec = m_GameObject->GetPosition();
	rotVec = m_GameObject->GetRotation();
	D3DXMatrixRotationYawPitchRoll(&rot, rotVec.y, rotVec.x, rotVec.z);
	D3DXMatrixTranslation(&trans, posVec.x, posVec.y, posVec.z);
	world = rot * trans;
	D3DXMatrixInverse(&viewMatrix, NULL, &world);

	Renderer::SetViewMatrix(&viewMatrix);



	//プロジェクションマトリクス設定
	D3DXMATRIX projectionMatrix;
	D3DXMatrixPerspectiveFovLH(&projectionMatrix, 1.0f,
		(float)Renderer::GetScreenWidth() / Renderer::GetScreenHeight(), 0.1f, 100.0f);

	m_ViewMatrix = viewMatrix;

	Renderer::SetProjectionMatrix(&projectionMatrix);
}

void Camera::MoveCamera() 
{
	bool directionInput = false;
	D3DXVECTOR3 cameraPosition = m_GameObject->GetPosition();
	D3DXVECTOR3 cameraMoveVelocity = D3DXVECTOR3(0.0f, 0.0f, 0.0f);

	if (Input::GetKeyPress('W')) //W
	{
		directionInput = true;
		D3DXVECTOR3 cameraForward = m_GameObject->GetForward();
		m_MoveSpeed += 0.01f;
		cameraMoveVelocity += cameraForward;

		if (Input::GetKeyPress('A'))
		{
			D3DXVECTOR3 cameraRight = m_GameObject->GetRight();
			cameraMoveVelocity -= cameraRight;
		}
		else if (Input::GetKeyPress('D'))
		{
			D3DXVECTOR3 cameraRight = m_GameObject->GetRight();
			cameraMoveVelocity += cameraRight;
		}
	}
	else if (Input::GetKeyPress('S')) //S
	{
		directionInput = true;
		D3DXVECTOR3 cameraForward = m_GameObject->GetForward();
		m_MoveSpeed += 0.01f;
		cameraMoveVelocity -= cameraForward;

		if (Input::GetKeyPress('A'))
		{
			D3DXVECTOR3 cameraRight = m_GameObject->GetRight();
			cameraMoveVelocity -= cameraRight;
		}
		else if (Input::GetKeyPress('D'))
		{
			D3DXVECTOR3 cameraRight = m_GameObject->GetRight();
			cameraMoveVelocity += cameraRight;
		}
	}
	else if (Input::GetKeyPress('A')) //A
	{
		directionInput = true;
		D3DXVECTOR3 cameraRight = m_GameObject->GetRight();
		m_MoveSpeed += 0.01f;
		cameraMoveVelocity -= cameraRight;

		if (Input::GetKeyPress('W'))
		{
			D3DXVECTOR3 cameraForward = m_GameObject->GetForward();
			cameraMoveVelocity += cameraForward;
		}
		else if (Input::GetKeyPress('S'))
		{
			D3DXVECTOR3 cameraForward = m_GameObject->GetForward();
			cameraMoveVelocity -= cameraForward;
		}
	}
	else if (Input::GetKeyPress('D'))
	{
		directionInput = true;
		D3DXVECTOR3 cameraRight = m_GameObject->GetRight();
		m_MoveSpeed += 0.01f;
		cameraMoveVelocity += cameraRight;

		if (Input::GetKeyPress('W'))
		{
			D3DXVECTOR3 cameraForward = m_GameObject->GetForward();
			cameraMoveVelocity += cameraForward;
		}
		else if (Input::GetKeyPress('S'))
		{
			D3DXVECTOR3 cameraForward = m_GameObject->GetForward();
			cameraMoveVelocity -= cameraForward;
		}
	}
	else {
		m_MoveSpeed = 0.0f;
	}

	if (m_MoveSpeed > 1.0f)
	{
		m_MoveSpeed = 1.0f;
	}

	if (directionInput)
	{
		D3DXVec3Normalize(&cameraMoveVelocity, &cameraMoveVelocity);
		cameraMoveVelocity *= m_MoveSpeed;
		//m_Position += cameraMoveVelocity;
		m_GameObject->MovePosition(cameraMoveVelocity);
	}
}

D3DXVECTOR3 Camera::GetRayFromScreen(float x, float y, float screenHeight, float screenWidth) {
	float scaledX, scaledY, scaledWidth;
	scaledWidth = screenHeight / (float)SCREEN_HEIGHT * (float)SCREEN_WIDTH;
	scaledX = x / (scaledWidth/2);
	scaledY = y / (screenHeight/2);
	D3DXMATRIX view = m_ViewMatrix;
	D3DXVECTOR3 viewX, viewY, viewZ;
	viewX = D3DXVECTOR3(view._11, view._21, view._31);
	viewY = D3DXVECTOR3(view._12, view._22, view._32);
	viewZ = D3DXVECTOR3(view._13, view._23, view._33);

	float fovy = 1.0f;
	float fovx = 1.54f; //なぜ1.54なのかわからないがこれで完璧にいける
	float yTotal = 1.0f * tanf(fovy / 2);
	float xTotal = 1.0f * tanf(fovx / 2);
	D3DXVECTOR3 dir = viewZ + viewX * scaledX * xTotal - viewY * scaledY * yTotal;
	D3DXVec3Normalize(&dir, &dir);
	return dir;
}