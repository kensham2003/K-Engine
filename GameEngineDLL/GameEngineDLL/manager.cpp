#include "main.h"
#include "manager.h"
#include "renderer.h"
#include "polygon2D.h"
#include "field.h"
#include "camera.h"
#include "model.h"
#include "player.h"
#include "input.h"


std::unordered_map<std::string, GameObject*> Manager::m_GameObject;
bool Manager::m_IsPlaying;

const char* g_RaycastName = "b";



void Manager::Init()
{
	Renderer::Init();
	Input::Init();

	m_GameObject["Camera"] = new Camera();
	m_GameObject["Camera"]->Init();

	m_GameObject["Field"] = new Field();
	m_GameObject["Field"]->Init();


}

void Manager::Uninit()
{
	for (auto object : m_GameObject)
	{
		object.second->Uninit();
		delete object.second;
	}

	Input::Uninit();
	Renderer::Uninit();
}

void Manager::Update()
{
	if (!m_IsPlaying) { return; }

	Input::Update();

	for (auto object : m_GameObject)
	{
		object.second->Update();
	}
}

void Manager::Draw(void * Resource, bool NewSurface)
{
	Renderer::Begin(Resource, NewSurface);
	
	for (auto object : m_GameObject)
	{
		object.second->Draw();
	}

	Renderer::End();
}


void Manager::AddGameObject(const char* ObjectName, const char* FileName)
{
	Player* player = new Player();
	player->Init();
	player->Load(FileName);

	m_GameObject[ObjectName] = player;
}

void Manager::RenameGameObject(const char* ObjectName, const char* newName) 
{
	GameObject* gameObject = m_GameObject[ObjectName];
	auto it = m_GameObject.find(ObjectName);
	if (it != m_GameObject.end()) {
		m_GameObject.erase(it);
		m_GameObject[newName] = gameObject;
	}
}

char* Manager::RaycastObject(float x, float y, float screenHeight) {
	Camera* camera = (Camera*)m_GameObject["Camera"];
	D3DXVECTOR3 ray = camera->GetRayFromScreen(x, y, screenHeight);
	std::vector<Player*> players = GetGameObjects<Player>();
	std::string temp = "a";
	for (auto player : players) {
		temp += temp;
		//return "abc";
		//if (player->IsRayCollideModel(ray, camera->GetPosition())) {
		//	for (auto& it : m_GameObject) {
		//		if (it.second == player) {
		//			return it.first.c_str();
		//		}
		//	}
		//}
	}
	//g_RaycastName = "a";
	int len = strlen(temp.c_str());
	char* data = new char[len + 1];
	memcpy(data, temp.c_str(), len + 1);
	return data;
}