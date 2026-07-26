# 仓库规范

## 项目结构与模块组织
本仓库是 RandomWorld 的 Unity `Assets` 目录。玩法代码位于 `Scripts/2D`，功能区域包括 `Character`、`Domain`、`Tool`、`UI`、`Enum`、`MVC`。尽量将纯规则和计算放在 `Scripts/2D/Domain/**` 中，将 Unity 对象、预制体、Photon 和场景访问放在适配器/管理器层中。第三方或导入的代码放在 `Scripts/Reference` 下；除非修改是专门针对特定供应商的，否则避免修改。

Unity 内容按 `Scenes`、`Resources`、`ResourcesLocal`、`Materials`、`Animation`、`URP`、`TextMesh Pro`、`StreamingAssets` 和 `AddressableAssetsData` 组织。每次资源变更都要保留并提交对应的 `.meta` 文件。

## 构建、测试与开发命令
使用 Unity Editor 打开包含本 `Assets` 文件夹的项目根目录。通过 Unity Build Settings 构建目标平台；已有的可运行输出及相关文件保存在 `Build` 下。

常用仓库命令：

```powershell
git status
git config core.hooksPath .githooks
powershell -NoProfile -ExecutionPolicy Bypass -File .\.gitarchive\Set-ArchivePassword.ps1
```

在预制体或 Addressables 变更后，需重新构建相关的 AssetBundle/Addressables 再验证游戏功能。

## 代码风格与命名规范
C# 代码使用 `LAB2D` 命名空间、四个空格缩进、花括号独占一行。类、方法、属性、常量和枚举值使用 PascalCase。私有字段通常使用 camelCase；在修改文件中优先使用 `this.` 访问实例成员（与本地风格一致时）。`MonoBehaviour` 类专注于 Unity 生命周期和场景连接；将确定性逻辑放入小的服务/工具类以便于审查。

## 测试规范
当前 `Assets` 中没有专门的测试文件夹。在 Unity Editor 中使用 Play Mode 和场景专项检查验证更改。对于 `Domain` 或 `Tool` 中的纯逻辑，引入新行为时添加针对性的 Unity Test Runner 覆盖，使用描述性名称如 `DamageCalculatorTests` 和 `ApplyDefense_ClampsToMinimumDamage`。

## 提交与 Pull Request 规范
近期历史使用简短的祈使式摘要，常用约定式前缀如 `refactor(WaveBoss): ...` 或 `refactor ...`。为清晰起见，优先使用 `feat(scope): 摘要`、`fix(scope): 摘要` 或 `refactor(scope): 摘要`。

Pull Request 应描述玩法影响，列出执行的验证，提及受影响的场景/预制体/资源，并包含 UI 或视觉更改的截图或短视频。适用时关联 Issue，并说明所需的 AssetBundle 或 Addressables 重新构建。

## 配置与资源安全
不要提交仅限本地的密钥或机器特定设置。始终保持 `.meta` 文件与资源在一起，避免在 Unity 外部重命名资源，提交前仔细审查预制体/场景的差异。
