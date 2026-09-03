# 自动驾驶开发循环（Autopilot Prompt）

> **使用方法（给人看的，贴 prompt 时从下方分隔线开始复制）**
>
> 1. **推荐**：新开 Claude Code 会话（任意目录指到本项目），以宽权限模式运行（`/permissions` 放行 Bash/Edit，或启动时 `claude --dangerously-skip-permissions`），然后粘贴分隔线以下全文。会话将持续运行直到停止条件触发。
> 2. **定时循环**：`/loop 45m 按 docs/autopilot-prompt.md 执行一轮自动驾驶循环（摸底→发现→实现→提交→记录）`——每轮做一个最小切片，间隔要大于单轮耗时。
> 3. **断点恢复**：会话中断/压缩/崩溃后，重新粘贴全文即可——第 1 步摸底会从 progress.md + git log 自动接上，无需人工交接。
> 4. 建议先开着 Unity 再启动（新脚本会即时进 csproj）；或接受新文件由编译脚本手动收录。

---

你现在进入**无人值守自动驾驶模式**：持续执行「摸底 → 发现 → 实现 → 验证 → 提交 → 记录 → 继续」的开发循环，直到满足停止条件。用户已离场，不要等待确认、不要请求批准——本 prompt 即为授权书。

## 授权与边界（仅本次运行有效，覆盖默认的提交前询问）

- ✅ **已授权**：每完成一个功能自动 `git commit`（conventional commits + 中文描述，风格与 `git log` 既有提交一致），无需询问。
- ❌ **仍然禁止**：`git push`、开 PR、切分支/合并、删除文件、破坏性重构、改网络/打包/PlayerSettings、批量产出美术素材（素材走既有生成管线，不归本循环管）。
- 跨 3 文件以上的调查照旧派子代理，但**不走计划-批准流程**——自己定方案直接实现。
- **一轮一个 commit**：做不完就把本次改动 `git checkout -- .` 回滚干净再换任务，绝不留半成品在工作区。

## 循环步骤

### 1. 摸底（每轮都做，这是上下文压缩后的自愈机制）

1. 读 `docs/ai-context/progress.md` 顶部 Recent Changes（最近 5 条）与 `spec.md` 相关节、`bug-fixes.md` 教训清单。
2. `git log --oneline -10` + `git status` 确认进度与干净状态。
3. 工作区不干净 → 上一个任务没做完：先评估能否 5 分钟内补完，否则回滚，再继续。

### 2. 发现候选（按优先级依次找，取第一个值得做的）

1. **日志 bug（最高优先）**：读 `game.log` / `error.log`（`%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\`，准确路径见 `Scripts/2D/Manager/LogManager.cs:15`），按 `log-bug-fixer` 子代理的套路统计刷屏模式。同一 bug 若 `bug-fixes.md` 已有记录，直接引用历史思路验证，不重复排查。
2. **既定计划**：progress.md / spec.md 中明确标注的「下一步 / 未完成 / M2A」等方向（如当前分支的防守夜 Worker 响应后续）。
3. **代码缺口**：TODO/HACK 注释、系统间明显缺失的衔接环节。
4. **玩法增强**：自拟一个低风险小功能（参考 spec.md 的系统清单找空缺）。

### 3. 风险过滤（命中任一 → 丢弃该候选，看下一个）

- 需要改场景/Prefab/ScriptableObject 资产、或运行时肉眼才能验证效果的（纯视觉/手感类）——本循环只能用「编译 + 单测」验证。
- 触及网络同步/打包配置/存档格式迁移的。
- 范围大到一轮做不完的 → 拆出最小垂直切片，只做切片本身。
- 与 `bug-fixes.md` 中已知陷阱同型的（历史教训优先复用结论，不重蹈覆辙）。

### 4. 实现

- 先 `codegraph_explore` / 读代码摸清现状与调用方，再动手。
- 遵守 CLAUDE.md 全部约定：新增建筑一律 ABuildItem 三同、日志通道 `AWorkerTask.LogProvider` 只加事件点、Worker 三层架构、素材正俯视等。
- **新增 .cs 文件时**：若使用秒级编译脚本（见第 5 步），按其要求把新文件追加进 rsp 列表（MAIN_NEW/EDITOR_NEW），否则脚本看不到新文件。

### 5. 编译验证

1. 优先用既有秒级编译脚本（`build_bee.py`，复用 Library/Bee 的 rsp + csc；用法见 memory `unity-compile-verification`）。
2. 找不到该脚本时：`dotnet build D:/LAB/Unity/RandomWorld/Assembly-CSharp.csproj`（新增文件后若 csproj 未含，改用 rsp 追加方式）。
3. 涉及 Domain 纯函数层的改动，跑对应单测（既有 `Scripts/2D/Editor/Tests/` 模式）。
4. **失败处理**：修复重试；连续 3 次失败 → 回滚全部改动、把根因教训追加到 `bug-fixes.md`、换下一个候选。

### 6. 提交 + 记录（必须落盘，这是断点续传的命脉）

1. `git add -A && git commit -m "type(scope): 中文描述"`。
2. `progress.md` Recent Changes 顶部追加一行，格式照旧：`- 2026-09 — type(scope): 描述`。
3. 日志驱动的 bug 修复同时追加 `bug-fixes.md`（现象/根因/修复/验证/教训）。
4. 成型的系统变更同步 `spec.md` 对应节（现在时，精简）。

### 7. 继续

回到第 1 步。每轮只输出一行进度（做了什么 / commit hash / 下一轮打算做什么），不写长篇汇报。

## 停止条件（满足任一即停，输出简短汇总）

- **连续 2 轮**找不到值得做的候选 → 停，列出剩余候选及为何不值得。
- 编译基础设施本身坏了（csproj 加载失败、Bee 缓存损坏）→ 停，报告，不要试图修 Unity 安装。
- 用户发来任何消息 → 完成当前原子操作（最迟做完当前这轮的提交）后立即停下回应。

## 设计说明（为什么这样写，执行时可忽略）

- **每轮摸底**是关键：上下文被压缩或会话重启后，靠 progress.md + git log 恢复状态，循环可以无限续命。
- **一轮一 commit + 做不完就回滚**：保证任意时刻工作区可中断、可交接，绝不污染。
- **风险过滤排除视觉类**：无人值守下无法开 Unity 看效果，只做编译/单测可闭环的任务，质量才有保底。
