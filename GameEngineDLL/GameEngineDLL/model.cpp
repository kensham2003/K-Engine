
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <shlwapi.h>
#pragma comment(lib, "shlwapi.lib")

#include "main.h"
#include "renderer.h"
#include "model.h"
#include "gameObject.h"

bool RayTriangleCollision(D3DXVECTOR3 ray, D3DXVECTOR3 cameraPos, D3DXVECTOR3* triangle, D3DXVECTOR3& outIntersectionPoint);

void Model::Init()
{
	Renderer::CreateVertexShader(&m_VertexShader, &m_VertexLayout, "asset/shader/vertexLightingVS.cso");

	Renderer::CreatePixelShader(&m_PixelShader, "asset/shader/vertexLightingPS.cso");
}

void Model::Uninit()
{
	Unload();

	m_VertexLayout->Release();
	m_VertexShader->Release();
	m_PixelShader->Release();
}

void Model::Update()
{
	D3DXVECTOR3 rot = m_GameObject->GetRotation();
	rot.x += 0.01f;
	m_GameObject->SetRotation(rot);
}

void Model::Draw()
{
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


	for( unsigned int i = 0; i < m_SubsetNum; i++ )
	{
		// �}�e���A���ݒ�
		Renderer::SetMaterial( m_SubsetArray[i].Material.Material );

		// �e�N�X�`���ݒ�
		Renderer::GetDeviceContext()->PSSetShaderResources( 0, 1, &m_SubsetArray[i].Material.Texture );

		// �|���S���`��
		Renderer::GetDeviceContext()->DrawIndexed( m_SubsetArray[i].IndexNum, m_SubsetArray[i].StartIndex, 0 );
	}

}



void Model::Load( const char *FileName )
{

	MODEL model;
	LoadObj( FileName, &model );

	m_VertexArray = model.VertexArray;
	m_IndexArray = model.IndexArray;
	m_VertexNum = model.VertexNum;
	m_IndexNum = model.IndexNum;

	// ���_�o�b�t�@����
	{
		D3D11_BUFFER_DESC bd;
		ZeroMemory( &bd, sizeof(bd) );
		bd.Usage = D3D11_USAGE_DEFAULT;
		bd.ByteWidth = sizeof( VERTEX_3D ) * model.VertexNum;
		bd.BindFlags = D3D11_BIND_VERTEX_BUFFER;
		bd.CPUAccessFlags = 0;

		D3D11_SUBRESOURCE_DATA sd;
		ZeroMemory( &sd, sizeof(sd) );
		sd.pSysMem = model.VertexArray;

		Renderer::GetDevice()->CreateBuffer( &bd, &sd, &m_VertexBuffer );
	}


	// �C���f�b�N�X�o�b�t�@����
	{
		D3D11_BUFFER_DESC bd;
		ZeroMemory( &bd, sizeof(bd) );
		bd.Usage = D3D11_USAGE_DEFAULT;
		bd.ByteWidth = sizeof( unsigned int ) * model.IndexNum;
		bd.BindFlags = D3D11_BIND_INDEX_BUFFER;
		bd.CPUAccessFlags = 0;

		D3D11_SUBRESOURCE_DATA sd;
		ZeroMemory( &sd, sizeof(sd) );
		sd.pSysMem = model.IndexArray;

		Renderer::GetDevice()->CreateBuffer( &bd, &sd, &m_IndexBuffer );
	}

	// �T�u�Z�b�g�ݒ�
	{
		m_SubsetArray = new SUBSET[ model.SubsetNum ];
		m_SubsetNum = model.SubsetNum;

		for( unsigned int i = 0; i < model.SubsetNum; i++ )
		{
			m_SubsetArray[i].StartIndex = model.SubsetArray[i].StartIndex;
			m_SubsetArray[i].IndexNum = model.SubsetArray[i].IndexNum;

			m_SubsetArray[i].Material.Material = model.SubsetArray[i].Material.Material;

			m_SubsetArray[i].Material.Texture = NULL;

			D3DX11CreateShaderResourceViewFromFile(Renderer::GetDevice(),
				model.SubsetArray[i].Material.TextureName,
				NULL,
				NULL,
				&m_SubsetArray[i].Material.Texture,
				NULL);

			assert(m_SubsetArray[i].Material.Texture);

		}
	}

	//delete[] model.VertexArray;
	//delete[] model.IndexArray;
	delete[] model.SubsetArray;

}





void Model::Unload()
{
	m_VertexBuffer->Release();
	m_IndexBuffer->Release();

	for (unsigned int i = 0; i < m_SubsetNum; i++)
	{
		m_SubsetArray[i].Material.Texture->Release();
	}

	delete[] m_SubsetArray;
	delete[] m_IndexArray;
	delete[] m_VertexArray;
}





//���f���Ǎ�////////////////////////////////////////////
void Model::LoadObj( const char *FileName, MODEL *Model )
{

	char dir[MAX_PATH];
	strcpy (dir, FileName );
	PathRemoveFileSpec(dir);





	D3DXVECTOR3	*positionArray;
	D3DXVECTOR3	*normalArray;
	D3DXVECTOR2	*texcoordArray;

	unsigned int	positionNum = 0;
	unsigned int	normalNum = 0;
	unsigned int	texcoordNum = 0;
	unsigned int	vertexNum = 0;
	unsigned int	indexNum = 0;
	unsigned int	in = 0;
	unsigned int	subsetNum = 0;

	MODEL_MATERIAL	*materialArray = NULL;
	unsigned int	materialNum = 0;

	char str[256];
	char *s;
	char c;


	FILE *file;
	file = fopen( FileName, "rt" );
	assert(file);



	//�v�f���J�E���g
	while( true )
	{
		fscanf( file, "%s", str );

		if( feof( file ) != 0 )
			break;

		if( strcmp( str, "v" ) == 0 )
		{
			positionNum++;
		}
		else if( strcmp( str, "vn" ) == 0 )
		{
			normalNum++;
		}
		else if( strcmp( str, "vt" ) == 0 )
		{
			texcoordNum++;
		}
		else if( strcmp( str, "usemtl" ) == 0 )
		{
			subsetNum++;
		}
		else if( strcmp( str, "f" ) == 0 )
		{
			in = 0;

			do
			{
				fscanf( file, "%s", str );
				vertexNum++;
				in++;
				c = fgetc( file );
			}
			while( c != '\n' && c!= '\r' );

			//�l�p�͎O�p�ɕ���
			if( in == 4 )
				in = 6;

			indexNum += in;
		}
	}


	//�������m��
	positionArray = new D3DXVECTOR3[ positionNum ];
	normalArray = new D3DXVECTOR3[ normalNum ];
	texcoordArray = new D3DXVECTOR2[ texcoordNum ];


	Model->VertexArray = new VERTEX_3D[ vertexNum ];
	Model->VertexNum = vertexNum;

	Model->IndexArray = new unsigned int[ indexNum ];
	Model->IndexNum = indexNum;

	Model->SubsetArray = new SUBSET[ subsetNum ];
	Model->SubsetNum = subsetNum;




	//�v�f�Ǎ�
	D3DXVECTOR3 *position = positionArray;
	D3DXVECTOR3 *normal = normalArray;
	D3DXVECTOR2 *texcoord = texcoordArray;

	unsigned int vc = 0;
	unsigned int ic = 0;
	unsigned int sc = 0;


	fseek( file, 0, SEEK_SET );

	while( true )
	{
		fscanf( file, "%s", str );

		if( feof( file ) != 0 )
			break;

		if( strcmp( str, "mtllib" ) == 0 )
		{
			//�}�e���A���t�@�C��
			fscanf( file, "%s", str );

			char path[256];
			strcpy( path, dir );
			strcat( path, "\\" );
			strcat( path, str );

			LoadMaterial( path, &materialArray, &materialNum );
		}
		else if( strcmp( str, "o" ) == 0 )
		{
			//�I�u�W�F�N�g��
			fscanf( file, "%s", str );
		}
		else if( strcmp( str, "v" ) == 0 )
		{
			//���_���W
			fscanf( file, "%f", &position->x );
			fscanf( file, "%f", &position->y );
			fscanf( file, "%f", &position->z );
			position++;
		}
		else if( strcmp( str, "vn" ) == 0 )
		{
			//�@��
			fscanf( file, "%f", &normal->x );
			fscanf( file, "%f", &normal->y );
			fscanf( file, "%f", &normal->z );
			normal++;
		}
		else if( strcmp( str, "vt" ) == 0 )
		{
			//�e�N�X�`�����W
			fscanf( file, "%f", &texcoord->x );
			fscanf( file, "%f", &texcoord->y );
			texcoord->y = 1.0f - texcoord->y;
			texcoord++;
		}
		else if( strcmp( str, "usemtl" ) == 0 )
		{
			//�}�e���A��
			fscanf( file, "%s", str );

			if( sc != 0 )
				Model->SubsetArray[ sc - 1 ].IndexNum = ic - Model->SubsetArray[ sc - 1 ].StartIndex;

			Model->SubsetArray[ sc ].StartIndex = ic;


			for( unsigned int i = 0; i < materialNum; i++ )
			{
				if( strcmp( str, materialArray[i].Name ) == 0 )
				{
					Model->SubsetArray[ sc ].Material.Material = materialArray[i].Material;
					strcpy( Model->SubsetArray[ sc ].Material.TextureName, materialArray[i].TextureName );
					strcpy( Model->SubsetArray[ sc ].Material.Name, materialArray[i].Name );

					break;
				}
			}

			sc++;
			
		}
		else if( strcmp( str, "f" ) == 0 )
		{
			//��
			in = 0;

			do
			{
				fscanf( file, "%s", str );

				s = strtok( str, "/" );	
				Model->VertexArray[vc].Position = positionArray[ atoi( s ) - 1 ];
				if( s[ strlen( s ) + 1 ] != '/' )
				{
					//�e�N�X�`�����W�����݂��Ȃ��ꍇ������
					s = strtok( NULL, "/" );
					Model->VertexArray[vc].TexCoord = texcoordArray[ atoi( s ) - 1 ];
				}
				s = strtok( NULL, "/" );	
				Model->VertexArray[vc].Normal = normalArray[ atoi( s ) - 1 ];

				Model->VertexArray[vc].Diffuse = D3DXVECTOR4( 1.0f, 1.0f, 1.0f, 1.0f );

				Model->IndexArray[ic] = vc;
				ic++;
				vc++;

				in++;
				c = fgetc( file );
			}
			while( c != '\n' && c != '\r' );

			//�l�p�͎O�p�ɕ���
			if( in == 4 )
			{
				Model->IndexArray[ic] = vc - 4;
				ic++;
				Model->IndexArray[ic] = vc - 2;
				ic++;
			}
		}
	}


	if( sc != 0 )
		Model->SubsetArray[ sc - 1 ].IndexNum = ic - Model->SubsetArray[ sc - 1 ].StartIndex;


	fclose( file );


	delete[] positionArray;
	delete[] normalArray;
	delete[] texcoordArray;
	delete[] materialArray;
}




//�}�e���A���ǂݍ���///////////////////////////////////////////////////////////////////
void Model::LoadMaterial( const char *FileName, MODEL_MATERIAL **MaterialArray, unsigned int *MaterialNum )
{

	char dir[MAX_PATH];
	strcpy(dir, FileName);
	PathRemoveFileSpec(dir);



	char str[256];

	FILE *file;
	file = fopen( FileName, "rt" );
	assert(file);

	MODEL_MATERIAL *materialArray;
	unsigned int materialNum = 0;

	//�v�f���J�E���g
	while( true )
	{
		fscanf( file, "%s", str );

		if( feof( file ) != 0 )
			break;


		if( strcmp( str, "newmtl" ) == 0 )
		{
			materialNum++;
		}
	}


	//�������m��
	materialArray = new MODEL_MATERIAL[ materialNum ];


	//�v�f�Ǎ�
	int mc = -1;

	fseek( file, 0, SEEK_SET );

	while( true )
	{
		fscanf( file, "%s", str );

		if( feof( file ) != 0 )
			break;


		if( strcmp( str, "newmtl" ) == 0 )
		{
			//�}�e���A����
			mc++;
			fscanf( file, "%s", materialArray[ mc ].Name );
			strcpy( materialArray[ mc ].TextureName, "" );

			materialArray[mc].Material.Emission.r = 0.0f;
			materialArray[mc].Material.Emission.g = 0.0f;
			materialArray[mc].Material.Emission.b = 0.0f;
			materialArray[mc].Material.Emission.a = 0.0f;
		}
		else if( strcmp( str, "Ka" ) == 0 )
		{
			//�A���r�G���g
			fscanf( file, "%f", &materialArray[ mc ].Material.Ambient.r );
			fscanf( file, "%f", &materialArray[ mc ].Material.Ambient.g );
			fscanf( file, "%f", &materialArray[ mc ].Material.Ambient.b );
			materialArray[ mc ].Material.Ambient.a = 1.0f;
		}
		else if( strcmp( str, "Kd" ) == 0 )
		{
			//�f�B�t���[�Y
			fscanf( file, "%f", &materialArray[ mc ].Material.Diffuse.r );
			fscanf( file, "%f", &materialArray[ mc ].Material.Diffuse.g );
			fscanf( file, "%f", &materialArray[ mc ].Material.Diffuse.b );
			materialArray[ mc ].Material.Diffuse.a = 1.0f;
		}
		else if( strcmp( str, "Ks" ) == 0 )
		{
			//�X�y�L����
			fscanf( file, "%f", &materialArray[ mc ].Material.Specular.r );
			fscanf( file, "%f", &materialArray[ mc ].Material.Specular.g );
			fscanf( file, "%f", &materialArray[ mc ].Material.Specular.b );
			materialArray[ mc ].Material.Specular.a = 1.0f;
		}
		else if( strcmp( str, "Ns" ) == 0 )
		{
			//�X�y�L�������x
			fscanf( file, "%f", &materialArray[ mc ].Material.Shininess );
		}
		else if( strcmp( str, "d" ) == 0 )
		{
			//�A���t�@
			fscanf( file, "%f", &materialArray[ mc ].Material.Diffuse.a );
		}
		else if( strcmp( str, "map_Kd" ) == 0 )
		{
			//�e�N�X�`��
			fscanf( file, "%s", str );

			char path[256];
			strcpy( path, dir );
			strcat( path, "\\" );
			strcat( path, str );

			strcat( materialArray[ mc ].TextureName, path );
		}
	}

	fclose(file);

	*MaterialArray = materialArray;
	*MaterialNum = materialNum;
}

float Model::IsRayCollide(D3DXVECTOR3 ray, D3DXVECTOR3 cameraPos) {
	D3DXVECTOR3 triangle[3];

	//�J�����������[���h���W�����[�J�����W�i���ōςނ̂ő����j
	D3DXVECTOR3 localRay, localCameraPos, intersection;
	D3DXMATRIX worldToLocal;
	D3DXMatrixInverse(&worldToLocal, NULL, &m_World);
	D3DXVec3TransformNormal(&localRay, &ray, &worldToLocal);//�����x�N�g����TransformNormal���g��
	D3DXVec3TransformCoord(&localCameraPos, &cameraPos, &worldToLocal);//���W�x�N�g����TransformCoord���g��

	for (int i = 0; i < m_IndexNum - 2; i++) {
		for (int j = 0; j < 3; j++) {
			triangle[j] = m_VertexArray[m_IndexArray[i + j]].Position;
			////���_�̃��[�J�����W�����[���h���W�i�S���v�Z����̂Œx���j
			//D3DXVec3TransformCoord(&triangle[j], &triangle[j], &m_World);
		}
		if (RayTriangleCollision(localRay, localCameraPos, triangle, intersection)) {
			D3DXVECTOR3 distance = intersection - localCameraPos;
			return D3DXVec3Length(&distance);
		}
		//if (RayTriangleCollision(ray, cameraPos, triangle)) { return true; }
	}
	return -1.0f;
}

bool RayTriangleCollision(D3DXVECTOR3 ray, D3DXVECTOR3 cameraPos, D3DXVECTOR3* triangle, D3DXVECTOR3& outIntersectionPoint) {
	const float eps = 0.00001f;
	D3DXVECTOR3 edge1, edge2, h, s, q;
	float a, f, u, v;
	edge1 = triangle[1] - triangle[0];
	edge2 = triangle[2] - triangle[0];
	D3DXVec3Cross(&h, &ray, &edge2);
	a = D3DXVec3Dot(&edge1, &h);

	//ray is parallel to triangle
	if (a > -eps && a < eps) { return false; }

	f = 1.0f / a;
	s = cameraPos - triangle[0];
	u = f * D3DXVec3Dot(&s, &h);

	if (u < 0.0f || u > 1.0f) { return false; }

	D3DXVec3Cross(&q, &s, &edge1);
	v = f * D3DXVec3Dot(&ray, &q);

	if (v < 0.0f || (u + v)>1.0f) { return false; }

	float t = f * D3DXVec3Dot(&edge2, &q);

	if (t > eps) {
		outIntersectionPoint = cameraPos + ray * t;
		return true; 
	}
	else { return false; }
}