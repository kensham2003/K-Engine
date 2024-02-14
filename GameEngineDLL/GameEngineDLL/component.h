#pragma once
class GameObject;

class Component {
protected:
	bool m_Active = true;

public:
	GameObject* m_GameObject{};

	virtual void Init() {}
	virtual void Uninit() {}
	virtual void Update() {}
	virtual void Draw() {}

	bool IsActive() { return m_Active; }
	void SetActive(bool active) { m_Active = active; }
};