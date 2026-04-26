# Unity Editor 工具提示词

生成只读的 Unity Editor 工具。

规则：
- 适合时使用 EditorWindow 或 MenuItem。
- 生成的 Editor 代码写入配置的 Unity Editor 目录。
- 工具可以扫描项目并导出报告，但不能修改资源。
- 避免修改场景、Prefab、ScriptableObject、StreamingAssets 和 Addressables。
