# Kings 资源包迁移方案与注意事项

## 目标

把当前游戏从 `Assets/Kings` 资源包目录中迁移出来，让 `Assets/UniversitySimulator` 成为游戏内容、运行时脚本、场景、卡牌模板和编辑器工具的主目录。迁移完成后，`Assets/Kings` 应该可以被删除，并且不影响以下功能：

- 主场景启动、滑动开始、正式进入卡牌流程。
- 大学模拟器普通卡、主线事件链、真结局卡牌。
- 数值系统、卡牌抽取、后续卡牌跳转、条件判断。
- 卡牌 CSV 校验、导入、场景接线、快速测试工具。
- 已经移除的首次选择角色界面不再出现。

## 当前项目状态

### 项目基础信息

- Unity 版本：`2022.3.62f3c1`
- 产品名：`escape`
- 当前版本号：`1.55`
- 当前启用构建场景：`Assets/Kings/Game.unity`
- 当前禁用构建场景：`Assets/Kings/theMayor/Mayor_Game.unity`
- 当前没有发现 `.asmdef`，所有脚本目前在默认程序集里编译。

### 目录体量

| 目录 | 文件数 | 体积 |
| --- | ---: | ---: |
| `Assets/Kings` | 1195 | 约 14.73 MB |
| `Assets/UniversitySimulator` | 303 | 约 36.69 MB |
| `Assets/Resources` | 2 | 很小 |
| `Assets/Scenes` | 2 | 很小 |
| `Assets/Screenshots` | 6 | 约 1 MB |

`Assets/UniversitySimulator` 已经是自研内容的主体，包含卡牌数据、卡牌预制体、美术、音频、脚本和编辑器工具。但运行时骨架仍然位于 `Assets/Kings`。

### 当前工作区风险

当前有大量未提交改动，主要集中在：

- `Assets/Kings/Game.unity`
- `Assets/UniversitySimulator/Data/cards_v1_kings_import_report.json`
- `Assets/UniversitySimulator/Editor/*`
- `Assets/UniversitySimulator/Prefabs/Cards/Main/*`
- 一批旧分类卡牌目录下的删除项

迁移前必须先做一个可回退点。建议先提交当前稳定状态，或至少复制整个项目备份。不要在脏工作区里直接批量移动和删除 `Assets/Kings`。

## 当前依赖结论

### 1. 主场景仍在 Kings 目录

当前游戏入口仍是：

```text
Assets/Kings/Game.unity
```

这是最重要的迁移对象。只删除 `Assets/Kings` 会直接丢失主场景。

### 2. 自研卡牌仍依赖 Kings 模板

GUID 扫描显示，`Assets/UniversitySimulator/Prefabs/Cards` 下的 63 张卡牌都引用了：

| 被引用资源 | 引用情况 |
| --- | --- |
| `Assets/Kings/cards/_templates/card_template00.prefab` | 63 张大学模拟器卡牌引用 |
| `Assets/Kings/cards/_templates/cs_None.asset` | 63 张大学模拟器卡牌引用 |
| `Assets/Kings/graphics/crown_lge.png` | 17 张主线/真结局卡牌引用 |

这说明自研卡牌大概率是基于 Kings 的模板预制体生成或派生的。迁移时必须移动这些模板和样式资源，并保留 `.meta` 里的 GUID。不能复制一份新模板再删除旧模板，否则卡牌预制体会断引用。

### 3. 自研脚本依赖 Kings 运行时类

`Assets/UniversitySimulator/Scripts` 和 `Assets/UniversitySimulator/Editor` 里直接使用了这些 Kings 类：

- `CardStack`
- `EventScript`
- `GameStateManager`
- `valueManager`
- `valueDefinitions`
- `ValueScript`
- `GameDictionary`
- `ReadOnlyInspector`
- `KingsCardStyleList`

因此第一阶段不能删除 Kings 脚本。必须先把运行时脚本迁移到新目录，并保持类名稳定。

### 4. 当前主场景直接引用的 Kings 运行时脚本

`Assets/Kings/Game.unity` 里仍直接挂载或引用了大量 Kings 脚本，其中包括：

- 核心流程：`CardStack.cs`、`EventScript.cs`、`GameStateManager.cs`
- 数值系统：`valueManager.cs`、`ValueScript.cs`、`valueDefinitions.cs`、`changeValue.cs`
- UI 展示：`ValueDisplay.cs`、`ValueChangePreview.cs`、`valueDependent*.cs`
- 输入和卡牌移动：`Swipe.cs`、`KeyboardToEvent.cs`
- 存档和日志：`SecurePlayerPrefs.cs`、`GameLogger.cs`、`GameDictionary.cs`
- 设置和菜单：`MusicPlayer.cs`、`saveSlider.cs`、`UniToggle.cs`、`LoadScene.cs`、`ExitScript.cs`
- 其他系统：`CountryNameGenerator.cs`、`GenderGenerator.cs`、`Timeline.cs`、`Quests.cs`、`Inventory.cs`、`AchievementsScript.cs`、`HighScoreNameLinker.cs`

其中有些功能可能最终不需要，但它们现在仍在场景里有引用。删除前必须先从场景或预制体中解除引用，并确认不会编译失败。

### 5. 编辑器工具里有硬编码 Kings 路径

必须修改这些路径，否则后续重新导入卡牌会继续写回 Kings 目录。

| 文件 | 当前硬编码 |
| --- | --- |
| `Assets/UniversitySimulator/Editor/UniversityCardImportTool.cs` | `Assets/Kings/cards/_templates/CardStyle_List.asset` |
| `Assets/UniversitySimulator/Editor/UniversityCardImportTool.cs` | `Assets/Kings/Game.unity` |
| `Assets/UniversitySimulator/Editor/UniversityCardQuickTesterWindow.cs` | `Assets/Kings/cards` |
| `ProjectSettings/EditorBuildSettings.asset` | `Assets/Kings/Game.unity`、`Assets/Kings/theMayor/Mayor_Game.unity` |
| `ProjectSettings/ProjectSettings.asset` | `Assets/Kings/graphics/icon512.png`、`Assets/Kings/graphics/splash.png` |
| `ProjectSettings/ProjectSettings.asset` | `D:/Unity/Kings/kings.keystore` |

### 6. 选择角色功能已经可以作为删除候选

当前 `Game.unity` 里的开场卡已经不再跳转到 `SelectKing`，而是跳转到 `_StartCard`。`SelectKing.prefab` 仍在 `Assets/Kings/cards/General/SelectKing.prefab`，但已经不是启动流程必需资源。

后续删除前仍要做一次全项目 GUID 扫描，确保没有其他场景或工具引用它。

## 迁移原则

### 保留 GUID，不直接复制

对还要保留的场景、预制体、材质、贴图、脚本，优先使用 Unity 的 Project 面板移动，或使用 `AssetDatabase.MoveAsset`。这样 `.meta` 文件会跟着走，GUID 保持不变。

不要使用文件管理器复制、粘贴、删除再重建。这样很容易生成新 GUID，导致：

- 场景组件丢脚本。
- 预制体变成 missing prefab。
- 卡牌变体断开模板。
- UI 图片、字体、动画控制器丢引用。

### 先迁移所有权，再删除功能

迁移分两步：

1. 先把当前游戏仍依赖的 Kings 资源移动到 `Assets/UniversitySimulator` 下，保持游戏能跑。
2. 再按引用扫描和功能验证逐步删除无用功能。

不要一边移动一边大量删功能。否则一旦出问题，很难判断是路径迁移导致，还是功能删除导致。

### 第一阶段不改类名和序列化字段

第一阶段不要重命名 `CardStack`、`EventScript`、`valueManager` 等类，也不要给它们加 namespace。Unity 的场景和预制体通过脚本 GUID 和序列化字段名保持引用。过早重命名会增加 missing script 和字段丢失风险。

等 `Assets/Kings` 被安全删除后，再做第二轮代码整理。

## 目标目录建议

建议把迁移后的目录整理成下面这样：

```text
Assets/UniversitySimulator/
  Scenes/
    Game.unity
  Runtime/
    CardSystem/
    Values/
    GameFlow/
    UI/
    EventMessages/
    LegacyIntegrations/
  Editor/
    UniversityCardImportTool.cs
    UniversityCardQuickTesterWindow.cs
    LegacyKingsEditors/
  Prefabs/
    CardTemplates/
      card_template00.prefab
      _StartCard.prefab
    Cards/
      Main/
      Mainline/
      TrueEnding/
  Data/
    CardStyles/
      CardStyle_List.asset
      cs_None.asset
  Art/
    LegacyKingsUI/
    cards/
  Audio/
```

`LegacyIntegrations` 里可以先放还没确认是否能删的旧系统，例如 Timeline、Inventory、Quests、Achievements、HighScore 等。等验证它们确实不用，再删除。

## 具体迁移步骤

### 第 0 步：准备回退点

1. 确认当前游戏能正常进 Play Mode。
2. 清理或提交现有改动。
3. 单独建立迁移分支。
4. 记录当前构建场景、卡牌数量、主线链数量和真结局入口。

最低验证基线：

- `Assets/Kings/Game.unity` 可以打开。
- 开始游戏不会出现 `Select Ruler`。
- 普通卡牌能抽取。
- 主线卡牌能进入。
- 真结局逻辑没有报错。

### 第 1 步：移动主场景

把：

```text
Assets/Kings/Game.unity
```

移动到：

```text
Assets/UniversitySimulator/Scenes/Game.unity
```

注意事项：

- 必须用 Unity 移动资源，保留场景 GUID。
- 移动后检查 Build Settings，只保留新的 `Assets/UniversitySimulator/Scenes/Game.unity`。
- 从 Build Settings 移除禁用的 `Assets/Kings/theMayor/Mayor_Game.unity`。
- 更新任何工具中对旧场景路径的硬编码。

### 第 2 步：移动核心脚本

第一阶段建议整体移动 `Assets/Kings/scripts`，不要先拆：

```text
Assets/Kings/scripts
```

迁移到：

```text
Assets/UniversitySimulator/Runtime/LegacyKingsRuntime
```

理由：

- 当前没有 asmdef，脚本之间依赖较多。
- 场景里直接挂了很多 Kings 脚本。
- 自研编辑器工具依赖 `EventScript`、`CardStack`、`KingsCardStyleList`、`valueDefinitions`。

移动后先保证编译通过，再做第二阶段瘦身。

### 第 3 步：移动卡牌模板和样式

必须移动并保留 GUID：

```text
Assets/Kings/cards/_templates/card_template00.prefab
Assets/Kings/cards/_templates/cs_None.asset
Assets/Kings/cards/_templates/CardStyle_List.asset
Assets/Kings/cards/General/_StartCard.prefab
```

建议目标位置：

```text
Assets/UniversitySimulator/Prefabs/CardTemplates/card_template00.prefab
Assets/UniversitySimulator/Data/CardStyles/cs_None.asset
Assets/UniversitySimulator/Data/CardStyles/CardStyle_List.asset
Assets/UniversitySimulator/Prefabs/CardTemplates/_StartCard.prefab
```

如果 `CardStyle_List.asset` 里还引用了 `card_template01` 到 `card_template04`，先不要立刻删除。迁移后可以创建一个新的大学模拟器专用样式列表，只保留当前用到的 `card_template00` 和 `cs_None`。

### 第 4 步：移动主场景依赖的 UI、美术、字体、音频、动画

优先用 Unity 的依赖工具生成完整依赖列表。依赖来源至少包括：

- 新位置的 `Game.unity`
- `Assets/UniversitySimulator/Prefabs/Cards/Main`
- `Assets/UniversitySimulator/Prefabs/Cards/Mainline`
- `Assets/UniversitySimulator/Prefabs/Cards/TrueEnding`
- `card_template00.prefab`
- `_StartCard.prefab`

当前扫描已确认主场景仍引用这些类型的 Kings 资源：

- 字体：`SourceSansPro-Regular.ttf`
- UI 图片：`panel.png`、`slider.png`、`button.png`、`white.png`、`circle*.png`、`menu_icon.png`、`settings.png` 等
- 主题图标：`authority_icon.png`、`charisma_icon.png`、`health_icon.png`、`intelligence_icon.png`、`look_icon.png`、`luck_icon.png`
- 卡牌底图：`card.png`、`card_back03.png`、`card_front03.png`
- 音效：`click1.ogg`、`handleSmallLeather*.ogg`、`pickup3.ogg` 等
- 动画控制器：`MainStatsSlider.controller`、`AchievementsPopupPanel.controller` 等

保守做法：

```text
Assets/Kings/graphics  -> Assets/UniversitySimulator/Art/LegacyKingsUI
Assets/Kings/fonts     -> Assets/UniversitySimulator/Art/LegacyKingsFonts
Assets/Kings/audio     -> Assets/UniversitySimulator/Audio/LegacyKingsSfx
Assets/Kings/animation -> Assets/UniversitySimulator/Animation/LegacyKings
```

激进做法：

- 只移动依赖列表里实际出现的资源。
- 每删一批都打开场景检查 missing reference。

建议先采用保守做法。资源包本身不大，先保功能，再做精简。

### 第 5 步：修改编辑器工具路径

修改 `UniversityCardImportTool.cs`：

```csharp
const string StyleListPath = "Assets/UniversitySimulator/Data/CardStyles/CardStyle_List.asset";
const string TargetScenePath = "Assets/UniversitySimulator/Scenes/Game.unity";
```

修改 `UniversityCardQuickTesterWindow.cs` 的搜索目录：

```csharp
static readonly string[] CardFolderRoots = {
    "Assets/UniversitySimulator/Prefabs/Cards",
    "Assets/UniversitySimulator/Prefabs/CardTemplates"
};
```

迁移后重新跑：

- `University Simulator/Cards/Validate Program Card CSV`
- `University Simulator/Cards/Import Program Cards And Wire Scene`

导入报告里不应再出现 `Assets/Kings/Game.unity` 或 `Assets/Kings/cards/_templates/CardStyle_List.asset`。

### 第 6 步：更新项目设置

需要检查：

- Build Settings 只保留新的主场景。
- Player icon 和 splash 不再指向 `Assets/Kings/graphics/icon512.png`、`Assets/Kings/graphics/splash.png`。
- Android keystore 路径不要再是 `D:/Unity/Kings/kings.keystore`。
- 如果后续改包名、公司名、图标，在迁移完成后单独做，不和资源移动混在一起。

### 第 7 步：删除无用功能

完成前面迁移后，先跑一次全项目扫描：

```text
搜索 Assets/Kings
搜索 Kings/
扫描是否仍有非 Kings 文件引用 Kings GUID
检查 Unity Console 是否有 missing script / missing prefab
```

确认没有引用后，再按批次删除。

#### 第一批可删除候选

这些最像原资源包示例内容，和当前游戏关系弱：

- `Assets/Kings/theMayor`
- `Assets/Kings/ImEx`
- `Assets/Kings/cards/Marriage`
- `Assets/Kings/cards/War`
- `Assets/Kings/cards/Misc`
- `Assets/Kings/cards/General/SelectKing.prefab`
- 未被 `CardStyle_List` 和自研卡牌引用的 `card_template01` 到 `card_template04`
- 未被项目设置引用的 Kings 图标和启动图

#### 第二批需要先解除场景引用

这些功能现在可能仍有脚本或 UI 对象在主场景里：

- Inventory
- Quests
- Timeline
- Achievements
- HighScore / Leaderboard
- Marriage
- War
- CountryName / GenderGenerator
- firstStartGameObjectSetter

处理方式：

1. 先在场景里确认对应 UI 面板、按钮、事件是否还在用。
2. 如果不用，先从场景移除组件和对象。
3. 编译并进 Play Mode。
4. 再删除对应脚本和资源。

不要先删脚本。先删脚本会导致场景出现 missing script，后续很难判断哪些对象原本该保留。

#### 第三批代码瘦身

在 `Assets/Kings` 删除完成后，再考虑：

- 把 `LegacyKingsRuntime` 拆成 `CardSystem`、`Values`、`GameFlow`、`UI`。
- 删除没有任何场景和代码引用的类。
- 把 `valueDefinitions` 里的旧王国数值改成大学模拟器专用命名。
- 为稳定模块加 asmdef。
- 给核心系统加 namespace。

这些属于二期整理，不建议和迁移同一批做。

## 保留功能清单

迁移时必须保持这些行为不变：

### 启动流程

- 主场景从新的 `Assets/UniversitySimulator/Scenes/Game.unity` 启动。
- 开始卡显示后，滑动进入 `_StartCard`。
- 不再出现 `Select Ruler` 或角色选择按钮。

### 卡牌系统

- `CardStack` 能读取 `allCards`。
- 普通卡池、主线卡池、真结局卡池仍存在。
- `followUpCard` 逻辑正常。
- 四向/左右滑动结果正常。
- 卡牌抽取计数、阻塞计数、保存和恢复正常。

### 数值系统

- `bodyMind`
- `academics`
- `relationships`
- `economy`
- `years`

这些大学模拟器核心数值必须继续随机初始化、变化、显示和参与条件判断。

### 主线和真结局

- `UniversityMainlineEventScheduler` 仍能挂到场景中的 `CardStack`。
- 主线入口概率规则保持不变。
- 永久标记 `MAIN_01` 到 `MAIN_04` 正常记录。
- `UniversityTrueEndingController` 能检测条件并投放真结局卡。
- `UniversityTrueEndingReturnToNewGame` 能回到新游戏。

### 编辑器导入工具

- CSV 校验仍能成功。
- 导入仍生成 63 张卡。
- 导入后场景里的 CardStack 分组仍正确。
- 报告不再写旧 Kings 路径。

## 验证门槛

迁移完成不能只看“能打开 Unity”。需要通过这些检查：

### 文件引用检查

必须满足：

```text
ProjectSettings/EditorBuildSettings.asset 不再引用 Assets/Kings
Assets/UniversitySimulator/Editor 不再硬编码 Assets/Kings
Assets/UniversitySimulator/Data 的新报告不再指向 Assets/Kings
非文档文件中不再出现 Assets/Kings 或 Kings/
非 Kings 文件不再引用 Kings GUID
```

### Unity 检查

必须满足：

- Console 没有编译错误。
- Console 没有 missing script。
- Console 没有 missing prefab。
- 主场景所有关键 UI 图片和字体正常显示。
- 卡牌模板没有断开。
- 自研卡牌预制体没有丢 `EventScript`。

### Play Mode 检查

建议清空 PlayerPrefs 后测试：

1. 进入主场景。
2. 滑动开始。
3. 确认不会出现角色选择界面。
4. 进入 `_StartCard`。
5. 继续滑动，能抽到普通卡。
6. 数值变化正常。
7. 主线事件能按概率出现。
8. 真结局条件满足时能进入真结局。
9. 游戏结束或重新开始后不卡死。

### 工具检查

迁移后执行：

1. `University Simulator/Cards/Validate Program Card CSV`
2. `University Simulator/Cards/Import Program Cards And Wire Scene`
3. 打开快速测试窗口，确认能找到新目录里的卡牌。
4. 检查生成报告路径是否全部指向 `Assets/UniversitySimulator`。

## 删除 `Assets/Kings` 的最终条件

只有全部满足后，才能删除原资源包目录：

- 新主场景已经在 `Assets/UniversitySimulator/Scenes`。
- Build Settings 不再包含 `Assets/Kings` 场景。
- 所有保留资源都已经移动到 `Assets/UniversitySimulator`。
- 全项目 GUID 扫描显示没有外部文件引用 `Assets/Kings` 中的 GUID。
- 代码搜索没有运行时硬编码 `Assets/Kings`。
- Unity 编译通过。
- Play Mode 启动流程、卡牌抽取、主线、真结局通过。
- 导入工具已改到新路径并验证成功。
- 当前迁移改动已提交。

## 我建议的执行顺序

1. 先提交当前工作区，锁定可回退版本。
2. 建立 `Assets/UniversitySimulator/Scenes`、`Runtime`、`Data/CardStyles`、`Prefabs/CardTemplates`、`Art/LegacyKingsUI` 等目录。
3. 用 Unity 移动 `Assets/Kings/Game.unity` 到新场景目录。
4. 用 Unity 移动 `Assets/Kings/scripts` 到 `Assets/UniversitySimulator/Runtime/LegacyKingsRuntime`。
5. 用 Unity 移动当前卡牌实际依赖的模板、样式、字体、UI 图、音频、动画。
6. 修改导入工具和快速测试工具里的旧路径。
7. 打开 Unity，让它重新导入和编译。
8. 修复 missing reference。
9. 运行卡牌校验和导入工具。
10. 做 Play Mode 验证。
11. 第一批删除明显无用的示例资源。
12. 再次验证。
13. 第二批处理可选功能系统。
14. 再次验证。
15. 确认 `Assets/Kings` 无引用后删除整个目录。

## 关键注意事项

- 不要直接删除 `Assets/Kings`。
- 不要先重命名核心类。
- 不要复制模板后删除原模板。
- 不要把功能删除和路径迁移混在一个大提交里。
- 不要忽略编辑器工具里的硬编码路径。
- 不要只依赖搜索文本路径，Unity 的真实引用主要靠 GUID。
- 不要删除 `.meta` 文件。
- 不要在大量未提交改动存在时做批量迁移。

## 结论

当前项目已经具备迁移条件，但不能一次性删除 Kings。正确路线是：

1. 把 Kings 中当前游戏真正使用的运行时骨架迁到 `Assets/UniversitySimulator`。
2. 修改构建入口和编辑器工具路径。
3. 用 GUID 扫描和 Play Mode 验证确认功能完整。
4. 再分批删除示例场景、示例卡牌、角色选择、Mayor、未用插件和旧 UI。

这样可以把项目所有权从 Kings 资源包转移出来，同时最大限度保护当前大学模拟器游戏的卡牌、主线和真结局功能。
