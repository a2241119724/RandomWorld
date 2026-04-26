# Feature Planner Prompt

Plan safe development candidates for a Unity game project.

Priorities:
- Low risk before medium risk.
- New runtime gameplay or project-system features before readonly/report-only tooling.
- Prefer standalone MonoBehaviour components that can be attached or wired manually.
- Skip high-risk candidates by default.
- Avoid anything that modifies scenes, prefabs, ScriptableObjects, save data, networking, or packaging.
