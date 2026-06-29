# 任务卡功能临时隔离说明

## 当前状态

项目中仍保留 Kings 原包的 Quest/任务系统资源与脚本，但当前游戏流程暂时不启用任务卡。

隔离后的开始流程为：

1. 玩家滑动 `MainMenuCard`。
2. `MainMenuCard` 不再指定 `_StartCard` 或任务卡作为后续卡。
3. `CardStack` 直接从日常卡池抽取普通卡。

这样可以避免未完成的 Kings 示例任务卡出现在正式流程中。

## 已隔离的入口

- `Assets/UniversitySimulator/Scenes/Game.unity`
  - `MainMenuCard` 的左右选项不再设置 `_StartCard` 为 `followUpCard`。
  - `GameStateManager.OnNewGame` 不再调用 `Quests.FillActiveQuests`。
  - 场景中的 `Quests.featureEnabled` 设为 `false`。

- `Assets/UniversitySimulator/Runtime/LegacyIntegrations/Addons/Quests/Scripts/Quests.cs`
  - 新增 `featureEnabled` 开关。
  - 关闭时不会加载任务目录、自动填充任务或注册卡牌销毁回调。

- `Assets/UniversitySimulator/Runtime/LegacyIntegrations/Addons/Quests/Scripts/Quest_UIList.cs`
  - 如果任务功能关闭，任务列表 UI 不会生成 Kings 示例任务条目。

## 保留内容

以下内容没有删除，后续可以继续使用或改造：

- Kings Quest 脚本：
  - `Quests`
  - `Quest_UIList`
  - `Quest_UIItem`
  - `QuestDefinition`
- Kings 示例任务资源：
  - `Quest_ConsultWitch`
  - `Quest_Reign30`
  - `Quest_WinWar`
  - 其它 `Assets/UniversitySimulator/Runtime/LegacyIntegrations/Addons/Quests/Resources` 下的任务资源
- 任务 UI Prefab：
  - `QuestUIElement_*`
- 场景中的 `Quests` 对象与 `QuestsPanel`

## 后续恢复步骤

当正式任务卡内容准备好后，可以按下面顺序恢复：

1. 将 `Game.unity` 中 `Quests.featureEnabled` 改回 `true`。
2. 如需新局自动发任务，把 `Quests.FillActiveQuests` 重新接到 `GameStateManager.OnNewGame`。
3. 如果任务卡需要作为开局后第一张特殊卡，把 `MainMenuCard` 的左右结果重新配置 `followUpCard`。
4. 替换或清理 Kings 示例任务资源，避免 `Consult a Witch`、`Reign for 30 years`、`Win a War` 等占位内容进入正式流程。
5. 重新运行 `UniversitySimulatorRegressionTests`，并更新其中关于任务隔离的断言。

## 验证要求

当前隔离状态需要满足：

- 开始后不会显示 `_StartCard` 或 Kings 示例任务卡。
- 开始后直接进入日常卡池。
- Unity 控制台无任务系统相关错误。
- `UniversitySimulatorRegressionTests` 通过。
