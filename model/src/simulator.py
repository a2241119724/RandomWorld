"""决策教师模拟器：生成 (状态 → 行为) 标注数据。

融合两层规则作为「教师」：
1. **WorkerBrain 规则基底** —— 复刻 C# 侧 `WorkerBrain.Decide()` 的优先级级联
   （Scripts/2D/AI/Worker/WorkerBrain.cs:259）与人格加权概率门控。
2. **现实人际关系规则** —— 补充硬编码规则覆盖不到的细粒度权衡，例如：
   天气炎热/严寒 + 事业心低 → 不愿出门干活；但食物少/没钱 → 生存压力又不得不出去。

模拟器在纯 Python 侧闭环运行，不依赖 Unity；Unity 接入阶段再把 state 替换为
真实扫描结果（视野内资源数、天气、任务队列压力等）。
"""
from __future__ import annotations

import random
from typing import Any

from .actions import ACTION_INDEX

# ---------------------------------------------------------------------------
# 阈值常量 —— 与 WorkerBrain.cs:78-118 保持一致
# ---------------------------------------------------------------------------
HUNGRY_THRESHOLD = 30.0        # HungryThreshold
HUNGRY_CRITICAL = 15.0         # 饥饿 < 15 时跳过其他决策、扩大扫描半径
TIRED_THRESHOLD = 35.0         # TiredThreshold
SPIRIT_THRESHOLD = 30.0        # SpiritThreshold
AMBITION_THRESHOLD = 50.0      # AmbitionThreshold
SOCIALITY_THRESHOLD = 55.0     # SocialityThreshold
DILIGENCE_THRESHOLD = 45.0     # DiligenceThreshold
MOOD_LOW_THRESHOLD = 35.0      # MoodLowThreshold

# 现实规则新增阈值
WEATHER_HOT = 32.0             # 高于此温度视为「炎热」
WEATHER_COLD = 2.0             # 低于此温度视为「严寒」
SURVIVAL_HUNGRY = 35.0         # 低于此饥饿值视为「生存压力」
SURVIVAL_GOLD = 20.0           # 低于此金币视为「生存压力」
FAVORABILITY_BOUNTY = 35.0     # 接受悬赏的最低好感度（对应 FavorabilityRuleService 阈值 35）

LIFE_STAGES = ["bootstrap", "settled", "established"]
GOALS = ["earn_money", "build_structure", "stock_food", "craft_equipment"]
COMBAT_ATTRS = ["atn", "int_", "def_", "res", "crt", "csd", "spd", "hit"]


def _prob(base: float, delta: float) -> float:
    """基准概率 + 人格偏差，夹到 [0,1]（对应 WorkerBrain 的概率函数）。"""
    return max(0.0, min(1.0, base + delta))


def random_state(rng: random.Random) -> dict[str, Any]:
    """生成一个随机 Worker 状态（覆盖 feature_schema 的全部字段）。"""
    level = rng.randint(1, 20)
    life_stage = rng.choices(
        LIFE_STAGES, weights=[0.2, 0.5, 0.3], k=1)[0]

    # 建家阶段与生命阶段关联：bootstrap 尚未建好床，settled/established 已有家
    if life_stage == "bootstrap":
        home_build_stage = rng.choice([0, 1])
    else:
        home_build_stage = 2

    state: dict[str, Any] = {}
    # ---- 生存 ----
    state["hungry"] = round(rng.uniform(0, 100), 2)
    state["hungry_max"] = 100.0
    state["tired"] = round(rng.uniform(0, 100), 2)
    state["tired_max"] = 100.0
    state["spirit"] = round(rng.uniform(0, 100), 2)
    state["spirit_max"] = 100.0

    # ---- 生命 / 成长 ----
    state["hp"] = round(rng.uniform(0, 100), 2)
    state["hp_max"] = 100.0
    state["mp"] = round(rng.uniform(0, 100), 2)
    state["mp_max"] = 100.0
    state["level"] = level
    state["exp"] = round(rng.uniform(0, 100), 2)
    state["exp_max"] = 100.0
    for a in COMBAT_ATTRS:
        state[a] = round(rng.uniform(0, 100) * (0.5 + level / 40.0), 2)  # 属性随等级增长

    # ---- 人格（4 现有 + 2 预留扩展）----
    state["mood"] = round(rng.uniform(0, 100), 2)
    state["ambition"] = round(rng.uniform(0, 100), 2)
    state["diligence"] = round(rng.uniform(0, 100), 2)
    state["sociality"] = round(rng.uniform(0, 100), 2)
    state["greed"] = round(rng.uniform(0, 100), 2)      # 预留扩展位
    state["laziness"] = round(rng.uniform(0, 100), 2)   # 预留扩展位

    # ---- 经济 ----
    state["gold"] = round(rng.expovariate(1 / 60.0), 2)  # 长尾：多数穷，少数富

    # ---- 阶段 / 目标 ----
    state["bed_available"] = 1.0 if home_build_stage >= 2 else 0.0
    state["life_stage"] = life_stage
    state["home_build_stage"] = home_build_stage
    state["current_goal"] = rng.choice(GOALS)

    # ---- 社交 ----
    state["favorability"] = round(rng.uniform(0, 100), 2)

    # ---- 全局上下文 ----
    state["weather_temperature"] = round(rng.uniform(-20, 40), 2)
    state["task_pressure"] = round(rng.uniform(0, 100), 2)
    state["time_of_day"] = round(rng.uniform(0, 24), 2)

    # ---- 局部视野 ----
    state["nearby_food"] = rng.randint(0, 8)
    state["nearby_resource"] = rng.randint(0, 8)
    state["nearby_building"] = rng.randint(0, 8)
    state["nearby_worker"] = rng.randint(0, 8)

    return state


def decide(state: dict[str, Any], rng: random.Random) -> str:
    """根据状态决定行为，返回 ACTIONS 中的字符串标签。"""
    hungry = state["hungry"]
    tired = state["tired"]
    spirit = state["spirit"]
    mood = state["mood"]
    ambition = state["ambition"]
    diligence = state["diligence"]
    sociality = state["sociality"]
    greed = state["greed"]
    laziness = state["laziness"]
    gold = state["gold"]
    bed = state["bed_available"] > 0.5
    life_stage = state["life_stage"]
    home_stage = state["home_build_stage"]
    goal = state["current_goal"]
    fav = state["favorability"]
    temp = state["weather_temperature"]
    nearby_food = state["nearby_food"]
    nearby_resource = state["nearby_resource"]
    nearby_building = state["nearby_building"]

    # ---- 1. 生存优先（最高优先级，对应 WorkerBrain.Decide 最前两级）----
    # 饥饿：临界 <15 强制，否则 <30 优先
    if hungry < HUNGRY_THRESHOLD:
        if nearby_food > 0:
            return "eat"
        # 附近没食物但饥饿 → 去采集食物（生存驱动）
        if hungry < HUNGRY_CRITICAL or gold < SURVIVAL_GOLD:
            return "self_gather"
        return "eat"

    if tired < TIRED_THRESHOLD:
        return "sleep" if bed else "ground_sleep"

    if spirit < SPIRIT_THRESHOLD:
        return "wander"

    # ---- 2. 现实规则：天气 vs 生存压力（WorkerBrain 覆盖不到的权衡）----
    weather_harsh = temp > WEATHER_HOT or temp < WEATHER_COLD
    survival_pressure = hungry < SURVIVAL_HUNGRY or gold < SURVIVAL_GOLD

    if weather_harsh:
        if survival_pressure:
            # 食物少/没钱 → 生存压力压过「不愿出门」
            return "self_gather"
        elif ambition < AMBITION_THRESHOLD:
            # 天气恶劣 + 事业心低 + 无生存压力 → 不愿出门干活
            if rng.random() < _prob(0.3, laziness / 200.0):
                return "idle"
            return "wander"

    # ---- 3. Bootstrap 阶段：建家 + 囤食物（对应 DecideBootstrap）----
    if life_stage == "bootstrap":
        if home_stage == 0 or home_stage == 1:
            # 建家：钱不够买材料时先取仓库（withdraw）
            if gold < 30 and rng.random() < 0.3:
                return "withdraw"
            return "self_build"            # 建房间 / 建床
        # 家已建好 → 囤食物
        if hungry > HUNGRY_THRESHOLD + 20 and nearby_resource > 0:
            return "self_gather"

    # ---- 4. 目标驱动（对应 RefreshGoal 后的目标导向行为）----
    if goal == "build_structure" and nearby_building < 3:
        # 钱不够买建材 → 先取仓库（withdraw）
        if gold < 30 and rng.random() < 0.3:
            return "withdraw"
        if rng.random() < _prob(0.4, ambition / 300.0):
            return "self_build"
    if goal == "stock_food" and nearby_food > 0:
        return "self_gather"

    # ---- 5. 人格门控的捡取/存取（对应 WorkerBrain 的 Diligence 门控）----
    if diligence > DILIGENCE_THRESHOLD and rng.random() < _prob(0.25, (diligence - DILIGENCE_THRESHOLD) / 300.0):
        if rng.random() < 0.5:
            return "self_carry"
        return "pickup"

    # 贪婪高 → 倾向囤积/存入仓库
    if greed > 70 and rng.random() < _prob(0.2, (greed - 70) / 200.0):
        return "store"

    # ---- 6. 社交行为（对应概率函数 CalculatePostBounty/AcceptBounty）----
    # 心情过低 → 拒绝社交类行为（现实规则）
    if mood >= MOOD_LOW_THRESHOLD:
        # 有钱 + 社交高 → 发布悬赏
        if gold > 40 and sociality > SOCIALITY_THRESHOLD:
            if rng.random() < _prob(0.3, (sociality - SOCIALITY_THRESHOLD) / 200.0):
                return "post_bounty"
        # 勤奋 + 好感度足够 → 接受悬赏
        if diligence > DILIGENCE_THRESHOLD and fav > FAVORABILITY_BOUNTY:
            if rng.random() < _prob(0.25, (diligence - DILIGENCE_THRESHOLD) / 300.0):
                return "accept_bounty"

    # ---- 7. 一般赚钱/干活（对应事业心 + 心情好 → 自己采集）----
    if ambition > AMBITION_THRESHOLD and mood > 50 and nearby_resource > 0:
        if rng.random() < _prob(0.4, (ambition - AMBITION_THRESHOLD) / 300.0):
            return "self_gather"
    if goal == "earn_money" and nearby_resource > 0:
        return "self_gather"

    # ---- 8. 局部信息驱动的种植/采集 ----
    if nearby_resource > 0:
        # 食物/装备类目标 → 有一定概率选择种植（而非采集）
        if goal in ("stock_food", "craft_equipment") and rng.random() < 0.15:
            return "self_plant"
        if rng.random() < 0.2:
            return "self_gather"
    if nearby_food > 0 and hungry < 50:
        return "self_gather"

    # ---- 9. 兜底：漫游 / 空闲（对应 WorkerBrain 的 Wander/Idle 兜底）----
    if rng.random() < 0.5:
        return "wander"
    return "idle"


def generate_samples(num_samples: int, seed: int | None = None) -> list[dict[str, Any]]:
    """批量采样：返回 [{'state': {...}, 'action': 'eat'}, ...]。"""
    rng = random.Random(seed)
    samples: list[dict[str, Any]] = []
    for _ in range(num_samples):
        state = random_state(rng)
        action = decide(state, rng)
        samples.append({"state": state, "action": action})
    return samples


def to_label_index(action: str) -> int:
    """行为字符串 → 模型输出维度索引。"""
    return ACTION_INDEX[action]
