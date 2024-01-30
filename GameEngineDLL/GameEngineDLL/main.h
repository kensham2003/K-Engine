#pragma once

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <windows.h>
#include <assert.h>



#pragma warning(push)
#pragma warning(disable:4005)

#include <d3d11.h>
#include <d3dx9.h>
#include <d3dx11.h>

#pragma warning(pop)

#include <string>

#pragma comment (lib, "winmm.lib")
#pragma comment (lib, "d3d11.lib")
#pragma comment (lib, "d3dx9.lib")
#pragma comment (lib, "d3dx11.lib")


#define SCREEN_WIDTH	(960)
#define SCREEN_HEIGHT	(540)

#define GET_VARIABLE_NAME(Variable) (#Variable)


HWND GetWindow();




extern "C" {
	__declspec(dllexport) void __cdecl Init();
	__declspec(dllexport) void __cdecl Cleanup();
	__declspec(dllexport) void __cdecl Render(void * Resource, bool NewSurface);

	__declspec(dllexport) void __cdecl SetObjectPosition(const char* ObjectName, D3DXVECTOR3 Position);
	__declspec(dllexport) void __cdecl SetObjectRotation(const char* ObjectName, D3DXVECTOR3 Rotation);
	__declspec(dllexport) void __cdecl SetObjectScale(const char* ObjectName, D3DXVECTOR3 Scale);

	__declspec(dllexport) void __cdecl SetObjectTransform(const char* ObjectName, D3DXVECTOR3 Position, D3DXVECTOR3 Rotation, D3DXVECTOR3 Scale);

	__declspec(dllexport) D3DXVECTOR3 __cdecl GetObjectPosition(const char* ObjectName);
	__declspec(dllexport) D3DXVECTOR3 __cdecl GetObjectRotation(const char* ObjectName);
	__declspec(dllexport) D3DXVECTOR3 __cdecl GetObjectScale(const char* ObjectName);

	__declspec(dllexport) D3DXVECTOR3 __cdecl GetObjectRight(const char* ObjectName);
	__declspec(dllexport) D3DXVECTOR3 __cdecl GetObjectTop(const char* ObjectName);
	__declspec(dllexport) D3DXVECTOR3 __cdecl GetObjectForward(const char* ObjectName);

	__declspec(dllexport) void __cdecl MoveObjectPosition(const char* ObjectName, D3DXVECTOR3 vec);
	__declspec(dllexport) void __cdecl MoveObjectRight(const char* ObjectName, float amount);
	__declspec(dllexport) void __cdecl MoveObjectTop(const char* ObjectName, float amount);
	__declspec(dllexport) void __cdecl MoveObjectForward(const char* ObjectName, float amount);

	__declspec(dllexport) void __cdecl RotateObject(const char* ObjectName, D3DXVECTOR3 vec);

	__declspec(dllexport) void __cdecl ScaleObject(const char* ObjectName, D3DXVECTOR3 vec);

	__declspec(dllexport) void __cdecl SetObjectName(const char* ObjectName, const char* newObjectName);

	__declspec(dllexport) void __cdecl CallMoveCamera();
	__declspec(dllexport) void __cdecl ResetMoveCamera();

	__declspec(dllexport) char* __cdecl RaycastObject(float x, float y, float screenHeight, float screenWidth);
	__declspec(dllexport) D3DXVECTOR3 __cdecl GetRayFromScreen(float x, float y, float screenHeight, float screenWidth);
	__declspec(dllexport) void __cdecl FreeRaycastChar(char* p);

	__declspec(dllexport) void __cdecl AddModel(const char* ObjectName, const char* FileName);
	__declspec(dllexport) void __cdecl AddBoxCollider(const char* ObjectName, const char* FileName);

	__declspec(dllexport) void __cdecl SetObjectBoxColliderSize(const char* ObjectName, D3DXVECTOR3 Size);
	__declspec(dllexport) void __cdecl SetObjectBoxColliderRotate(const char* ObjectName, D3DXVECTOR3 Rotate);
	__declspec(dllexport) void __cdecl SetObjectBoxColliderOffset(const char* ObjectName, D3DXVECTOR3 Offset);

	__declspec(dllexport) bool __cdecl GetMaterialTextureEnable(const char* ObjectName);
	__declspec(dllexport) int __cdecl GetModelSubsetNum(const char* ObjectName);

	__declspec(dllexport) void __cdecl SetScenePlaying(bool playing);
}
