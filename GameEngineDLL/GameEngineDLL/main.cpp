

#include "main.h"
#include "manager.h"
#include "camera.h"


const char* CLASS_NAME = "AppClass";
const char* WINDOW_NAME = "DX11ƒQ[ƒ€";


void Init()
{

	Manager::Init();

}

void Cleanup()
{

	Manager::Uninit();

}


void Render(void * Resource, bool NewSurface)
{

	Manager::Update();
	Manager::Draw(Resource, NewSurface);

}



void SetObjectPosition(const char* ObjectName, D3DXVECTOR3 Position)
{
	Manager::GetGameObject(ObjectName)->SetPosition(Position);
}

void SetObjectRotation(const char* ObjectName, D3DXVECTOR3 Rotation)
{
	Manager::GetGameObject(ObjectName)->SetRotation(Rotation);
}

void SetObjectScale(const char* ObjectName, D3DXVECTOR3 Scale)
{
	Manager::GetGameObject(ObjectName)->SetScale(Scale);
}



D3DXVECTOR3 GetObjectPosition(const char* ObjectName)
{
	return Manager::GetGameObject(ObjectName)->GetPosition();
}

D3DXVECTOR3 GetObjectRotation(const char* ObjectName)
{
	return Manager::GetGameObject(ObjectName)->GetRotation();
}

D3DXVECTOR3 GetObjectScale(const char* ObjectName)
{
	return Manager::GetGameObject(ObjectName)->GetScale();
}

D3DXVECTOR3 GetObjectRight(const char* ObjectName) 
{
	return Manager::GetGameObject(ObjectName)->GetRight();
}

D3DXVECTOR3 GetObjectTop(const char* ObjectName)
{
	return Manager::GetGameObject(ObjectName)->GetTop();
}

D3DXVECTOR3 GetObjectForward(const char* ObjectName)
{
	return Manager::GetGameObject(ObjectName)->GetForward();
}

void MoveObjectPosition(const char* ObjectName, D3DXVECTOR3 vec) {
	return Manager::GetGameObject(ObjectName)->MovePosition(vec);
}

void MoveObjectRight(const char* ObjectName, float amount) {
	return Manager::GetGameObject(ObjectName)->MoveRight(amount);
}

void MoveObjectTop(const char* ObjectName, float amount) {
	return Manager::GetGameObject(ObjectName)->MoveTop(amount);
}

void MoveObjectForward(const char* ObjectName, float amount) {
	return Manager::GetGameObject(ObjectName)->MoveForward(amount);
}

void SetObjectName(const char* ObjectName, const char* newObjectName) {
	return Manager::RenameGameObject(ObjectName, newObjectName);
}

void CallMoveCamera() {
	GameObject* camObj = Manager::GetGameObject("Camera");
	Camera* camera = (Camera*)camObj;
	camera->MoveCamera();
}

void ResetMoveCamera() {
	GameObject* camObj = Manager::GetGameObject("Camera");
	Camera* camera = (Camera*)camObj;
	camera->ResetSpeed();
}

char* RaycastObject(float x, float y, float screenHeight) {
	//return Manager::RaycastObject(x, y, screenHeight);
	char a = 'c';
	return &a;
}

void AddObject(const char* ObjectName, const char* FileName)
{
	Manager::AddGameObject(ObjectName, FileName);
}
