

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

void SetObjectTransform(const char* ObjectName, D3DXVECTOR3 Position, D3DXVECTOR3 Rotation, D3DXVECTOR3 Scale) {
	std::shared_ptr<GameObject> gameObject = Manager::GetGameObject(ObjectName);
	gameObject->SetPosition(Position);
	gameObject->SetRotation(Rotation);
	gameObject->SetScale(Scale);
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

void RotateObject(const char* ObjectName, D3DXVECTOR3 vec) {
	return Manager::GetGameObject(ObjectName)->Rotate(vec);
}

void ScaleObject(const char* ObjectName, D3DXVECTOR3 vec) {
	return Manager::GetGameObject(ObjectName)->AddScale(vec);
}

void SetObjectName(const char* ObjectName, const char* newObjectName) {
	return Manager::RenameGameObject(ObjectName, newObjectName);
}

void CallMoveCamera() {
	//GameObject* camObj = Manager::GetGameObject<GameObject>("Camera");
	//Camera* camera = (Camera*)camObj;
	Manager::GetGameObject("Camera")->GetComponent<Camera>()->MoveCamera();
	//camera->MoveCamera();
}

void ResetMoveCamera() {
	//GameObject* camObj = Manager::GetGameObject("Camera");
	//Camera* camera = (Camera*)camObj;
	//camera->ResetSpeed();
	Manager::GetGameObject("Camera")->GetComponent<Camera>()->ResetSpeed();
}

char* RaycastObject(float x, float y, float screenHeight, float screenWidth) {
	return Manager::RaycastObject(x, y, screenHeight, screenWidth);
}

D3DXVECTOR3 GetRayFromScreen(float x, float y, float screenHeight, float screenWidth) {
	return Manager::GetGameObject("Camera")->GetComponent<Camera>()->GetRayFromScreen(x, y, screenHeight, screenWidth);
}

void FreeRaycastChar(char* p) {
	return Manager::FreeRaycastChar(p);
}

void AddModel(const char* ObjectName, const char* FileName)
{
	Manager::AddModel(ObjectName, FileName);
}

void AddBoxCollider(const char* ObjectName, const char* FileName)
{
	Manager::AddBoxCollider(ObjectName, FileName);
}

void SetObjectBoxColliderSize(const char* ObjectName, D3DXVECTOR3 Size) {
	Manager::SetBoxColliderSize(ObjectName, Size);
}

void SetObjectBoxColliderRotate(const char* ObjectName, D3DXVECTOR3 Rotate) {
	Manager::SetBoxColliderRotate(ObjectName, Rotate);
}

void SetObjectBoxColliderOffset(const char* ObjectName, D3DXVECTOR3 Offset) {
	Manager::SetBoxColliderOffset(ObjectName, Offset);
}

void RemoveBoxCollider(const char* ObjectName)
{
	Manager::RemoveBoxCollider(ObjectName);
}

bool GetMaterialTextureEnable(const char* ObjectName)
{
	return Manager::GetMaterialTextureEnable(ObjectName);
}

int GetModelSubsetNum(const char* ObjectName)
{
	return Manager::GetMaterialTextureEnable(ObjectName);
}

void SetScenePlaying(bool playing) {
	Manager::SetPlaying(playing);
}