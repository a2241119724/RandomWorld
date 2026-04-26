# Unity Editor Tool Prompt

Generate readonly Unity Editor tools.

Rules:
- Use EditorWindow or MenuItem when appropriate.
- Put generated code in the report folder first.
- The tool may scan and export reports, but must not modify assets.
- Avoid scene, prefab, ScriptableObject, StreamingAssets, and Addressables changes.
