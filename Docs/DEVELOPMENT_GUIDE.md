# 大学生模拟器开发指南

本文档是本项目的协作基准。它基于三类材料整理：

- `C:\Users\24276\Downloads\Kings_Documentation.pdf`：Kings 资源包开发说明。
- `C:\Users\24276\Downloads\大学生模拟器_正式策划案_v0.2.docx`：游戏策划案。
- 当前 Unity 工程 `E:\escape`：已导入 Kings 资源包，Unity 版本为 `2022.3.62f3c1`。

项目当前目标不是重做一个完整系统，而是在 Kings 资源包基础上做一个
Reigns-like 大学生活滑卡叙事策略游戏。V1 Demo 的第一优先级是闭环：

1. 出现事件卡。
2. 玩家左滑或右滑。
3. 四项状态变化。
4. 根据条件、权重、冷却抽取下一张卡。
5. 状态失衡时进入结局。
6. 可以重开。

---

## 1. 给你的阅读版

### 1.1 这个项目现在是什么

这是一个 Unity 2022.3 项目，根目录已经包含：

- `Assets/Kings/`：第三方 Kings 卡牌滑动决策游戏资源包。
- `Assets/Kings/Game.unity`：当前启用的 Kings 示例主场景。
- `Assets/Kings/theMayor/Mayor_Game.unity`：另一个 Mayor 示例场景，目前没有启用到 Build Settings。
- `Assets/Kings/ImEx/cardlist.csv`：Kings 自带导出示例，主要覆盖卡牌文本。
- `Packages/manifest.json`：Unity 包依赖。
- `ExternalAssets/`：新建的源素材和大文件存放区。
- `.agents/skills/unity-skills/`：新装的 UnitySkills 工作区 skill。

重要判断：`Assets/Kings/` 应先视为资源包原件。我们的游戏内容和二次开发代码建议放到新的项目区，例如后续新建
`Assets/UniversitySimulator/`，不要直接把 Kings 示例改到不可回退。

### 1.2 游戏设计方向

策划案定义的核心体验是：

- 操作极简：只做左右滑。
- 数值隐藏：玩家不能看到精确加减。
- 四项状态平衡：身心、学业、人际、经济。
- 卡池概率叙事：不是传统剧情树，而是按状态、标签、权重、冷却抽卡。
- 结局表达失衡：不是简单失败，而是某种生活状态吞没玩家。

V1 不做复杂角色好感、大地图、道具系统、长篇剧情树、精确数值展示和大量动画。

### 1.3 你的主要职责

你负责决定游戏体验和内容质量：

- 确认四项状态的中文命名、图标风格、危险区表达。
- 写事件卡文本、左右选项、选择反馈。
- 判断每张卡的代价是否均衡，是否有明显正确答案。
- 决定结局文本和主题表达。
- 审核美术风格和卡图是否统一。
- 在 GitHub PR 中确认内容和玩法方向。

你不需要直接改 C# 才能参与。更适合你的工作入口是事件表、结局表、状态表和素材目录。

### 1.4 Codex/AI 的主要职责

Codex 负责把策划内容落到工程里：

- 阅读项目、PDF 和策划案，维护开发指南。
- 写或修改 C# 脚本、Unity 数据结构、导入工具和编辑器工具。
- 维护事件数据 schema，把策划表转成 Unity 可读数据。
- 新建/复制场景、Prefab、ScriptableObject 时保留 `.meta` 和可回退路径。
- 检查 GitHub 协作配置、`.gitignore`、`.gitattributes`、Git LFS。
- 在你明确要求时提交、推送、开 PR。

Codex 不能替你做最终创意判断，也不能在 Unity 编辑器没有打开、UnitySkills 服务没有运行时验证 Play Mode 行为。

### 1.5 最重要的协作规则

1. 不删除、不重排 `valueDefinitions.cs` 里的枚举值。
2. 不删除 Unity 的 `.meta` 文件。
3. 不直接大改 `Assets/Kings/` 原资源包，先复制到项目区再改。
4. 大图、PSD、音频母版、视频放 `ExternalAssets/`，最终进 Unity 的版本再放到 `Assets/`。
5. 事件文本可以频繁改；数值字段和触发条件要像程序接口一样谨慎改。
6. 每次进入实现前，先确认 V1 是否仍然只做滑卡闭环。

---

## 2. Codex/AI 执行版

### 2.1 当前工程事实

- Unity 版本：`2022.3.62f3c1`。
- Editor 设置：`Hidden Meta Files`，`Force Text` 序列化，适合 Git 协作。
- Build Settings 当前启用：`Assets/Kings/Game.unity`。
- Build Settings 当前禁用：`Assets/Kings/theMayor/Mayor_Game.unity`。
- Kings 示例卡牌数量：
  - `_templates`: 6
  - `Ads`: 2
  - `General`: 51
  - `Marriage`: 6
  - `Misc`: 2
  - `War`: 5
- Mayor 示例卡牌数量：11。
- 当前 `valueDefinitions.values` 已包含 Kings 和 Mayor 示例值，末尾是 Mayor 相关值。
- `KingsImEx` 的 CSV 默认只导入/导出卡牌组、卡名、样式和 EventScript 文本字段，不覆盖完整条件、结果、权重、冷却等玩法字段。

### 2.2 资源包核心机制摘要

Kings 的核心工作流：

1. 每张卡都有 `EventScript`。
2. `EventScript` 管文本、是否可随机抽取、优先级、概率、最大抽取次数、冷却、条件、结果、后续卡和 UnityEvent。
3. `CardStack` 管卡组、子卡池条件、可抽卡列表、高优先级卡、follow-up card、fallback card 和刷下一张卡。
4. `Swipe` 管左右/上下滑动输入、预览、键盘控制和动画参数。
5. `ValueScript` 管单个数值的范围、初始随机值、UI Slider/Text、变化事件和 Min/Max 事件。
6. `valueManager` 可检查缺失或重复值。
7. `GameLogger`、`Achievements`、`ScoreCalculator`、`Dictionary`、`Quests`、`Timeline` 是扩展系统。V1 默认不依赖它们，除非某个功能明确进入范围。

### 2.3 推荐项目结构

后续开发建议新建以下结构：

```text
Assets/UniversitySimulator/
  Scenes/
  Scripts/
  Data/
  Prefabs/
    Cards/
    UI/
  Art/
    Cards/
    UI/
    Icons/
  Audio/
  ScriptableObjects/
Docs/
ExternalAssets/
```

原则：

- `Assets/Kings/` 是 vendor asset，除必要兼容补丁外不要直接改。
- `Assets/UniversitySimulator/` 是本项目业务层。
- `ExternalAssets/` 是源素材和大文件暂存区，不会被 Unity 自动导入。
- `Docs/` 存开发指南、字段表、流程说明和内容规范。

### 2.4 字段修改权限总表

| 区域 | 可以直接改 | 需要先说明/PR 审核 | 禁止或高风险 |
| --- | --- | --- | --- |
| 事件文本 | 标题、描述、左右选项、反馈 | 大批量替换、改语气规范 | 无备份覆盖 prefab 文本 |
| 事件数值 | 单张卡的小幅数值调优 | 状态轴范围、结局阈值、隐藏变量权重 | 偷偷显示精确数值 |
| `valueDefinitions.cs` | 只在末尾追加新值 | 重命名旧值 | 删除、重排、插入到中间 |
| `ValueScript` | UI 绑定、初始值、Min/Max 事件 | 变更全局范围、结局触发事件 | 让枚举值缺失对应对象 |
| `CardStack` | 新增卡到正确组 | 改组条件、fallback、高优先级策略 | 移除大量卡导致无可抽卡 |
| Prefab | 复制模板后改本项目副本 | 改 Kings 原始模板 | 删除 `.meta` 或直接覆盖源模板 |
| 场景 | 新建本项目场景 | 改 Build Settings 主场景 | 无备份大改 Kings 示例场景 |
| 包依赖 | 添加明确需要的包 | 升级 Unity/Kings/核心包 | 手写不确定的 lock 文件 |
| Git/LFS | 新增 LFS 规则、素材目录 | 历史迁移、清理大文件历史 | 重写共享分支历史 |

### 2.5 `valueDefinitions.cs` 规则

Kings 文档明确指出：值在多个脚本和 prefab 中通过枚举序号引用。删除某一行或在中间插入会让已有卡牌指向错误值。

必须遵守：

- 允许：在 enum 最末尾追加新值。
- 谨慎：重命名旧值。序号不变，但会影响可读性和所有下拉显示。
- 禁止：删除旧值。
- 禁止：调整旧值顺序。
- 禁止：在旧值中间插入新值。

V1 有两条路线：

- 低风险路线：先保留 Kings 旧四值，在 UI 上把它们显示为大学状态。例如用现有值做原型，确认玩法闭环。
- 清晰路线：在 enum 末尾追加 `bodyMind`、`academics`、`relationships`、`economy`、隐藏变量等，再新建本项目 ValueScript 和场景。

推荐选择清晰路线，但必须在新场景中做，不要破坏 Kings 示例。

### 2.6 事件卡字段规则

策划案中的事件字段应作为业务数据标准：

- `eventId`：唯一编号，如 `E001`。
- `eventType`：普通、特殊、子卡池、结局。
- `title`：标题，2-6 字。
- `description`：描述，20-60 字。
- `leftChoice` / `rightChoice`：左右选项，短，像玩家脑中的念头。
- `leftFeedback` / `rightFeedback`：选择反馈，10-30 字。
- `stateDelta`：身心、学业、人际、经济的内部变化。
- `hiddenDelta`：自我选择、家庭期待、回避倾向、真实连接、内卷倾向、消费压力等。
- `conditions`：状态、时间、标签、隐藏变量条件。
- `weight`：符合条件后的抽取权重。
- `cooldown`：出现后多少轮不重复。
- `unique`：是否只出现一次。
- `tags`：宿舍、课程、家庭、经济、社交等。
- `artPath`：对应卡图路径。

不要把策划字段和 Kings 原生 ImEx CSV 混为一谈。Kings 的 `cardlist.csv` 当前表头是：

```text
GroupName;CardName;StyleName;EventScript.titleText;EventScript.questionText;EventScript.answerLeft;EventScript.answerRight;EventScript.answerUp;EventScript.answerDown
```

这只覆盖文本和样式，不覆盖完整游戏逻辑。若 V1 要做真正数据驱动，应该新增本项目事件 JSON/CSV schema，并写适配脚本把事件数据喂给运行时或生成 prefab。

### 2.7 EventScript 修改规则

`EventScript` 安全字段：

- `textFields.titleText`
- `textFields.questionText`
- `textFields.answerLeft`
- `textFields.answerRight`
- `textFields.answerUp`
- `textFields.answerDown`

`additionalTexts` 当前不能通过 Kings ImEx 导入导出。

逻辑字段需要谨慎：

- `isDrawable`：普通卡应为 true；follow-up 卡通常不应随机抽取。
- `isHighPriorityCard`：条件满足时会优先抽到，不能滥用。
- `cardPropability`：仅影响普通抽取概率，不影响 high priority。
- `maxDraws`：控制每局最大出现次数。
- `redrawBlockCnt`：控制冷却。
- `conditions`：决定卡是否可出现。
- `Results`：左右滑具体结果，支持 simple、conditional、random conditions、random。
- `followUpCard`：选择后直接或延迟进入后续卡。
- `changeValueOnCardDespawn`：无论滑向哪边都会执行。
- `OnCardSpawn` / `OnCardDespawn`：可以触发日志、成就、全局消息等；注意 Spawn 事件可能因恢复存档重复触发。

规则：

- 文本可批量改。
- 单卡数值可小幅调。
- 条件和 follow-up 改动必须记录原因。
- UnityEvent 改动必须在 PR 描述里说明触发时机。
- 不通过脚本字符串硬改 prefab YAML，除非明确知道序列化结构并有回滚。

### 2.8 CardStack 和卡池规则

CardStack 是抽卡总入口。每张可抽卡必须被挂进某个 group。

规则：

- `General` 或主卡池组必须始终有可用卡，否则会掉到 fallback。
- 子卡池通过 group 的 `subStackCondition` 控制。
- 特殊阶段如考试周、小组作业、兼职、生病、家庭压力可以用临时子卡池实现。
- 高优先级卡只用于结局、强制引导、特殊触发，不用于普通事件。
- follow-up 卡通常 `isDrawable=false`，由上一张卡链接进入。
- 每次新增卡后，需要检查：卡在 CardStack 中、条件可达、冷却合理、不会造成无卡可抽。

### 2.9 数值和结局规则

策划案要求内部值为 0-100，初始值 50，任一状态 `<=0` 或 `>=100` 触发结局。

V1 四项状态：

- 身心
- 学业
- 人际
- 经济

每项都有低端和高端失衡：

- 身心过低：透支崩溃。
- 身心过高：自我封闭。
- 学业过低：学业崩盘。
- 学业过高：绩点机器。
- 人际过低：社交孤岛。
- 人际过高：所有人的好人。
- 经济过低：生活失控。
- 经济过高：被金钱吞没。

规则：

- 玩家不显示精确数字。
- 可以显示图标、状态条偏移、颜色、闪烁、音效、文本频率。
- 不要把状态做成“越高越好”。
- 每次选择最好有收益和损失。
- 结局文本表达主题，不写简单失败提示。

### 2.10 内容写作规则

事件文本遵守策划案原则：

- 短：标题 2-6 字，描述 20-60 字，反馈 10-30 字。
- 具体：写具体物品、场景和动作。
- 留白：不解释所有前因后果。
- 有代价：两个选项都应有收益和损失。
- 不说教：不替玩家判断哪个选择更高尚。
- 日常但有刺：普通事件要能引出压力、孤独或自我怀疑。

AI 可以帮你扩写事件，但不能替代你最终判断主题是否准确。

### 2.11 UnitySkills 安装状态

本项目已加入第三方 UnitySkills：

- Unity 包依赖：`Packages/manifest.json`
- 包名：`com.besty.unity-skills`
- 来源：`https://github.com/Besty0728/Unity-Skills.git?path=/SkillsForUnity`
- 工作区 skill：`.agents/skills/unity-skills/`
- Agent 配置：`.agents/skills/unity-skills/scripts/agent_config.json`

注意：

- 这是第三方工具，不是 OpenAI 官方 curated skill。
- Unity 打开项目后会解析 Git 依赖并更新 `Packages/packages-lock.json`。
- 要让 Codex 真正操作 Unity，需要在 Unity 中打开 `Window > UnitySkills > Server` 并启动本地 REST 服务。
- 在服务未运行前，Codex 只能改文件，不能直接验证场景、Prefab、Inspector 或 Play Mode。

### 2.12 GitHub 协作规则

当前仓库：

- 分支：`main`
- 远端：`https://github.com/zhu-bai-w/escape.git`
- Git LFS：已在本地启用。
- `.gitattributes` 已添加 Unity 文本序列化和大文件 LFS 规则。

提交内容建议：

- 应提交：`Assets/`、`Packages/`、`ProjectSettings/`、`Docs/`、`ExternalAssets/README.md`、必要源素材。
- 不提交：`Library/`、`Temp/`、`Obj/`、`Build/`、`Logs/`、`UserSettings/`、`.vs/`。
- Unity 文件变更必须包含对应 `.meta`。
- 二进制素材走 Git LFS。
- 不在共享分支上重写历史，除非团队明确同意。

分支建议：

- 新功能：`codex/feature-name`
- 文档：`codex/docs-development-guide`
- 素材导入：`codex/assets-batch-name`
- 修复：`codex/fix-issue-name`

PR 描述至少写：

- 本次改了什么。
- 是否改了数据字段、数值轴、CardStack、Prefab 或场景。
- 是否需要 Unity 打开后重新生成 lock 或 meta。
- 测试过什么。

仓库已提供 `.github/pull_request_template.md`，开 PR 时按模板勾选高风险项。

### 2.13 大文件和图片存储规则

源素材放 `ExternalAssets/`：

- `ExternalAssets/Images/`
- `ExternalAssets/Audio/`
- `ExternalAssets/Video/`
- `ExternalAssets/Models/`
- `ExternalAssets/References/`

Unity-ready 资源放 `Assets/UniversitySimulator/Art/` 或对应业务目录。

规则：

- 源图、PSD、PSB、视频、音频母版先放 `ExternalAssets/`。
- 经过裁切、压缩、命名确认后再导入 Unity。
- 导入 Unity 后必须提交 `.meta`。
- 不把账号、授权文件、密钥放进仓库。
- 大文件历史迁移需要单独计划，不在普通功能 PR 里顺手做。

### 2.14 V1 推荐开发路线

阶段 1：隔离项目区

- 新建 `Assets/UniversitySimulator/`。
- 复制 Kings 主场景为本项目场景。
- 复制卡牌模板为本项目模板。
- 保留 Kings 示例可运行。

阶段 2：定义值和 UI

- 追加大学生活四项状态和隐藏变量。
- 创建对应 ValueScript GameObject。
- 设置 0-100 范围、初始 50。
- UI 只显示模糊状态，不显示数字。

阶段 3：事件数据

- 建立事件表 schema。
- 先做 20-30 张普通卡。
- 实现权重、冷却、唯一卡、标签和条件筛选。
- 决定是运行时读取 JSON，还是编辑器生成 prefab。

阶段 4：结局闭环

- 至少实现 6 个结局。
- 先做四项低端结局和两个高端结局。
- 结局后可以重开。

阶段 5：测试和调参

- 验证不会无卡可抽。
- 验证状态能进入危险区。
- 验证玩家不需要知道精确数值也能感知趋势。
- 验证事件文本没有明显最优解。

### 2.15 Codex 操作边界

Codex 可以直接做：

- 读文档、写文档。
- 扫描项目结构。
- 修改 C#、JSON、CSV、Markdown。
- 添加项目目录、配置 Git LFS。
- 生成事件表模板和数据校验脚本。
- 在用户要求时创建分支、提交、推送、开 PR。

Codex 需要先说明再做：

- 改 `Packages/manifest.json`。
- 改 `valueDefinitions.cs`。
- 改场景、Prefab、CardStack。
- 批量改事件数值和结局阈值。
- 批量导入图片或音频。
- 做 Git LFS 历史迁移。

Codex 不应做：

- 删除或重排 enum 值。
- 删除 `.meta`。
- 未经确认替换 Kings 原始资源。
- 在没有 Unity 验证的情况下声称 Play Mode 已通过。
- 修改账号、密钥、付费插件授权文件。
- 私自推送到远端或开 PR。

### 2.16 每次任务前检查清单

开始前：

- 当前改动是不是还属于 V1 闭环？
- 是否会碰 `Assets/Kings/` 原件？
- 是否会碰 enum、CardStack、Prefab、场景或包依赖？
- 是否需要 UnitySkills 服务验证？
- 是否会产生大文件或二进制素材？

结束前：

- `git status` 是否只包含本次相关改动？
- Unity 文件是否带 `.meta`？
- 大文件是否命中 LFS 规则？
- 文档是否更新了字段或协作规则？
- 是否明确说明哪些验证未能执行？
