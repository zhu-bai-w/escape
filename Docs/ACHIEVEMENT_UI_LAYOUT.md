# 成就界面 UI 与布局

日期：2026-06-29

## 当前可复用组件

通过 UnitySkills 只读查询和场景文件检查，当前场景已经有完整的成就入口壳子：

| 对象 | 路径 | 当前用途 |
| --- | --- | --- |
| 成就按钮 | `MenuCanvas/MenuPanel/BottomMenuPanel/MenuButtonPanel/AchievementsButton` | 底部菜单入口 |
| 成就面板 | `MenuCanvas/MenuPanel/AchievementsPanel` | 旧 Kings 成就面板，当前可改造成新列表 |
| 成就滚动列表 | `MenuCanvas/MenuPanel/AchievementsPanel/Scroll View/Viewport/Content` | 可复用为成就列表容器 |
| 旧成就条目 | `Achievement1Marry`、`Achievement2RuleYears5/10/20/30` | 可作为布局参考，不建议继续用旧命名 |
| 成就弹窗 | `GameCanvas/GamePanel/TopPanel/AchievementsPopupPanel` | 可复用弹窗动画和字段 |
| 成就控制脚本 | `Scripts` 对象上的 `AchievementsScript` | 可复用弹窗字段，也可逐步替换 |

当前 UI 使用 UGUI，主要组件是 `Image`、`Text`、`Button`、`Scroll View`。项目虽然安装了 TextMeshPro，但现有面板仍以 legacy `Text` 为主。短期保持 legacy `Text`，避免一次性重做字体和引用。

## 命名建议

玩家可见名称建议从“Achievements”改成“成长印记”或“成就”。如果想保持直接，底部按钮用“成就”，面板标题用“成长印记”。

| 位置 | 文案 |
| --- | --- |
| 底部按钮 | 成就 |
| 面板标题 | 成就 |
| 进度文字 | 已留下 X / Y 个印记 |
| 空状态 | 还没有留下新的印记 |
| 未解锁占位 | 未留下的印记 |
| 弹窗前缀 | 新印记 |

## 面板整体布局

复用 `AchievementsPanel`，建议布局如下：

```text
AchievementsPanel
  Header
    TitleText            成长印记
    ProgressText         已留下 8 / 24 个印记
    CloseButton          复用 close.png
  FilterTabs
    All                  全部
    Ending               结局
    Choice               选择
    Mainline             主线
    Progress             累计
  Scroll View
    Viewport
      Content
        AchievementListItem
        AchievementListItem
        ...
  DetailHint             可选：选中某条时显示补充说明
```

如果不想第一版做筛选，`FilterTabs` 可以先不显示，但布局空间建议预留。

## 列表条目布局

建议把旧 `Achievement1Marry` 或 `Achievement2RuleYears5` 复制成一个新 prefab：

```text
Assets/UniversitySimulator/Prefabs/UI/AchievementListItem.prefab
```

单条高度建议 300 像素。当前成就面板的滚动区域约为 3000 x 1240，所以一屏会显示 3 到 4 个成就；不要再使用旧 Kings 的 86 到 96 像素小条目。结构：

```text
AchievementListItem
  BackgroundImage        panel_outline.png 或 panel.png
  IconFrame              circle.png，约 188x188
    Icon                 成就图标
    LockOverlay          未解锁时显示 unclear.png 或半透明遮罩
  TitleText              金钱的诅咒，字号约 52
  DescriptionText        金钱溢出的情况下结束一局游戏，字号约 36
  StatusText             已获得 / 未获得，字号约 30
  CheckmarkImage         checkmark.png
```

### 条目状态

| 状态 | 表现 |
| --- | --- |
| 已解锁 | 图标全亮，标题和描述正常显示，右侧显示 `已获得` 或勾选 |
| 未解锁但公开 | 图标半透明，标题正常显示，描述正常显示，右侧显示 `未获得` |
| 隐藏成就 | 图标用 `unclear.png`，标题显示 `未留下的印记`，描述显示 `继续生活看看` |
| 进度成就 | 右侧显示 `2/5`，底部可加一条细进度条 |

第一版建议大部分成就公开标题和描述，只隐藏真结局相关成就。因为本设计里的成就不强调挑战难度，公开能让玩家更容易理解系统反馈。

## 分类和图标

优先复用 `Assets/UniversitySimulator/Art/LegacyKingsUI`：

| 分类 | 图标建议 | 颜色倾向 |
| --- | --- | --- |
| 结局 | `achievements.png`、`crown.png`、`star.png` | 深色底，金色点缀 |
| 金钱 | `money_large.png`、`money_small.png`、`tl_money.png` | 金色 |
| 身心 | `health_icon.png`、`tl_heart.png` | 红色或粉色 |
| 学业 | `tl_idea.png`、`intelligence_icon.png` | 蓝白或亮黄 |
| 人际 | `people_large.png`、`people_small.png`、`tl_people.png` | 绿色或青色 |
| 主线 | `crown_lge.png`、`tl_crown.png` | 金色 |
| 选择 | `tl_event0.png`、`tl_arrow.png` | 中性色 |
| 未解锁 | `unclear.png` | 灰色 |
| 已完成 | `checkmark.png` | 亮色 |

不建议新增一批复杂图标。第一版用现有图标足够，后续再针对高价值成就补专属图。

## 弹窗布局

复用当前：

```text
GameCanvas/GamePanel/TopPanel/AchievementsPopupPanel
```

建议视觉层级：

```text
AchievementsPopupPanel
  Background              panel_top.png 或 panel.png
  Icon                    56x56
  PrefixText              新印记
  TitleText               金钱的诅咒
  DescriptionText         金钱溢出的情况下结束一局游戏。
```

弹窗行为：

| 行为 | 说明 |
| --- | --- |
| 出现位置 | 顶部居中，避免挡住卡牌正文 |
| 出现时机 | 成就解锁后立即入队 |
| 显示时长 | 2.4 到 3 秒 |
| 多个成就同时解锁 | 排队显示，不叠在一起 |
| 场景重载 | 写入 pending 队列，下次场景启动继续显示 |
| 菜单打开时 | 仍可显示，但不抢焦点 |

现有 `AchievementsScript` 已有字段：

- `achievementAnimator`
- `triggerOnAchievement`
- `anim_titleText`
- `anim_descriptionText`
- `anim_achievementImage`

短期可以给 `AchievementsScript` 增加公开方法来显示自定义成就。长期建议新增 `UniversityAchievementPopup`，由它持有这些 UI 引用。

## 面板刷新逻辑

建议新增 `UniversityAchievementListView` 挂在 `AchievementsPanel` 上：

| 字段 | 绑定对象 |
| --- | --- |
| `catalog` | `UniversityAchievementCatalog.asset` |
| `contentRoot` | `Scroll View/Viewport/Content` |
| `itemPrefab` | `AchievementListItem.prefab` |
| `progressText` | Header 中的新进度文本，或复用 `achievementProgressText` |
| `emptyState` | 可选 |

刷新时机：

1. `AchievementsPanel.OnEnable`
2. 成就解锁时
3. 筛选 tab 切换时
4. 语言或翻译刷新时，如果后续接入本地化

不要再依赖旧 `achievementStage.achievementGameobject` 一个个激活。新成就数量会很快超过旧场景里 5 个固定条目。

## 与底部菜单的关系

当前 `UniversitySettingsMenuController` 已经有：

```csharp
public GameObject achievementsPanel;
```

并在 `OpenSettingsPanel()` 中关闭成就面板：

```csharp
SetActive(achievementsPanel, false);
```

如果当前按钮还没有专门打开成就面板，建议补一个方法：

```csharp
public void OpenAchievementsPanel()
{
    SetActive(menuPanel, true);
    SetActive(settingsPanel, false);
    SetActive(exitPanel, false);
    SetActive(playerInfoPanel, false);
    SetActive(achievementsPanel, true);
    SetActive(questsPanel, false);
    SetActive(settingsSelectedIcon, false);
    CloseSlotPanel();
    SetCardMoveEnabled(false);
}
```

按钮事件接到 `AchievementsButton.onClick -> OpenAchievementsPanel()`。

## 尺寸建议

当前场景是横屏优先，菜单面板已有底部按钮栏。建议保守使用现有面板尺寸，不重做整体 Canvas。

### 横屏

| 元素 | 建议 |
| --- | --- |
| 面板边距 | 左右 40，上 32，下 140，避开底部菜单 |
| Header 高度 | 96 |
| 筛选栏高度 | 52 |
| 列表条目 | 高 300，宽度撑满 |
| 图标 | 188x188 |
| 标题字体 | 52 |
| 描述字体 | 36 |
| 状态字体 | 30 |
| 条目间距 | 24 |
| 列表边距 | 32 |

### 竖屏

如果后续启用竖屏或窄屏：

| 元素 | 建议 |
| --- | --- |
| 面板边距 | 左右 24，上 28，下 120 |
| 筛选栏 | 横向滚动或只保留“全部/结局/选择”三个 |
| 列表条目 | 高 300 或按屏幕高度缩到一屏 3 条 |
| 描述 | 最多两行，超出省略 |
| 右侧状态 | 移到标题下方，避免挤压描述 |

## 第一版落地顺序

1. 保留 `AchievementsPanel` 和 `AchievementsPopupPanel`。
2. 新建 `AchievementListItem.prefab`，不要继续复用 `Achievement1Marry` 这种旧命名。
3. 新增 `UniversityAchievementListView` 动态生成列表。
4. 新增或改造弹窗脚本，让它能显示任意成就标题、描述和图标。
5. 把底部 `AchievementsButton` 接到打开成就面板。
6. 替换旧面板中可见的英文占位文案，例如 `Achievement Title`、`Achievement Description`。
7. 用 8 个数值结局成就测试列表和弹窗。
8. 再接入单卡选择成就和筛选 tab。

## UI 验收清单

| 检查项 | 标准 |
| --- | --- |
| 打开成就面板 | 卡牌停止移动，菜单打开，成就列表可滚动 |
| 关闭或切回设置 | 卡牌移动状态按现有菜单逻辑恢复 |
| 已解锁成就 | 图标、标题、描述、状态正确 |
| 未解锁成就 | 不误显示已获得状态 |
| 成就弹窗 | 不遮挡卡牌主要文字，不连续重叠 |
| 多成就同时触发 | 逐条排队显示 |
| GameOver 重载 | 待显示弹窗不丢 |
| 横屏 | 文本不溢出条目 |
| 竖屏或窄屏 | 描述不挤压按钮和底部栏 |
| 旧 Kings 文案 | 不再出现 `marry`、`rule years`、`King`、`Queen` 这类旧主题可见文本 |
