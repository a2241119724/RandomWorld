<!-- CODEGRAPH_START -->
## CodeGraph

在已由 CodeGraph 索引的仓库中（仓库根目录存在 `.codegraph/` 目录时），当你需要理解或定位代码时，优先使用它而非 grep/find 或读取文件：

- **MCP 工具**（可用时）：`codegraph_explore` 一次调用即可回答大多数代码问题——返回相关符号的原始源码及它们之间的调用路径，包括 grep 无法追踪的动态分发跳转。在查询中命名一个文件或符号即可读取其当前行号源码。如果列出但被延迟加载，通过工具搜索按名称加载。
- **Shell**（始终可用）：`codegraph explore "<符号名称或问题>"` 输出相同的内容。

如果没有 `.codegraph/` 目录，则完全跳过 CodeGraph——索引由用户决定。
<!-- CODEGRAPH_END -->
