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
	static std::unordered_map<std::string, GameObject*> m_GameObjectMap;

	static std::list<GameObject*> m_GameObject[5];

	static bool m_IsPlaying;


public:
	static void Init();
	static void Uninit();
	static void Update();
	static void Draw(void * Resource, bool NewSurface);

	static GameObject* AddGameObject(const char* ObjectName, int layer); //Add empty object

	//template <typename T> //テンプレート関数
	//static T* AddGameObject(int Layer) {
	//	T* gameObject = new T();
	//	m_GameObject[Layer].push_back(gameObject);
	//	gameObject->Init();

	//	return gameObject;
	//}

	static void AddModel(const char* ObjectName, const char* FileName);

	static void RenameGameObject(const char* ObjectName, const char* newName);

	//static GameObject* GetGameObject(const char* Name)
	//{
	//	return m_GameObjectMap[Name];
	//}

	//名前が合っているゲームオブジェクトを取得
	static GameObject* GetGameObject(std::string name) {
		for (int i = 0; i < 5; i++) {
			for (GameObject* object : m_GameObject[i]) {
					if (object->GetName() == name) {
						return object;
					}
			}
		}
		return nullptr;
	}

	////追加された同じ型のゲームオブジェクトを全部取得
	//template <typename T>
	//static std::vector<T*> GetGameObjects() {
	//	std::vector<T*> objects; //STLの配列
	//	for (auto& it : m_GameObjectMap) {
	//		GameObject* object = it.second;
	//		if (typeid(*object) == typeid(T)) { //型を調べる(RTTI動的型情報)
	//			objects.push_back((T*)object);
	//		}
	//	}
	//	return objects;
	//}

	//指定したコンポーネントを持っているのオブジェクトを取得
	template <typename T>
	static std::vector<GameObject*> GetGameObjects() {
		std::vector<GameObject*> objects;
		for (int i = 0; i < 5; i++) {
			for (GameObject* object : m_GameObject[i]) {
				if (object->GetComponent<T>() != nullptr) {
					objects.push_back(object);
				}
			}
		}
		return objects;
	}

	static char* RaycastObject(float x, float y, float screenHeight, float screenWidth);

	static void FreeRaycastChar(char* p) { delete[] p; }

	static void SetPlaying(bool playing) { m_IsPlaying = playing; }
};