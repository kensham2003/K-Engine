
#include "main.h"
#include "renderer.h"
#include "field.h"
#include "gameObject.h"



void Field::Init()
{
	VERTEX_3D vertex[4];

	vertex[0].Position = D3DXVECTOR3( -10.0f, 0.0f, 10.0f );
	vertex[0].Normal   = D3DXVECTOR3( 0.0f, 1.0f, 0.0f );
	vertex[0].Diffuse  = D3DXVECTOR4( 1.0f, 1.0f, 1.0f, 1.0f );
	vertex[0].TexCoord = D3DXVECTOR2( 0.0f, 0.0f );

	vertex[1].Position = D3DXVECTOR3( 10.0f, 0.0f, 10.0f );
	vertex[1].Normal   = D3DXVECTOR3( 0.0f, 1.0f, 0.0f );
	vertex[1].Diffuse  = D3DXVECTOR4( 1.0f, 1.0f, 1.0f, 1.0f );
	vertex[1].TexCoord = D3DXVECTOR2( 10.0f, 0.0f );
	{
		vertex[2].Position = D3DXVECTOR3(-10.0f, 0.0f, -10.0f);
		vertex[2].Normal = D3DXVECTOR3(0.0f, 1.0f, 0.0f);
		vertex[2].Diffuse = D3DXVECTOR4(1.0f, 1.0f, 1.0f, 1.0f);
		vertex[2].TexCoord = D3DXVECTOR2(0.0f, 10.0f);

		vertex[3].Position = D3DXVECTOR3(10.0f, 0.0f, -10.0f);
		vertex[3].Normal = D3DXVECTOR3(0.0f, 1.0f, 0.0f);
		vertex[3].Diffuse = D3DXVECTOR4(1.0f, 1.0f, 1.0f, 1.0f);
		vertex[3].TexCoord = D3DXVECTOR2(10.0f, 10.0f);
	}

	// 頂点バッファ生成
	D3D11_BUFFER_DESC bd;
	ZeroMemory( &bd, sizeof(bd) );
	bd.Usage = D3D11_USAGE_DEFAULT;
	bd.ByteWidth = sizeof( VERTEX_3D ) * 4;
	bd.BindFlags = D3D11_BIND_VERTEX_BUFFER;
	bd.CPUAccessFlags = 0;

	D3D11_SUBRESOURCE_DATA sd;
	ZeroMemory( &sd, sizeof(sd) );
	sd.pSysMem = vertex;

	Renderer::GetDevice()->CreateBuffer( &bd, &sd, &m_VertexBuffer );


	// テクスチャ読み込み
	D3DX11CreateShaderResourceViewFromFile(Renderer::GetDevice(),
											"asset/texture/floor_tiles_06_diff_1k.png",
											NULL,
											NULL,
											&m_Texture,
											NULL);
	assert( m_Texture );



	Renderer::CreateVertexShader(&m_VertexShader, &m_VertexLayout,
												"asset/shader/vertexLightingVS.cso");

	Renderer::CreatePixelShader(&m_PixelShader, "asset/shader/vertexLightingPS.cso");


	//m_GameObject->SetPosition(D3DXVECTOR3( 0.0f, 0.0f, 0.0f ));
	//m_GameObject->SetRotation(D3DXVECTOR3( 0.0f, 0.0f, 0.0f ));
	//m_GameObject->SetScale(D3DXVECTOR3( 1.0f, 1.0f, 1.0f ));

}


void Field::Uninit()
{

	m_VertexBuffer->Release();
	m_Texture->Release();

	m_VertexLayout->Release();
	m_VertexShader->Release();
	m_PixelShader->Release();

}


void Field::Update()
{

}


void Field::Draw()
{
	// 入力レイアウト設定
	Renderer::GetDeviceContext()->IASetInputLayout(m_VertexLayout);

	// シェーダ設定
	Renderer::GetDeviceContext()->VSSetShader(m_VertexShader, NULL, 0);
	Renderer::GetDeviceContext()->PSSetShader(m_PixelShader, NULL, 0);


	// ワールドマトリクス設定
	D3DXMATRIX world, scale, rot, trans;
	D3DXVECTOR3 posVec, rotVec, sclVec;
	posVec = m_GameObject->GetPosition();
	rotVec = m_GameObject->GetRotation();
	sclVec = m_GameObject->GetScale();
	D3DXMatrixScaling(&scale, sclVec.x, sclVec.y, sclVec.z);
	D3DXMatrixRotationYawPitchRoll(&rot, rotVec.y, rotVec.x, rotVec.z);
	D3DXMatrixTranslation(&trans, posVec.x, posVec.y, posVec.z);
	world = scale * rot * trans;
	Renderer::SetWorldMatrix(&world);


	// 頂点バッファ設定
	UINT stride = sizeof( VERTEX_3D );
	UINT offset = 0;
	Renderer::GetDeviceContext()->IASetVertexBuffers( 0, 1, &m_VertexBuffer, &stride, &offset );

	// マテリアル設定
	MATERIAL material;
	ZeroMemory( &material, sizeof(material) );
	material.Diffuse = D3DXCOLOR( 1.0f, 1.0f, 1.0f, 1.0f );
	material.TextureEnable = true;
	Renderer::SetMaterial( material );

	// テクスチャ設定
	Renderer::GetDeviceContext()->PSSetShaderResources(0, 1, &m_Texture);

	// プリミティブトポロジ設定
	Renderer::GetDeviceContext()->IASetPrimitiveTopology( D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP );

	// ポリゴン描画
	Renderer::GetDeviceContext()->Draw( 4, 0 );

}