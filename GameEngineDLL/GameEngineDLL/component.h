#pragma once
class GameObject;

class Component {
protected:

public:
	GameObject* m_GameObject{};

	virtual void Init() {}
	virtual void Uninit() {}
	virtual void BeginPlay() {}
	virtual void Update(DWORD time) {}
	virtual void Draw() {}
};