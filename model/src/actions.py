"""Worker 行为（决策输出）常量定义。

与 C# 侧 ``WorkerBrain.WorkerDecisionType``（Scripts/2D/AI/Worker/WorkerBrain.cs:20-63）
保持一一对应，顺序固定 —— 模型输出的第 i 维即 ``ACTIONS[i]`` 的概率/分数。
"""

# 顺序必须与 C# WorkerDecisionType 枚举保持一致
ACTIONS = [
    "idle",
    "self_gather",
    "post_bounty",
    "accept_bounty",
    "eat",
    "sleep",
    "self_carry",
    "pickup",
    "self_build",
    "self_plant",
    "wander",
    "ground_sleep",
    "store",
    "withdraw",
]

NUM_ACTIONS = len(ACTIONS)

ACTION_INDEX = {name: i for i, name in enumerate(ACTIONS)}

# 中文标签（用于日志/评估可读性）
ACTION_LABELS_ZH = {
    "idle": "空闲/锻炼",
    "self_gather": "自主采集",
    "post_bounty": "发布悬赏",
    "accept_bounty": "接受悬赏",
    "eat": "吃饭",
    "sleep": "睡觉",
    "self_carry": "搬运自己悬赏物品",
    "pickup": "拾取",
    "self_build": "自主建造",
    "self_plant": "种植",
    "wander": "漫游",
    "ground_sleep": "地面睡眠",
    "store": "存入仓库",
    "withdraw": "取出仓库",
}
