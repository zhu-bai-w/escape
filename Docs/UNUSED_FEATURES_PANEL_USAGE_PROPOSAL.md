# 大学生模拟器未充分使用功能与面板使用策划案

日期：2026-06-28

## 1. 结论

当前项目的 V1 核心已经比较明确：左右滑卡、四项状态、抽卡、主线事件、真结局。主场景已经迁移到 `Assets/UniversitySimulator/Scenes/Game.unity`，卡牌数据当前为 63 张，其中 46 张普通卡、16 张主线卡、1 张真结局卡。

场景里仍保留了 Kings 资源包的多个扩展系统和面板：成就、任务、时间线、物品栏、排行榜、本局日志、设置等。它们不是完全没有接入，而是处在三种状态：

1. **可直接改造使用**：成就、玩家档案、时间线/日志、本局结算。
2. **系统接着了但内容不适配**：任务、物品、排行榜。
3. **应该保留为工具面板**：设置、退出确认、清档、重开。

建议第一阶段不要把所有旧系统都打开给玩家，而是把它们统一改造成一个“校园档案”层：记录玩家每局的选择、主线进度、结局、成就和回顾。这样不破坏 V1 的极简滑卡体验，也能让已经存在的面板产生价值。

## 2. 当前扫描结果

### 2.1 主场景中的面板和系统

| 功能/面板 | 当前入口 | 当前状态 | 结论 |
| --- | --- | --- | --- |
| 玩家信息面板 | `MenuCanvas/MenuPanel/PlayerInfoPanel` | 对象存在，默认 active | 可改造成“本局档案” |
| 成就面板 | `MenuCanvas/MenuPanel/AchievementsPanel` | 对象存在，默认 inactive | 需要内容重写后启用 |
| 成就弹窗 | `GameCanvas/GamePanel/TopPanel/AchievementsPopupPanel` | 对象存在，默认 active | 可用于解锁提示 |
| 任务面板 | `MenuCanvas/MenuPanel/QuestsPanel` | 对象存在，默认 inactive | 系统可用，内容不适配 |
| 排行榜面板 | `MenuCanvas/MenuPanel/HighscorePanel` | 对象存在，默认 inactive | 可改成本地“结局回顾” |
| 设置面板 | `MenuCanvas/MenuPanel/SettingsPanel` | 对象存在，默认 inactive | 保留工具用途 |
| 物品栏 | `GameCanvas/InventoryPanel_landscape` | 对象存在，默认 active | 不建议作为 V1 玩法系统 |
| 任务管理器 | `Quests` | 场景对象存在，脚本启用 | 新游戏会调用 `FillActiveQuests` |
| 时间线管理器 | `Timeline` | 场景对象存在，脚本启用 | 新游戏会调用 `CreateNewFollowingHistory` |
| 背包管理器 | `Inventory` | 场景对象存在，脚本启用 | 会加载旧物品资源 |
| 计分系统 | `ScoreCalculator` | 场景对象存在，脚本启用 | GameOver 前会计算分数 |
| 移动端排行榜 | `MobileLeaderboard` | 场景对象存在，脚本启用 | 编译宏未启用，当前不适合展示 |
| 日志系统 | `GameLogger` | 场景对象存在，脚本启用 | 可改造成“本局回忆” |

### 2.2 当前不适配内容

| 系统 | 现有内容问题 | 影响 |
| --- | --- | --- |
| 成就 | 只有 `marry` 和 `rule_years` 两类，文本仍是 `King and Queen`、`long live the king` | 与大学主题完全不匹配，并且当前卡牌里没有发现调用 `add_Achievement` 的入口 |
| 任务 | 资源仍是 `Get Married`、`Win a War`、`Consult a Witch`、`Reign for 10/20/30 years` | 新游戏会抽任务，但目标和奖励都是旧王国玩法 |
| 物品 | 资源仍是 `Sharp Sword`、`Crystal Ball`、`Magic Bean`、`Mysterious Egg` 等 | 会把游戏从克制的生活叙事拉向奇幻道具玩法 |
| 时间线 | 资源仍是 `You became king`、`War won`、`Divorced`、`Bankrupt` 等 | 如果展示给玩家，会破坏大学生活叙事 |
| 排行榜/分数 | UI 文本仍有 `Highscore`、`Best Scores`、`Longest Reign`、`Shortest Reign` | 可用，但必须改名和改指标 |
| 移动端排行榜 | 按钮会调用移动端排行榜/成就接口，但 `USE_MOBILE_LEADERBOARD` 未启用 | 原型阶段不应放在玩家入口 |

### 2.3 当前可复用的新系统

当前项目已经有一个更适合大学主题的长期进度系统：

- `UniversityTrueEndingProgress` 记录总天数、本局天数、GameOver 次数、真结局是否触发。
- 默认永久标记为 `MAIN_01`、`MAIN_02`、`MAIN_03`、`MAIN_04`。
- 主线入口卡 `S100/S200/S300/S400` 会对应四条主线：社交、学业、身心、经济。
- 真结局需要四个永久标记都完成。

这套系统比旧 AchievementsScript 更贴近当前项目。成就、档案、时间线应该优先读取这套数据，而不是继续沿用 `marry/rule_years`。

## 3. 总体设计方向：校园档案

把底部菜单和旧扩展面板统一改造成“校园档案”。它不是额外玩法入口，而是玩家在滑卡之外查看自己经历的地方。

建议菜单结构：

| 新标签 | 复用面板 | 用途 |
| --- | --- | --- |
| 档案 | `PlayerInfoPanel` | 显示本局天数、四项状态趋势、主线进度 |
| 印记 | `AchievementsPanel` | 显示长期成就、主线完成、真结局进度 |
| 回忆 | `HighscorePanel` 或 Timeline UI | 显示本局关键选择和结局记录 |
| 目标 | `QuestsPanel` | 只做轻量提示，不做强制任务 |
| 设置 | `SettingsPanel` | 音量、清档、重开、退出 |

原则：

- 不在游戏中显示精确状态数值。
- 不引入传统 RPG 道具养成。
- 不把任务做成“必须完成的 checklist”。
- 成就只奖励理解和回顾，不提供数值强度。
- 所有新增入口都服务“我这次大学生活变成了什么样”。

## 4. 各系统使用方案

### 4.1 成就面板：改造成“成长印记”

**当前问题**

成就系统还在场景里，弹窗和面板也存在，但成就类型仍是旧王国模板。当前只支持：

- `marry`
- `rule_years`

且当前卡牌和主线没有发现实际调用 `add_Achievement`。因此成就面板现在更像是“壳子存在，内容未接入”。

**建议定位**

成就不作为数值奖励，而作为玩家理解主题的回顾系统。命名建议从 `Achievements` 改为“成长印记”或“留痕”。

**第一批成就设计**

| 成就 | 触发条件 | 来源 |
| --- | --- | --- |
| 网线另一端 | 完成 `MAIN_01` 社交主线 | `UniversityTrueEndingProgress.GetPermanentFlag("MAIN_01")` |
| 写下自己的故事 | 完成 `MAIN_02` 学业主线 | `MAIN_02` |
| 承认疲惫 | 完成 `MAIN_03` 身心主线 | `MAIN_03` |
| 那张票 | 完成 `MAIN_04` 经济主线 | `MAIN_04` |
| 人生是旷野 | 触发真结局 | `HasTriggeredTrueEnding` |
| 又来一天 | 单局达到 10 天 | `CurrentRunDays >= 10` |
| 熬过月末 | 单局达到 30 天 | `CurrentRunDays >= 30` |
| 反复横跳 | GameOver 达到 3 次 | `GameOverCount >= 3` |
| 四线交汇 | 四条主线都完成但尚未触发真结局 | 四个 MAIN flag |

**落地方式**

短期建议新写一个轻量桥接脚本，例如 `UniversityAchievementBridge`：

- 读取 `UniversityTrueEndingProgress`。
- 把旧 `AchievementsScript` 的 UI 面板当展示层。
- 不继续扩展 `achievementTyp` 枚举来硬塞所有新成就，避免以后又被枚举限制。

如果要继续使用旧 `AchievementsScript`，需要改造：

- 把 `achievementTyp` 扩展为大学主题。
- 把旧文案和图标全部替换。
- 在主线完成、真结局、GameOver、天数变化时触发对应成就。

更推荐桥接脚本，因为当前已有长期进度数据，不需要强行套进旧成就计数模型。

### 4.2 玩家信息面板：改造成“本局档案”

**当前问题**

`PlayerInfoPanel` 是菜单中默认 active 的内容，但它来自旧 Kings 模板，和当前大学主题不完全一致。

**建议显示内容**

| 信息 | 展示方式 | 说明 |
| --- | --- | --- |
| 本局天数 | “第 X 天” | 可读 `UniversityTrueEndingProgress.CurrentRunDays` |
| 总经历天数 | “已走过 X 天” | 可读 `LifetimeDays` |
| 本局状态 | 四个图标 + 模糊状态词 | 不显示精确数值 |
| 主线进度 | 4 个小图标点亮 | 社交、学业、身心、经济 |
| 本局关键词 | 2-4 个词 | 根据最近高频 tag 或关键选择生成 |

**状态词建议**

| 状态值区间 | 展示词 |
| --- | --- |
| 0-20 | 危险 |
| 21-40 | 偏低 |
| 41-60 | 平稳 |
| 61-80 | 偏高 |
| 81-100 | 失衡边缘 |

注意：这些词不等于“越高越好”。例如经济过高也可以叫“被安全感绑住”，人际过高可以叫“过度迎合”。

### 4.3 时间线与日志：改造成“回忆录”

**当前问题**

`Timeline` 和 `GameLogger` 都存在。新游戏时还会调用时间线的 `CreateNewFollowingHistory`。但当前时间线资源仍是国王、战争、结婚、破产等旧事件。

**建议定位**

把时间线改成“回忆录”，只记录对玩家有意义的节点，不记录所有普通卡。

建议记录：

- 第一次进入某条主线。
- 主线关键选择。
- 四项状态首次进入危险区。
- GameOver 的原因。
- 真结局触发。

**第一批 HistoryEvent 替换建议**

| 旧资源 | 新事件建议 |
| --- | --- |
| `YouBecameKing` | `入学第一天` |
| `WarStarted` | `开始硬撑` 或 `开始回避` |
| `WarWon` | `撑过考试周` |
| `WarLost` | `被压力击穿` |
| `Marriage_YouMarried` | `建立一段真实连接` |
| `MoneyCredit` | `把钱花在自己身上` |
| `GameOver_Money` | `生活失控` |
| `GameOver_People` | `社交孤岛` |

**落地方式**

- 保留 `Timeline` 框架。
- 新建大学主题 `HistoryEvent` 资源，替换旧 Resources 下的王国事件。
- 在主线卡的 `EventScript` extras 或 `UniversityMainlineCardHook` 中添加记录。
- GameOver 前由已有 `preGameover` 事件写入结局日志。

### 4.4 排行榜与分数：改造成“结局回顾”

**当前问题**

`ScoreCalculator` 会在 GameOver 前执行，`HighscorePanel` 也存在；但 UI 仍是 `Highscore`、`Best Scores`、`Longest Reign`、`Shortest Reign`，指标是旧王国逻辑。

**建议定位**

不要做传统排行榜。大学模拟器的主题不是“分数越高越好”，而是“你怎样度过这段生活”。建议改成“结局回顾”：

- 最近 3 次人生片段。
- 每次存活天数。
- 结局标题。
- 完成的主线印记数量。
- 一个总结词：例如“过度自律”“社交透支”“短暂自由”“旷野”。

**分数是否保留**

可以保留内部 score 用于排序，但 UI 不直接叫分数。建议命名为：

- “回忆长度”
- “完成度”
- “生活张力”

如果必须显示数值，只在结算或回顾面板显示，不在滑卡过程显示。

### 4.5 任务面板：暂改成“阶段提示”

**当前问题**

任务系统已经接入：新游戏会调用 `FillActiveQuests`，按钮可以重抽和重置任务。但所有 QuestDefinition 还是旧内容，如结婚、战争、统治年数。

**风险**

任务容易把游戏从“选择造成生活偏移”变成“照着目标刷条件”。这和当前策划案的 V1 目标冲突。

**建议定位**

第一阶段不要叫“任务”，改叫“阶段提示”或“想做的事”。它只给玩家一些柔性方向，不要求完成。

示例：

| 提示 | 触发/完成 |
| --- | --- |
| 这周别把身心压到危险区 | 连续 5 天身心不进危险 |
| 给关系留一点空间 | 完成一次社交主线选择 |
| 不要只做正确的事 | 选择一次真实自我相关卡 |
| 给自己花一次钱 | 完成经济主线的右选路径 |

**落地建议**

短期可以先隐藏 `QuestsPanel` 的入口，等成就和回忆录稳定后再启用。若启用，必须先：

- 删除或替换全部旧 QuestDefinition。
- 把 `Reroll Quests` 改成中文且弱化目标感。
- 取消“奖励道具”显示，改成“记录/感想/印记”。

### 4.6 物品栏：不建议作为 V1 道具系统

**当前问题**

`Inventory` 系统启用，`InventoryPanel_landscape` 也在场景中。当前物品仍是奇幻/王国资源，如剑、水晶球、魔豆、魔鸡、宝箱。

**策划判断**

V1 不建议使用传统道具，因为：

- 会引入额外策略层，稀释滑卡核心。
- “使用道具改变数值”容易让玩家追求最优解。
- 当前策划已经明确 V1 不做道具系统。

**如果以后要用**

建议改成“纪念物”而不是“道具”：

| 纪念物 | 来源 | 作用 |
| --- | --- | --- |
| 展览票根 | 经济主线 | 只用于回忆录展示 |
| 没交出去的草稿 | 学业主线 | 解锁一段文本 |
| 网友发来的合照 | 社交主线 | 点亮主线印记 |
| 退烧药盒 | 身心主线 | 记录一次危险区经历 |

纪念物不提供可消费的数值收益。它只作为“你经历过什么”的证据。

### 4.7 移动端排行榜：原型阶段隐藏

`Mobile_Leaderboard` 当前存在，按钮也会调用相关方法，但移动平台宏未启用，且没有平台成就配置。建议原型阶段隐藏移动端排行榜和平台成就按钮，只保留本地回顾。

## 5. 推荐优先级

### P0：先清理玩家能看到的旧王国内容

目标：避免玩家打开菜单看到 King、Queen、War、Reign 这类内容。

要做：

- 把 `AchievementsPanel` 的旧标题和文案替换或暂时隐藏。
- 把 `QuestsPanel` 入口暂时隐藏，或替换所有旧任务。
- 把 `HighscorePanel` 改名为“回顾”。
- 把 `InventoryPanel_landscape` 暂时隐藏，除非已经改成纪念物。
- 隐藏移动端排行榜按钮。

### P1：启用成长印记

目标：让成就面板服务当前主线和真结局。

要做：

- 新增大学主题成就数据。
- 接入 `MAIN_01` 到 `MAIN_04`。
- 接入真结局触发状态。
- 成就弹窗改中文文案。
- 成就面板显示总进度，例如 `4/9`。

### P2：启用本局档案与回忆录

目标：让玩家能回看这局走到哪里。

要做：

- `PlayerInfoPanel` 显示本局天数、总天数、主线进度。
- `Timeline` 改为大学主题事件资源。
- `GameLogger` 只记录关键节点。
- GameOver 后展示本局摘要。

### P3：选择是否启用阶段提示

目标：在不破坏滑卡体验的前提下给玩家轻度方向。

要做：

- 替换全部旧 QuestDefinition。
- 把任务奖励从道具改为印记/回忆。
- 控制每局只显示 1-2 个提示。
- 不强迫玩家完成。

### P4：纪念物系统

目标：只在游戏主题需要时启用。

要做：

- 删除奇幻物品。
- 新建大学生活纪念物。
- 不提供直接数值收益。
- 只用于回忆、收藏、成就。

## 6. 面板改名建议

| 当前名称 | 建议名称 |
| --- | --- |
| `Achievements` | 成长印记 |
| `Quests` | 阶段提示 / 想做的事 |
| `Highscore` | 回顾 / 结局回顾 |
| `Best Scores` | 最近几次人生片段 |
| `Inventory` | 纪念物 |
| `Player Info` | 档案 |
| `Reroll Quests` | 换一组想法 |
| `Reset Quests` | 清空提示 |

## 7. 数据接入建议

### 7.1 成就数据来源

优先读这些数据：

- `UniversityTrueEndingProgress.CurrentRunDays`
- `UniversityTrueEndingProgress.LifetimeDays`
- `UniversityTrueEndingProgress.GameOverCount`
- `UniversityTrueEndingProgress.HasTriggeredTrueEnding`
- `UniversityTrueEndingProgress.GetPermanentFlag("MAIN_01")`
- `UniversityTrueEndingProgress.GetPermanentFlag("MAIN_02")`
- `UniversityTrueEndingProgress.GetPermanentFlag("MAIN_03")`
- `UniversityTrueEndingProgress.GetPermanentFlag("MAIN_04")`

后续可新增：

- Seen card IDs：已见过哪些卡。
- Seen tags：经历过哪些标签。
- Ending IDs：触发过哪些结局。
- Mainline choice records：主线关键选择。

### 7.2 回忆录数据来源

短期可使用：

- 主线入口卡和主线完成事件。
- GameOver 事件。
- 真结局事件。
- 进入危险区事件。

长期可记录：

- 最近 5 个关键选择。
- 玩家本局最高/最低状态趋势。
- 最后导致结局的 3 张卡。

### 7.3 任务数据来源

如果启用阶段提示，不建议继续用旧 Quest 资源。建议新建大学主题资源：

- `Quest_StayBalanced_5Days`
- `Quest_CompleteSocialMainline`
- `Quest_SpendForSelf`
- `Quest_WriteOwnStory`

奖励不建议使用 InventoryItem，改为：

- 解锁成就。
- 写入回忆录。
- 点亮档案标签。

## 8. 实施顺序

1. 静态清理 UI 文案，把所有玩家可见的旧王国文本替换掉。
2. 隐藏移动端排行榜按钮和旧任务/旧物品入口。
3. 新增“成长印记”数据表或桥接脚本。
4. 将四条主线和真结局接入成就面板。
5. 改造玩家档案面板，显示本局天数和主线进度。
6. 替换 Timeline 的 HistoryEvent 资源为大学主题。
7. GameOver 后展示“结局回顾”，复用 HighscorePanel 的布局。
8. 评估是否启用阶段提示。
9. 最后再决定是否把 Inventory 改为纪念物系统。

## 9. 验证清单

每次改造后需要检查：

- 主场景仍能进入滑卡流程。
- 菜单打开后不再出现 King、Queen、War、Reign 等旧主题词。
- 滑卡过程中不显示精确数值。
- 完成 `MAIN_01` 到 `MAIN_04` 后对应印记能点亮。
- 真结局触发后能点亮最终印记。
- GameOver 后能看到本局回顾。
- 隐藏的旧任务/物品入口不会挡住现有 UI。
- Unity Console 没有 missing script、missing prefab、空引用报错。

## 10. 最终建议

第一阶段最值得投入的是 **成就面板、玩家档案、回忆录/结局回顾**。这三项能直接提升当前 63 张卡和 4 条主线的反馈闭环。

任务和物品暂时不要作为正式玩法打开。它们现在虽然有系统、有面板、有资源，但内容仍是 Kings 示例，不符合大学生活主题。等主线、成就、回顾稳定后，再把任务改成轻量阶段提示，把物品改成不可消费的纪念物，会更符合当前游戏方向。
