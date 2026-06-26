# 事件链卡片导入流程

这份流程用于后续重复导入“入口选择卡 + 后续叙事链”的卡片。固定入口是：

1. 编辑 `Assets/UniversitySimulator/Data/cards_v1_program.csv`
2. 在 Unity 执行 `University Simulator/Cards/Validate Program Card CSV`
3. 校验通过后执行 `University Simulator/Cards/Import Program Cards And Wire Scene`
4. 查看报告：
   - `Assets/UniversitySimulator/Data/cards_v1_program_validation.json`
   - `Assets/UniversitySimulator/Data/cards_v1_kings_import_report.json`

## 表格字段约定

普通卡继续按现有字段填写。事件链卡额外使用这些字段：

| 字段 | 用法 |
| --- | --- |
| `eventId` | 卡片唯一 ID，例如 `S100` |
| `cardName` | 固定写成 `US_` + `eventId`，例如 `US_S100` |
| `groupName` | 主线事件链写 `Mainline` |
| `poolId` | 主线事件链写 `mainline` |
| `isDrawable` | 事件链卡必须写 `false`，不要让普通卡池随机抽到 |
| `unique` / `maxDraws` | 建议 `unique=true`、`maxDraws=1` |
| `chainId` | 同一条事件链用同一个 ID，例如 `MAIN_SOCIAL` |
| `chainOrder` | 链内顺序，从 `1` 开始连续填写 |
| `nextLeftCardId` / `nextRightCardId` | 控制选项后接哪张卡 |
| `permanentFlagLeft` / `permanentFlagRight` | 真结局标记，只在入口真选项侧填写 |
| `isEndingChain` | 真结局卡链才写 `true` |

## 主线事件链规则

主线入口卡是 `chainOrder=1`：

- 真选项放右侧。
- `nextRightCardId` 指向第二张卡，例如 `S100 -> S101`。
- `nextLeftCardId` 留空，选错后直接回普通卡池。
- `permanentFlagRight` 写真结局标记，例如 `MAIN_01`。
- `permanentFlagLeft` 留空。
- 可以有数值变化。

后续叙事卡是 `chainOrder=2` 及以后：

- 左右选项可以相同。
- 所有数值变化保持 `0`。
- 不填写永久标记。
- 非最后一张：`nextLeftCardId` 和 `nextRightCardId` 都指向下一张。
- 最后一张：两个 next 字段都留空，链路结束后回普通卡池。

## 当前主线调度规则

导入菜单会自动把 `UniversityMainlineEventScheduler` 挂到场景里的 `CardStack` 上，并写入当前规则：

- 开局先保护 `10` 张普通卡，期间不会抽到主线入口。
- 之后初始概率 `15%`。
- 每次没抽到，概率增加 `15%`。
- 连续没抽到达到 `6` 次后必定出现。
- 抽到任意一个主线入口后，概率窗口清零，再等 `10` 张普通卡后重新开始计数。
- 每条主线入口在单局只提供一次机会；选错只能等下一局。
- 已经拿到对应永久标记的主线入口不会再被投放。

## 真结局规则

当前真结局默认只看四个永久标记：

- `MAIN_01`
- `MAIN_02`
- `MAIN_03`
- `MAIN_04`

如果后续要导入真结局卡链，需要在表中增加真结局起始卡，并将该卡设置为 `isEndingChain=true`。导入器会自动把第一张真结局链卡接到 `UniversityTrueEndingController.trueEndingStartCard`。

## 校验会拦截的问题

`Validate Program Card CSV` 会检查：

- 缺少必要字段。
- `eventId` 重复或为空。
- `cardName` 不符合 `US_事件ID` 规范。
- next 字段指向不存在的卡。
- 事件链缺少 `chainOrder=1`。
- 同一条链里 `chainOrder` 重复或不连续。
- 事件链卡 `isDrawable` 没有关闭。
- 主线入口左侧错误地接了后续卡。
- 主线入口右侧没有接下一张。
- 主线入口缺少右侧永久标记。
- 后续叙事卡存在数值变化、永久标记或错误 next 链接。
- 终点卡仍然接了下一张。

校验失败时不要执行导入，先修表。
