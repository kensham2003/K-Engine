#pragma once

#include "component.h"
#include <list>
#include <string>

class GameObject
{

protected://継承先のクラスからアクセスできる

	bool m_Destroy = false; //このゲームオブジェクトを削除するか

	std::string m_Name;

	D3DXVECTOR3	m_Position{};
	D3DXVECTOR3	m_Rotation{};
	D3DXVECTOR3	m_Scale = D3DXVECTOR3(1.0f, 1.0f, 1.0f);

	std::list<Component*> m_Component;

public:
	GameObject() {}//コンストラクタ
	GameObject(std::string name) :m_Name(name) {}
	virtual ~GameObject() {}//デストラクタ（仮想関数）

	//virtual void Init() = 0;//純粋仮想関数
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

	//削除処理を設定
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

	//右方向ベクトル取得（ローカル座標のX軸）
	D3DXVECTOR3 GetRight() {
		D3DXMATRIX rot;
		//正規化されてる
		D3DXMatrixRotationYawPitchRoll(&rot, m_Rotation.y, m_Rotation.x, m_Rotation.z);

		D3DXVECTOR3 right;
		right.x = rot._11;
		right.y = rot._12;
		right.z = rot._13;

		return right;
	}

	//上方向ベクトル取得（ローカル座標のY軸）
	D3DXVECTOR3 GetTop() {
		D3DXMATRIX rot;
		//正規化されてる
		D3DXMatrixRotationYawPitchRoll(&rot, m_Rotation.y, m_Rotation.x, m_Rotation.z);

		D3DXVECTOR3 top;
		top.x = rot._21;
		top.y = rot._22;
		top.z = rot._23;

		return top;
	}

	//前方向ベクトル取得（ローカル座標のZ軸）
	D3DXVECTOR3 GetForward() {
		D3DXMATRIX rot;
		//正規化されてる
		D3DXMatrixRotationYawPitchRoll(&rot, m_Rotation.y, m_Rotation.x, m_Rotation.z);

		D3DXVECTOR3 forward;
		forward.x = rot._31;
		forward.y = rot._32;
		forward.z = rot._33;

		return forward;
	}

	//位置をvecの分を移動する
	void MovePosition(D3DXVECTOR3 vec) {
		m_Position += vec;
	}


	//右にamountユニット分を移動する
	void MoveRight(float amount) {
		m_Position += GetRight() * amount;
	}

	//上にamountユニット分を移動する
	void MoveTop(float amount) {
		m_Position += GetTop() * amount;
	}

	//前にamountユニット分を移動する
	void MoveForward(float amount) {
		m_Position += GetForward() * amount;
	}

	//回転をvecの分する
	void Rotate(D3DXVECTOR3 vec) {
		m_Rotation += vec;
	}

	//スケールをvec分上げる
	void AddScale(D3DXVECTOR3 vec) {
		if (m_Scale.x + vec.x < 0.01f)return;
		if (m_Scale.y + vec.y < 0.01f)return;
		if (m_Scale.z + vec.z < 0.01f)return;
		m_Scale += vec;
	}


	//コンポーネントを追加
	template <typename T> //テンプレート関数
	T* AddComponent() {
		Component* component = new T();
		component->m_GameObject = this;
		m_Component.push_back(component);
		component->Init();

		return (T*)component;
	}

	//コンポーネントを取得
	template <typename T>
	T* GetComponent() {
		for (Component* component : m_Component) {
			if (typeid(*component) == typeid(T)) { //型を調べる(RTTI動的型情報)
				return (T*)component;
			}
		}
		return nullptr;
	}

};