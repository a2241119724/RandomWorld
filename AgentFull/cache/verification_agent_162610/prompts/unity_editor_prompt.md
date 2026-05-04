# Unity Editor Tool Prompt

Generate readonly Unity Editor tools.

Rules:
- Use EditorWindow or MenuItem when appropriate.
- Put generated Editor code in the configured Unity Editor folder.
- The tool may scan and export reports, but must not modify assets.
- Avoid scene, prefab, ScriptableObject, StreamingAssets, and Addressables changes.
