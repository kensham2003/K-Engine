#pragma once

#include "component.h"
#include <list>
#include <string>

class GameObject
{

protected://�p����̃N���X����A�N�Z�X�ł���

	bool m_Destroy = false; //���̃Q�[���I�u�W�F�N�g���폜���邩

	std::string m_Name;

	D3DXVECTOR3	m_Position{};
	D3DXVECTOR3	m_Rotation{};
	D3DXVECTOR3	m_Scale = D3DXVECTOR3(1.0f, 1.0f, 1.0f);

	std::list<Component*> m_Component;

public:
	GameObject() {}//�R���X�g���N�^
	GameObject(std::string name) :m_Name(name) {}
	virtual ~GameObject() {}//�f�X�g���N�^�i���z�֐��j

	//virtual void Init() = 0;//�������z�֐�
	//virtual void Uninit() = 0;
	//virtual void Update() = 0;
	//virtual void Draw() = 0;

	virtual void Init() {}

	virtual void Uninit() {
		for (Component* component : m_Component) {
			component->Uninit();
			delete component;
		}
		m_Component.clear();
	}
	virtual void Update() {
		for (Component* component : m_Component) {
			component->Update();
		}
	}
	virtual void Draw() {
		for (Component* component : m_Component) {
			component->Draw();
		}
	}

	//�폜������ݒ�
	void SetDestroy() { m_Destroy = true; }

	bool Destroy() {
		if (m_Destroy) {
			Uninit();
			delete this;
			return true;
		}
		else {
			return false;
		}
	}

	void SetPosition(D3DXVECTOR3 Position) { m_Position = Position; }
	void SetRotation(D3DXVECTOR3 Rotation) { m_Rotation = Rotation; }
	void SetScale(D3DXVECTOR3 Scale) { m_Scale = Scale; }

	D3DXVECTOR3 GetPosition() { return m_Position; }
	D3DXVECTOR3 GetRotation() { return m_Rotation; }
	D3DXVECTOR3 GetScale() { return m_Scale; }

	std::string GetName() { return m_Name; }
	void SetName(std::string name) { m_Name = name; }

	//�E�����x�N�g���擾�i���[�J�����W��X���j
	D3DXVECTOR3 GetRight() {
		D3DXMATRIX rot;
		//���K������Ă�
		D3DXMatrixRotationYawPitchRoll(&rot, m_Rotation.y, m_Rotation.x, m_Rotation.z);

		D3DXVECTOR3 right;
		right.x = rot._11;
		right.y = rot._12;
		right.z = rot._13;

		return right;
	}

	//������x�N�g���擾�i���[�J�����W��Y���j
	D3DXVECTOR3 GetTop() {
		D3DXMATRIX rot;
		//���K������Ă�
		D3DXMatrixRotationYawPitchRoll(&rot, m_Rotation.y, m_Rotation.x, m_Rotation.z);

		D3DXVECTOR3 top;
		top.x = rot._21;
		top.y = rot._22;
		top.z = rot._23;

		return top;
	}

	//�O�����x�N�g���擾�i���[�J�����W��Z���j
	D3DXVECTOR3 GetForward() {
		D3DXMATRIX rot;
		//���K������Ă�
		D3DXMatrixRotationYawPitchRoll(&rot, m_Rotation.y, m_Rotation.x, m_Rotation.z);

		D3DXVECTOR3 forward;
		forward.x = rot._31;
		forward.y = rot._32;
		forward.z = rot._33;

		return forward;
	}

	//�ʒu��vec�̕����ړ�����
	void MovePosition(D3DXVECTOR3 vec) {
		m_Position += vec;
	}


	//�E��amount���j�b�g�����ړ�����
	void MoveRight(float amount) {
		m_Position += GetRight() * amount;
	}

	//���amount���j�b�g�����ړ�����
	void MoveTop(float amount) {
		m_Position += GetTop() * amount;
	}

	//�O��amount���j�b�g�����ړ�����
	void MoveForward(float amount) {
		m_Position += GetForward() * amount;
	}

	//��]��vec�̕�����
	void Rotate(D3DXVECTOR3 vec) {
		m_Rotation += vec;
	}

	//�X�P�[����vec���グ��
	void AddScale(D3DXVECTOR3 vec) {
		if (m_Scale.x + vec.x < 0.01f)return;
		if (m_Scale.y + vec.y < 0.01f)return;
		if (m_Scale.z + vec.z < 0.01f)return;
		m_Scale += vec;
	}


	//�R���|�[�l���g��ǉ�
	template <typename T> //�e���v���[�g�֐�
	T* AddComponent() {
		Component* component = new T();
		component->m_GameObject = this;
		m_Component.push_back(component);
		component->Init();

		return (T*)component;
	}

	//�R���|�[�l���g���擾
	template <typename T>
	T* GetComponent() {
		for (Component* component : m_Component) {
			if (typeid(*component) == typeid(T)) { //�^�𒲂ׂ�(RTTI���I�^���)
				return (T*)component;
			}
		}
		return nullptr;
	}

};