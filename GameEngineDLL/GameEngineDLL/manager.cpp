#include "main.h"
#include "manager.h"
#include "renderer.h"
#include "polygon2D.h"
#include "field.h"
#include "camera.h"
#include "model.h"
#include "player.h"
#include "input.h"


std::unordered_map<std::string, GameObject*> Manager::m_GameObjectMap;
std::list<GameObject*> Manager::m_GameObject[5];
bool Manager::m_IsPlaying;

const char* g_RaycastName = "b";

//GameObject* g_Test;

int g_Cnt = 1;


void Manager::Init()
{
	Renderer::Init();
	Input::Init();

	//m_GameObjectMap["Camera"] = new Camera();
	//m_GameObjectMap["Camera"]->Init();
	AddGameObject("Camera", LAYER_CAMERA)->AddComponent<Camera>();

	AddGameObject("Field", LAYER_3D_OBJECT)->AddComponent<Field>();
	//AddModel("test", "D:\\GitProjects\\GameEngineNew\\asset\\model\\Barrel_01_1k.obj");

	//m_GameObjectMap["Field"] = new Field();
	//m_GameObjectMap["Field"]->Init();

}

void Manager::Uninit()
{
	//for (auto object : m_GameObjectMap)
	//{
	//	object.second->Uninit();
	//	delete object.second;
	//}
	for (int i = 4; i >= 0; i--) {
		for (GameObject* gameObject : m_GameObject[i]) {
			gameObject->Uninit();
			delete gameObject;
		}
		m_GameObject[i].clear();
	}

	Input::Uninit();
	Renderer::Uninit();
}

void Manager::Update()
{
	Input::Update();

	if (!m_IsPlaying) { return; }

	//for (auto object : m_GameObjectMap)
	//{
	//	object.second->Update();
	//}
	for (int i = 0; i < 5; i++) {
		for (GameObject* gameObject : m_GameObject[i]) {
			//if (m_SceneChange) { return; }
			gameObject->Update();
		}

		m_GameObject[i].remove_if([](GameObject* object) {return object->Destroy(); }); //ƒ‰ƒ€ƒ_Ž®
	}
}

void Manager::Draw(void * Resource, bool NewSurface)
{
	Renderer::Begin(Resource, NewSurface);
	
	//for (auto object : m_GameObjectMap)
	//{
	//	object.second->Draw();
	//}
	for (int i = 0; i < 5; i++) {
		for (GameObject* gameObject : m_GameObject[i]) {
			gameObject->Draw();
		}
	}

	Renderer::End();
}

GameObject* Manager::AddGameObject(const char * ObjectName, int layer)
{
	GameObject* gameObject = new GameObject(ObjectName);
	m_GameObject[layer].push_back(gameObject);
	return gameObject;
}


void Manager::AddModel(const char* ObjectName, const char* FileName)
{
	//Player* player = new Player();
	//player->Init();
	//player->Load(FileName);
	//Player* player = new Player(ObjectName);
	//player->Init();
	//player->AddComponent<Model>()->Load(FileName);
	//m_GameObject[LAYER_3D_OBJECT].push_back(player);
	//m_GameObjectMap[ObjectName] = player;
	GameObject* gameObject = AddGameObject(ObjectName, 1);
	gameObject->AddComponent<Model>()->Load(FileName);
}

void Manager::RenameGameObject(const char* ObjectName, const char* newName) 
{
	//GameObject* gameObject = m_GameObjectMap[ObjectName];
	//auto it = m_GameObjectMap.find(ObjectName);
	//if (it != m_GameObjectMap.end()) {
	//	m_GameObjectMap.erase(it);
	//	m_GameObjectMap[newName] = gameObject;
	//}
	GetGameObject(ObjectName)->SetName(newName);
}

char* Manager::RaycastObject(float x, float y, float screenHeight, float screenWidth) {
	//Camera* camera = (Camera*)m_GameObjectMap["Camera"];
	GameObject* camObj = GetGameObject("Camera");
	Camera* camera = camObj->GetComponent<Camera>();
	D3DXVECTOR3 ray = camera->GetRayFromScreen(x, y, screenHeight, screenWidth);
	D3DXVECTOR3 testRay = ray * 5;
	//g_Test->SetPosition(camera->GetPosition() + testRay);
	//g_Cnt++;
	//g_Test->SetPosition(ray);
	std::vector<GameObject*> players = GetGameObjects<Model>();
	std::string temp = "";
	for (auto player : players) {
		if (player->GetComponent<Model>()->IsRayCollide(ray, camObj->GetPosition())) {
			//for (auto& it : m_GameObjectMap) {
			//	if (it.second == player) {
			//		temp = it.first;
			//	}
			//}
			temp = player->GetName();
		}
	}
	int len = strlen(temp.c_str());
	char* data = new char[len + 1];
	memcpy(data, temp.c_str(), len + 1);
	return data;
}