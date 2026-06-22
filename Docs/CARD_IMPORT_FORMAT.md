# 卡片程序配表格式

本项目的正式程序配表位于：

- `Assets/UniversitySimulator/Data/cards_v1_program.csv`

同时生成一个 KingsImEx 兼容文本表：

- `Assets/UniversitySimulator/Data/cards_v1_kings_text_import.csv`

## 1. 主程序配表用途

`cards_v1_program.csv` 是后续 Unity 导入器或运行时读取器应该使用的完整数据源。它包含文本、左右选项、四轴数值、权重、冷却、唯一性、卡池、触发备注和原始策划行号。

它不是 KingsImEx 原生格式。KingsImEx 只能导入文本，不能导入数值变化、权重、冷却和触发条件。

## 2. 字段说明

| 字段 | 含义 |
| --- | --- |
| `eventId` | 事件唯一 ID，例如 `E001`。 |
| `cardName` | Unity 资源名，当前格式为 `US_E001`。 |
| `groupName` | 导入到 CardStack 时建议使用的组名。 |
| `styleName` | 卡片样式名，当前统一为 `cs_None`，后续可替换为大学主题样式。 |
| `eventType` | 程序类型：`normal` 或 `story`。 |
| `eventSubType` | 策划表原始类型，例如 `普通`、`剧情`、`新增-低经济`。 |
| `sourceType` | 策划表来源：`原有` 或 `新增`。 |
| `titleText` | 卡片标题。 |
| `questionText` | 卡面正文。 |
| `answerLeft` / `answerRight` | 左右选项文本。 |
| `leftFeedback` / `rightFeedback` | 玩家选择后的反馈文本；当前为空，因为策划表暂未提供反馈文案。 |
| `left_bodyMind` / `right_bodyMind` | 左/右选择对身心的变化。 |
| `left_academics` / `right_academics` | 左/右选择对学业的变化。 |
| `left_relationships` / `right_relationships` | 左/右选择对人际的变化。 |
| `left_economy` / `right_economy` | 左/右选择对经济的变化。 |
| `left_hiddenDelta` / `right_hiddenDelta` | 隐藏变量变化，当前为空。 |
| `weight` | 策划权重，保留 `0.8 / 1 / 1.2`。 |
| `cardProbability` | Kings EventScript 兼容概率，已限制到 `0..1`。 |
| `cooldown` | 抽到后冷却轮数。 |
| `maxDraws` | 最大抽取次数；唯一卡为 `1`，其他为 `100`。 |
| `unique` | 是否唯一，`true` 或 `false`。 |
| `isDrawable` | 是否可随机抽取，当前统一为 `true`。 |
| `isHighPriority` | 是否高优先级，当前统一为 `false`。 |
| `poolId` | 程序卡池 ID，例如 `main`、`low_economy`、`exam_week`。 |
| `conditionExpression` | 从触发建议中解析出的条件表达式，仅作导入器参考。 |
| `condition_*` | 简单条件的结构化列；复杂 OR 条件保留在 `conditionExpression`。 |
| `tags` | 标签，使用 `|` 分隔。 |
| `artPath` | 卡图路径，当前为空。 |
| `triggerNotes` | 策划原始触发/卡池建议。 |
| `designerNotes` | 策划备注。 |
| `leftSummary` / `rightSummary` | 策划表自动生成的数值变化摘要。 |
| `leftOffsetNote` / `rightOffsetNote` | 策划表偏移说明。 |
| `originalRow` | 原 Excel 的行号，方便回查。 |

## 3. KingsImEx 文本表用途

`cards_v1_kings_text_import.csv` 只包含：

```text
GroupName;CardName;StyleName;EventScript.titleText;EventScript.questionText;EventScript.answerLeft;EventScript.answerRight;EventScript.answerUp;EventScript.answerDown
```

它只能用于导入标题、正文、左右选项。不要把它当成完整玩法数据源。

## 4. 当前缺口

- 策划表还没有玩家可见的选择反馈，因此 `leftFeedback/rightFeedback` 为空。
- 隐藏变量变化还没有结构化填写，因此 `left_hiddenDelta/right_hiddenDelta` 为空。
- `triggerNotes` 中仍有自然语言规则，复杂条件需要后续人工确认或导入器特殊处理。
- `styleName` 当前使用 `cs_None`，后续制作大学主题 CardStyle 后可以批量替换。

