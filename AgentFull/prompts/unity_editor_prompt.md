# Unity Editor 工具提示词

生成只读的 Unity Editor 工具。

规则：
- 适合时使用 EditorWindow 或 MenuItem。
- 生成的 Editor 代码写入配置的 Unity Editor 目录。
- 工具可以扫描项目并导出报告，但不能修改资源。
- 避免修改场景、Prefab、ScriptableObject、StreamingAssets 和 Addressables。
- 所有 C# 注释必须使用中文，包括 XML summary、普通注释和说明性注释。
- 需要结合上下文包中的项目结构、关键脚本片段、会话历史、用户输入和最近模型调用来生成工具。
