#pragma once

#include <string>
#include <unordered_map>

#include "gameObject.h"

#define LAYER_CAMERA (0)
#define LAYER_3D_OBJECT (1)
#define LAYER_EFFECT (2)
#define LAYER_2D_OBJECT (3)
#define LAYER_FADE (4)

class Manager
{
private:
	static std::unordered_map<std::string, GameObject*> m_GameObject;

	static bool m_IsPlaying;


public:
	static void Init();
	static void Uninit();
	static void Update();
	static void Draw(void * Resource, bool NewSurface);


	static void AddGameObject(const char* ObjectName, const char* FileName);

	static void RenameGameObject(const char* ObjectName, const char* newName);

	static GameObject* GetGameObject(const char* Name)
	{
		return m_GameObject[Name];
	}

	//�ǉ����ꂽ�����^�̃Q�[���I�u�W�F�N�g��S���擾
	template <typename T>
	static std::vector<T*> GetGameObjects() {
		std::vector<T*> objects; //STL�̔z��
		for (auto& it : m_GameObject) {
			GameObject* object = it.second;
			if (typeid(*object) == typeid(T)) { //�^�𒲂ׂ�(RTTI���I�^���)
				objects.push_back((T*)object);
			}
		}
		return objects;
	}

	static char* RaycastObject(float x, float y, float screenHeight, float screenWidth);

	static void FreeRaycastChar(char* p) { delete[] p; }

	static void SetPlaying(bool playing) { m_IsPlaying = playing; }
};