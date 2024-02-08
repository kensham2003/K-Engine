#include "main.h"
#include "renderer.h"
#include "model.h"
#include "sphereCollider.h"
#include "gameObject.h"

void SphereCollider::Init() {
	Renderer::CreateVertexShader(&m_VertexShader, &m_VertexLayout, "asset/shader/unlitTextureVS.cso");

	Renderer::CreatePixelShader(&m_PixelShader, "asset/shader/unlitTexturePS.cso");
}

void SphereCollider::Draw() {
	Renderer::ChangeRasterizerState();
	//Model::Draw();

	// ���̓��C�A�E�g�ݒ�
	Renderer::GetDeviceContext()->IASetInputLayout(m_VertexLayout);

	// �V�F�[�_�ݒ�
	Renderer::GetDeviceContext()->VSSetShader(m_VertexShader, NULL, 0);
	Renderer::GetDeviceContext()->PSSetShader(m_PixelShader, NULL, 0);


	// �}�g���N�X�ݒ�
	D3DXMATRIX world, scale, rot, trans;
	D3DXVECTOR3 posVec, rotVec, sclVec;
	posVec = m_GameObject->GetPosition();
	rotVec = m_GameObject->GetRotation();
	sclVec = m_GameObject->GetScale();
	posVec += m_Offset;
	sclVec *= m_Size;
	D3DXMatrixScaling(&scale, sclVec.x, sclVec.y, sclVec.z);
	D3DXMatrixRotationYawPitchRoll(&rot, rotVec.y, rotVec.x, rotVec.z);
	D3DXMatrixTranslation(&trans, posVec.x, posVec.y, posVec.z);
	world = scale * rot * trans;

	Renderer::SetWorldMatrix(&world);

	m_World = world;


	// ���_�o�b�t�@�ݒ�
	UINT stride = sizeof(VERTEX_3D);
	UINT offset = 0;
	Renderer::GetDeviceContext()->IASetVertexBuffers(0, 1, &m_VertexBuffer, &stride, &offset);

	// �C���f�b�N�X�o�b�t�@�ݒ�
	Renderer::GetDeviceContext()->IASetIndexBuffer(m_IndexBuffer, DXGI_FORMAT_R32_UINT, 0);

	// �v���~�e�B�u�g�|���W�ݒ�
	Renderer::GetDeviceContext()->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);


	for (unsigned int i = 0; i < m_SubsetNum; i++)
	{
		// �}�e���A���ݒ�
		Renderer::SetMaterial(m_SubsetArray[i].Material.Material);

		// �e�N�X�`���ݒ�
		if (m_SubsetArray[i].Material.Texture) {
			Renderer::GetDeviceContext()->PSSetShaderResources(0, 1, &m_SubsetArray[i].Material.Texture);
		}

		// �|���S���`��
		Renderer::GetDeviceContext()->DrawIndexed(m_SubsetArray[i].IndexNum, m_SubsetArray[i].StartIndex, 0);
	}


	Renderer::ChangeRasterizerState();
}
