#include "main.h"
#include "manager.h"
#include "renderer.h"
#include "polygon2D.h"
#include "field.h"
#include "camera.h"
#include "model.h"
#include "player.h"
#include "input.h"


std::list<std::shared_ptr<GameObject>> Manager::m_GameObject[5];
std::stringstream Manager::m_GameObjectCache[5];
bool Manager::m_IsPlaying;


void Manager::Init()
{
	Renderer::Init();
	Input::Init();

	AddGameObject("Camera", LAYER_CAMERA)->AddComponent<Camera>();

	AddGameObject("Field", LAYER_3D_OBJECT)->AddComponent<Field>();

}

void Manager::Uninit()
{
	for (int i = 4; i >= 0; i--) {
		for (auto& gameObject : m_GameObject[i]) {
			gameObject->Uninit();
			//delete gameObject;
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

	for (int i = 0; i < 5; i++) {
		for (auto& gameObject : m_GameObject[i]) {
			//if (m_SceneChange) { return; }
			gameObject->Update();
		}

		m_GameObject[i].remove_if([](std::shared_ptr<GameObject> object) {return object->Destroy(); }); //ƒ‰ƒ€ƒ_Ž®
	}
}

void Manager::Draw(void * Resource, bool NewSurface)
{
	Renderer::Begin(Resource, NewSurface);
	
	for (int i = 0; i < 5; i++) {
		for (auto& gameObject : m_GameObject[i]) {
			gameObject->Draw();
		}
	}

	Renderer::End();
}

std::shared_ptr<GameObject> Manager::AddGameObject(const char * ObjectName, int layer)
{
	std::shared_ptr<GameObject> gameObject(new GameObject(ObjectName));
	m_GameObject[layer].push_back(gameObject);
	return gameObject;
}


void Manager::AddModel(const char* ObjectName, const char* FileName)
{
	std::shared_ptr<GameObject> gameObject = AddGameObject(ObjectName, 1);
	gameObject->AddComponent<Model>()->Load(FileName);
}

void Manager::RenameGameObject(const char* ObjectName, const char* newName) 
{
	GetGameObject(ObjectName)->SetName(newName);
}

char* Manager::RaycastObject(float x, float y, float screenHeight, float screenWidth) {
	std::shared_ptr<GameObject> camObj = GetGameObject("Camera");
	Camera* camera = camObj->GetComponent<Camera>();
	D3DXVECTOR3 ray = camera->GetRayFromScreen(x, y, screenHeight, screenWidth);
	std::vector<std::shared_ptr<GameObject>> players = GetGameObjects<Model>();
	std::string temp = "";
	float raycastLimit = 100.0f;
	for (auto player : players) {
		float dist = player->GetComponent<Model>()->IsRayCollide(ray, camObj->GetPosition());
		//if (player->GetComponent<Model>()->IsRayCollide(ray, camObj->GetPosition())) {
		//	temp = player->GetName();
		//}
		if (dist > 0) {
			if (dist < raycastLimit) {
				raycastLimit = dist;
				temp = player->GetName();
			}
		}
	}
	int len = strlen(temp.c_str());
	char* data = new char[len + 1];
	memcpy(data, temp.c_str(), len + 1);
	return data;
}

void Manager::SetPlaying(bool playing)
{
	m_IsPlaying = playing;
	if (playing) {
		for (int i = 0; i < 5; i++) {
			m_GameObjectCache[i].str("");
			m_GameObjectCache[i].clear();
			cereal::JSONOutputArchive o_archive(m_GameObjectCache[i]);
			for (auto& gameObject : m_GameObject[i]) {
				o_archive(*gameObject);
			}
		}
	}
	else {
		for (int i = 0; i < 5; i++) {
			if (m_GameObjectCache[i].str() == "")continue;
			cereal::JSONInputArchive i_archive(m_GameObjectCache[i]);
			for (auto& gameObject : m_GameObject[i]) {
				i_archive(*gameObject);
			}
		}
	}
}
