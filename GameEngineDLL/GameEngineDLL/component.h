#pragma once
class GameObject;

class Component {
protected:

public:
	GameObject* m_GameObject{};

	virtual void Init() {}
	virtual void Uninit() {}
	virtual void Update() {}
	virtual void Draw() {}
};