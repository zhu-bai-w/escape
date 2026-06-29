# 成就功能实现方案

日期：2026-06-29

## 当前项目基线

当前项目已经迁移并保留了 Kings 的成就骨架，但只适合作为 UI 和弹窗基础，不适合继续作为完整成就数据模型。

已确认内容：

- Unity 版本：`2022.3.62f3c1`
- 当前场景：`Assets/UniversitySimulator/Scenes/Game.unity`
- 当前场景中 `Scripts` 对象挂有 `CardStack`、`GameStateManager`、`AchievementsScript`、`UniversityTrueEndingController`、`UniversityValueEndingController`
- 成就面板存在：`MenuCanvas/MenuPanel/AchievementsPanel`
- 成就弹窗存在：`GameCanvas/GamePanel/TopPanel/AchievementsPopupPanel`
- Kings 旧脚本存在：`AchievementsScript.cs`、`addAchievement.cs`
- 当前 `AchievementsScript.achievementTyp` 只有 `marry` 和 `rule_years` 两个枚举值
- 当前卡牌没有发现实际调用 `addAchievement.add_Achievement`
- 当前结局控制器 `UniversityValueEndingController` 已经能判断哪个数值在本次滑卡后越界，并保留 `eventId`、`valueType`、`triggerOnMaximum`、`deviation`

结论：不要把所有新成就继续硬塞进 `AchievementsScript.achievementTyp` 枚举。推荐新增大学模拟器自己的成就系统，复用 Kings 的面板、滚动列表、弹窗动画和图标。

## 推荐架构

项目规模属于小型游戏，不需要复杂框架。推荐 5 个类加 1 份数据表：

| 名称 | 类型 | 职责 |
| --- | --- | --- |
| `UniversityAchievementDefinition` | `ScriptableObject` 或可序列化类 | 一条成就定义：ID、标题、描述、分类、图标、条件 |
| `UniversityAchievementCatalog` | `ScriptableObject` | 持有全部成就定义，供系统和 UI 读取 |
| `UniversityAchievementState` | 普通 C# 静态类 | 读写解锁状态、进度、弹窗队列 |
| `UniversityAchievementSystem` | `MonoBehaviour` | 订阅选择、结局、主线、天数事件，并判断解锁 |
| `UniversityAchievementListView` | `MonoBehaviour` | 刷新成就面板列表 |
| `UniversityAchievementPopup` | `MonoBehaviour` | 显示单个或排队的解锁弹窗 |

短期可以先用 ScriptableObject。后续如果希望策划直接改 CSV，也可以从 `Docs/ACHIEVEMENT_TABLE.md` 派生一份 `achievement_catalog.csv`，由编辑器导入器生成 ScriptableObject。

## 数据结构

### 成就定义

建议字段：

```csharp
public enum UniversityAchievementCategory
{
    Ending,
    Mainline,
    Choice,
    Tendency,
    Progress
}

public enum UniversityAchievementConditionType
{
    ValueEnding,
    MainlineFlag,
    TrueEnding,
    CurrentRunDays,
    GameOverCount,
    SpecificChoice,
    TagCountInRun,
    StatPositiveChoiceCountInRun,
    StatNegativeStreakInRun,
    CompositeChoiceCountInRun
}
```

`UniversityAchievementDefinition` 建议字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | string | 稳定 ID，例如 `ACH_END_ECON_HIGH` |
| `title` | string | 面板和弹窗标题 |
| `description` | string | 面板和弹窗描述 |
| `category` | enum | 分类筛选 |
| `conditionType` | enum | 判断方式 |
| `targetEventId` | string | 单卡选择或结局 ID |
| `targetFlagId` | string | 主线永久标记，例如 `MAIN_01` |
| `targetValue` | `valueDefinitions.values` | 数值结局或倾向统计使用 |
| `triggerOnMaximum` | bool | 高溢出还是低跌穿 |
| `targetDirection` | enum/string | `left`、`right`、`up`、`down`、`add0`、`add1` |
| `targetTag` | string | 标签统计，例如 `真实自我` |
| `targetCount` | int | 需要达到的次数 |
| `icon` | Sprite | 面板和弹窗图标 |
| `hiddenBeforeUnlock` | bool | 是否隐藏未解锁标题 |

### 运行状态

成就是长期元进度，建议不跟随手动存档槽覆盖。使用 `SecurePlayerPrefs` 或 `PlayerPrefs`，键名统一：

| 数据 | Key |
| --- | --- |
| 是否解锁 | `US.Achievements.Unlocked.<id>` |
| 解锁时间戳 | `US.Achievements.UnlockedAt.<id>` |
| 长期进度 | `US.Achievements.Progress.<id>` |
| 待展示弹窗队列 | `US.Achievements.PendingPopupQueue` |

本局统计只在内存中维护，新游戏时重置：

| 统计 | 用途 |
| --- | --- |
| `tagCount` | 一局内经历了多少张某标签卡 |
| `specificChoiceSet` | 是否做过某张卡某方向选择 |
| `statPositiveChoiceCount` | 一局内多少次选择让某数值上升 |
| `statNegativeChoiceCount` | 一局内多少次选择让某数值下降 |
| `statNegativeStreak` | 连续负向选择 |
| `moneySpendForMoodCount` | 经济下降但身心或人际上升的组合选择 |
| `lastValueEnding` | 最近一次数值结局 |

## 事件来源

### 1. 单卡选择事件

当前 `CardStack.leftSwipe()`、`rightSwipe()`、`upSwipe()`、`downSwipe()`、`additionalChoices()` 在调用 `EventScript` 后仍能拿到当前 `spawnedCard`。建议在这些方法里增加一行记录调用。

推荐调用位置：

```csharp
EventScript es = spawnedCard.GetComponent<EventScript>();
if (es != null) {
    es.onLeftSwipe();
    UniversityAchievementSystem.RecordChoice(spawnedCard, "left");
}
onCardSwipe.Invoke();
nextCard(E_moveOutDirection.left);
```

右滑、上滑、下滑和附加选项同理。记录要发生在 `nextCard()` 之前，因为这时当前卡还没有被销毁。

选择事件要记录：

| 字段 | 来源 |
| --- | --- |
| `eventId` | 优先从新增 `UniversityCardMetadata` 读取；没有则从实例名 `US_E001(Clone)` 解析 |
| `direction` | `left`、`right`、`up`、`down`、`add0`、`add1` |
| `tags` | 从卡表导入到 `UniversityCardMetadata`，或从运行时注册表查 |
| `valueDeltas` | 从卡表导入到 `UniversityCardMetadata`，不要运行时反推 |
| `chainId` | 主线卡使用 |
| `chainOrder` | 主线卡使用 |

建议给导入器补一个轻量组件：

```csharp
public class UniversityCardMetadata : MonoBehaviour
{
    public string eventId;
    public string[] tags;
    public float leftBodyMind;
    public float leftAcademics;
    public float leftRelationships;
    public float leftEconomy;
    public float rightBodyMind;
    public float rightAcademics;
    public float rightRelationships;
    public float rightEconomy;
    public string chainId;
    public int chainOrder;
}
```

如果不想马上改导入器，可以先用 `cards_v1_program.csv` 生成一个 `UniversityCardRuntimeCatalog` ScriptableObject，用 `eventId` 查标签和数值变化。

### 2. 数值结局事件

`UniversityValueEndingController.EvaluateAfterCardSwipe()` 已经选出了最佳结局：

- `bestRule.eventId`
- `bestRule.valueType`
- `bestRule.triggerOnMaximum`
- `bestDeviation`

在 `bestRule != null` 后增加：

```csharp
UniversityAchievementSystem.RecordValueEnding(
    bestRule.eventId,
    bestRule.valueType,
    bestRule.triggerOnMaximum,
    bestDeviation
);
```

注意不要只在 `GameStateManager.OnGameOver` 里弹窗。`executeGameover()` 会立刻重载场景，弹窗可能来不及显示。推荐做法：

1. `RecordValueEnding` 立刻解锁对应成就，并把弹窗写入待展示队列。
2. 如果当前还在结局卡展示阶段，就立刻显示。
3. 如果马上发生场景重载，就在下一次场景启动后从待展示队列补弹。

这样“金钱溢出结束一局游戏”既能准确记录，也不会因为重载丢失提示。

### 3. 主线完成事件

当前 `UniversityMainlineCardHook.ApplyChoice()` 会设置永久标记：

```csharp
UniversityTrueEndingProgress.SetPermanentFlag(permanentFlag, true);
```

建议紧接着调用：

```csharp
UniversityAchievementSystem.RecordMainlineFlag(permanentFlag);
```

也可以不改 `UniversityMainlineCardHook`，而是在每次选择后统一扫描：

```csharp
GetPermanentFlag("MAIN_01")
GetPermanentFlag("MAIN_02")
GetPermanentFlag("MAIN_03")
GetPermanentFlag("MAIN_04")
```

短期推荐扫描，侵入更低。长期推荐显式事件，调试更清楚。

### 4. 真结局事件

当前 `UniversityTrueEndingController` 在满足条件后会：

```csharp
cardStack.followUpCard = trueEndingStartCard;
UniversityTrueEndingProgress.SetTrueEndingTriggered(true);
```

建议在这里调用：

```csharp
UniversityAchievementSystem.RecordTrueEndingQueued();
```

同时 `UniversityAchievementSystem` 在启动时也要扫描 `HasTriggeredTrueEnding`，避免玩家已经触发但弹窗或面板未刷新。

### 5. 天数和 GameOver 事件

天数可以直接监听已有事件：

```csharp
UniversityTrueEndingProgress.OnCurrentRunDaysChanged += EvaluateRunDayAchievements;
```

GameOver 可以监听：

```csharp
GameStateManager.instance.OnGameOver.AddListener(EvaluateGameOverAchievements);
```

但弹窗展示仍要考虑场景重载。GameOver 相关弹窗同样建议写入待展示队列。

## 判断流程

每次收到事件后按这个顺序处理：

1. 更新本局统计。
2. 更新长期进度。
3. 遍历 `UniversityAchievementCatalog` 中尚未解锁的成就。
4. 根据 `conditionType` 判断是否达成。
5. 达成后写入 `Unlocked`。
6. 加入弹窗队列。
7. 通知面板刷新。

伪代码：

```csharp
void EvaluateAll(UniversityAchievementContext context)
{
    foreach (var achievement in catalog.achievements)
    {
        if (state.IsUnlocked(achievement.id))
        {
            continue;
        }

        if (!conditionEvaluator.IsMet(achievement, context))
        {
            continue;
        }

        state.Unlock(achievement.id);
        popup.Enqueue(achievement);
        listView.RefreshIfOpen();
    }
}
```

## 与旧 AchievementsScript 的关系

不建议继续扩展 `AchievementsScript.achievementTyp` 枚举来承载所有新成就，原因：

- 枚举现在只有 `marry` 和 `rule_years`，语义已经不匹配。
- 每加一个成就都要改代码并重编译。
- 枚举序列化依赖顺序，后续改动容易污染场景里的旧配置。
- 它的 `achievementStage` 更像“一个计数成就的多个阶段”，不适合单卡选择和数值结局。

推荐改造方式：

| 旧组件 | 处理方式 |
| --- | --- |
| `AchievementsScript` | 保留，短期只用于读取旧场景引用或作为弹窗字段容器 |
| `addAchievement` | 保留兼容，不再作为新卡默认入口 |
| `AchievementsPanel` | 复用为新列表面板 |
| `AchievementsPopupPanel` | 复用动画、图标、标题、描述 |
| `achievementTyp` | 不再扩展，除非只做临时原型 |

如果想最小改动复用弹窗，可以给 `AchievementsScript` 增加一个公开方法：

```csharp
public void ShowCustomAchievement(string title, string description, Sprite sprite)
```

内部复用现有 `achievementAnimator`、`anim_titleText`、`anim_descriptionText`、`anim_achievementImage`。这样不用重建弹窗动画。

## 文件落点

建议新增文件：

```text
Assets/UniversitySimulator/Scripts/Achievements/
  UniversityAchievementCategory.cs
  UniversityAchievementConditionType.cs
  UniversityAchievementDefinition.cs
  UniversityAchievementCatalog.cs
  UniversityAchievementState.cs
  UniversityAchievementSystem.cs
  UniversityAchievementListView.cs
  UniversityAchievementPopup.cs
  UniversityCardMetadata.cs
```

建议新增数据资产：

```text
Assets/UniversitySimulator/Data/Achievements/
  UniversityAchievementCatalog.asset
```

如果希望继续由表格维护：

```text
Assets/UniversitySimulator/Data/achievements_v1.csv
Assets/UniversitySimulator/Editor/UniversityAchievementImportTool.cs
```

## 分阶段实施

### P0：只做结局和主线成就

目标：先让“金钱的诅咒”这类成就能跑通。

要做：

- 新增成就数据模型和状态保存。
- 接入 `UniversityValueEndingController.RecordValueEnding`。
- 启动时扫描 `MAIN_01` 到 `MAIN_04` 和 `HasTriggeredTrueEnding`。
- 复用 `AchievementsPopupPanel` 显示弹窗。
- 面板先显示静态列表和解锁状态。

### P1：接入单卡选择成就

目标：支持“在某张卡里选了某个选项就解锁”。

要做：

- 在卡牌导入时写入 `UniversityCardMetadata`。
- 在 `CardStack` 的各个滑卡入口记录方向。
- 实现 `SpecificChoice` 条件。
- 落地 6 到 10 个高记忆点选择成就。

### P2：接入倾向型成就

目标：根据一局内的选择倾向给反馈。

要做：

- 统计一局内标签次数。
- 统计四项数值正负选择次数。
- 支持连续负向选择。
- 支持组合选择，例如经济下降但身心或人际上升。

### P3：编辑器工具

目标：让新增成就不需要程序手填 ScriptableObject。

要做：

- 新增 `achievements_v1.csv`。
- 新增导入器生成或更新 `UniversityAchievementCatalog.asset`。
- 校验 ID 重复、图标缺失、引用不存在的卡牌 ID。

## 验证清单

最少需要覆盖这些测试：

| 场景 | 预期 |
| --- | --- |
| 经济高溢出触发 `END07` | 解锁 `ACH_END_ECON_HIGH` |
| 经济低跌穿触发 `END08` | 解锁 `ACH_END_ECON_LOW` |
| 完成 `MAIN_01` | 解锁 `ACH_MAIN_SOCIAL` |
| 四个主线 flag 全部为 true | 解锁 `ACH_MAIN_ALL` |
| `HasTriggeredTrueEnding == true` | 解锁 `ACH_TRUE_END` |
| 选择 `E001` 左侧 | 解锁 `ACH_CHOICE_E001_L` |
| 同一成就重复触发 | 不重复写解锁，不重复增加已完成数量 |
| GameOver 立刻重载场景 | 弹窗不会丢失，下一次场景启动能补弹 |
| 打开成就面板 | 已解锁和未解锁状态正确 |

建议在 `Assets/UniversitySimulator/Editor/UniversitySimulatorRegressionTests.cs` 中追加 EditMode 测试，沿用项目现有测试风格。
