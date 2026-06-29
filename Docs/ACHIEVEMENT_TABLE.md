# 成就表格

日期：2026-06-29

## 设计依据

本表基于当前 Unity 项目和 Kings 迁移资源检查结果制定：

- 当前场景：`Assets/UniversitySimulator/Scenes/Game.unity`
- 当前卡表：`Assets/UniversitySimulator/Data/cards_v1_program.csv`
- 当前卡牌结构：46 张日常卡、16 张主线卡、8 张数值结局卡、1 张真结局卡
- 已存在成就组件：`AchievementsScript`、`addAchievement`
- 已存在成就入口：`MenuCanvas/MenuPanel/AchievementsPanel`
- 已存在弹窗：`GameCanvas/GamePanel/TopPanel/AchievementsPopupPanel`
- 可复用 Kings 图标：`Assets/UniversitySimulator/Art/LegacyKingsUI`

成就不要求很难，也不要求都很有意义。它主要用于回应玩家做过的选择、走向过的结局、反复形成的倾向。

## 字段说明

| 字段 | 说明 |
| --- | --- |
| ID | 建议使用的稳定成就 ID，写入存档时不要再改 |
| 名称 | 面板和弹窗显示的标题 |
| 描述 | 面板中显示的描述 |
| 分类 | 用于面板筛选和图标颜色 |
| 触发事件 | 成就实际监听的事件 |
| 实现条件 | 工程侧可直接判断的条件 |
| 图标建议 | 优先复用 Kings 迁移资源 |

## 第一批推荐成就

| ID | 名称 | 描述 | 分类 | 触发事件 | 实现条件 | 图标建议 |
| --- | --- | --- | --- | --- | --- | --- |
| ACH_END_ECON_HIGH | 金钱的诅咒 | 金钱溢出的情况下结束一局游戏。 | 结局 | 数值结局 | `endingId == "END07"` 或 `valueType == economy && triggerOnMaximum` | `money_large.png` |
| ACH_END_ECON_LOW | 月底黑洞 | 经济跌穿的情况下结束一局游戏。 | 结局 | 数值结局 | `endingId == "END08"` 或 `valueType == economy && !triggerOnMaximum` | `money_small.png` |
| ACH_END_BODY_HIGH | 自律的牢笼 | 身心过度饱和的情况下结束一局游戏。 | 结局 | 数值结局 | `endingId == "END01"` 或 `valueType == bodyMind && triggerOnMaximum` | `health_icon.png` |
| ACH_END_BODY_LOW | 医务室常驻 | 身心跌穿的情况下结束一局游戏。 | 结局 | 数值结局 | `endingId == "END02"` 或 `valueType == bodyMind && !triggerOnMaximum` | `tl_heart.png` |
| ACH_END_ACAD_HIGH | 优绩主义 | 学业溢出的情况下结束一局游戏。 | 结局 | 数值结局 | `endingId == "END03"` 或 `valueType == academics && triggerOnMaximum` | `tl_idea.png` |
| ACH_END_ACAD_LOW | 退学申请 | 学业跌穿的情况下结束一局游戏。 | 结局 | 数值结局 | `endingId == "END04"` 或 `valueType == academics && !triggerOnMaximum` | `intelligence_icon.png` |
| ACH_END_REL_HIGH | 人形 AI | 人际溢出的情况下结束一局游戏。 | 结局 | 数值结局 | `endingId == "END05"` 或 `valueType == relationships && triggerOnMaximum` | `people_large.png` |
| ACH_END_REL_LOW | 透明校友 | 人际跌穿的情况下结束一局游戏。 | 结局 | 数值结局 | `endingId == "END06"` 或 `valueType == relationships && !triggerOnMaximum` | `people_small.png` |
| ACH_MAIN_SOCIAL | 网线另一端 | 完成一次真实连接。 | 主线 | 主线选择 | `UniversityTrueEndingProgress.GetPermanentFlag("MAIN_01")` | `tl_people.png` |
| ACH_MAIN_ACADEMICS | 写下自己的故事 | 完成一次属于自己的结课作业。 | 主线 | 主线选择 | `UniversityTrueEndingProgress.GetPermanentFlag("MAIN_02")` | `tl_idea.png` |
| ACH_MAIN_BODY | 承认疲惫 | 完成一次身心主线。 | 主线 | 主线选择 | `UniversityTrueEndingProgress.GetPermanentFlag("MAIN_03")` | `tl_heart.png` |
| ACH_MAIN_ECON | 那张票 | 完成一次和钱有关的选择。 | 主线 | 主线选择 | `UniversityTrueEndingProgress.GetPermanentFlag("MAIN_04")` | `tl_money.png` |
| ACH_MAIN_ALL | 四线交汇 | 四条主线都留下了印记。 | 主线 | 主线汇总 | `MAIN_01` 到 `MAIN_04` 均为 true | `star.png` |
| ACH_TRUE_END | 人生是旷野 | 触发真结局。 | 主线 | 真结局 | `UniversityTrueEndingProgress.HasTriggeredTrueEnding` | `crown_lge.png` |
| ACH_DAYS_05 | 走过五天 | 在一局中走过五天。 | 累计 | 本局天数 | `CurrentRunDays >= 5` | `tl_event0.png` |
| ACH_DAYS_10 | 又来十天 | 在一局中走过十天。 | 累计 | 本局天数 | `CurrentRunDays >= 10` | `tl_event0.png` |
| ACH_DAYS_20 | 熬过二十天 | 在一局中走过二十天。 | 累计 | 本局天数 | `CurrentRunDays >= 20` | `tl_crown.png` |
| ACH_DAYS_30 | 三十天大学生 | 在一局中走过三十天。 | 累计 | 本局天数 | `CurrentRunDays >= 30` | `crown.png` |
| ACH_OVER_01 | 第一次结束 | 结束一局游戏。 | 累计 | GameOver | `GameOverCount >= 1` | `achievements.png` |
| ACH_OVER_03 | 反复横跳 | 累计结束三局游戏。 | 累计 | GameOver | `GameOverCount >= 3` | `star.png` |
| ACH_CHOICE_E001_L | 今晚五排缺一 | 在“室友开黑”里去打一会儿。 | 选择 | 单卡选择 | `eventId == "E001" && direction == left` | `tl_event0.png` |
| ACH_CHOICE_E001_R | 单词守夜人 | 在“室友开黑”里坚持学习。 | 选择 | 单卡选择 | `eventId == "E001" && direction == right` | `tl_idea.png` |
| ACH_CHOICE_E005_L | 操场夜灯 | 在“朋友失恋”里去陪朋友。 | 选择 | 单卡选择 | `eventId == "E005" && direction == left` | `tl_heart.png` |
| ACH_CHOICE_E006_L | 打工人上号 | 在“兼职邀请”里接下兼职。 | 选择 | 单卡选择 | `eventId == "E006" && direction == left` | `tl_money.png` |
| ACH_CHOICE_E010_R | 文件没了我也没了 | 在“电脑崩溃”里直接休息。 | 选择 | 单卡选择 | `eventId == "E010" && direction == right` | `health_icon.png` |
| ACH_CHOICE_E011_L | 随缘体测 | 在“体测通知”里选择不管了随缘。 | 选择 | 单卡选择 | `eventId == "E011" && direction == left` | `tl_heart.png` |
| ACH_CHOICE_E012_R | 创意版受害者 | 在“课堂展示”里做创意版。 | 选择 | 单卡选择 | `eventId == "E012" && direction == right` | `tl_idea.png` |
| ACH_CHOICE_E013_L | 直接沟通 | 在“室友作息”里直接沟通。 | 选择 | 单卡选择 | `eventId == "E013" && direction == left` | `tl_people.png` |
| ACH_CHOICE_E018_L | 借钱如流水 | 在“朋友借钱”里借给对方。 | 选择 | 单卡选择 | `eventId == "E018" && direction == left` | `money_small.png` |
| ACH_CHOICE_E020_R | 硬撑不是铁人 | 在“感冒前兆”里继续硬撑。 | 选择 | 单卡选择 | `eventId == "E020" && direction == right` | `health_icon.png` |
| ACH_CHOICE_E024_R | 人是铁饭是钢 | 在“外卖超时”里选择先吃饭。 | 选择 | 单卡选择 | `eventId == "E024" && direction == right` | `tl_money.png` |
| ACH_CHOICE_E038_L | 降重服务体验者 | 在“论文查重”里花钱降重。 | 选择 | 单卡选择 | `eventId == "E038" && direction == left` | `tl_idea.png` |
| ACH_CHOICE_E049_R | 听演出是对的 | 在“演唱会之夜”里去听演出。 | 选择 | 单卡选择 | `eventId == "E049" && direction == right` | `tl_excited.png` |
| ACH_CHOICE_E052_L | 今天必须上分 | 在“说实话没办法”里选择今天必须上分。 | 选择 | 单卡选择 | `eventId == "E052" && direction == left` | `tl_event0.png` |
| ACH_CHOICE_E056_L | 我逃课 | 在“我逃课”里真的逃课。 | 选择 | 单卡选择 | `eventId == "E056" && direction == left` | `tl_arrow.png` |
| ACH_CHOICE_E057_R | 妈妈我好想你 | 在“我哭泣”里说出想家。 | 选择 | 单卡选择 | `eventId == "E057" && direction == right` | `tl_heart.png` |
| ACH_CHOICE_E059_L | 我购买 | 在“我装扮”里选择购买。 | 选择 | 单卡选择 | `eventId == "E059" && direction == left` | `money_large.png` |
| ACH_CHOICE_E063_R | 算了我穷 | 在“同学聚餐”里因为穷而不去。 | 选择 | 单卡选择 | `eventId == "E063" && direction == right` | `money_small.png` |
| ACH_CHOICE_E069_R | 装作没听见 | 在“作弊诱惑”里装作没听见。 | 选择 | 单卡选择 | `eventId == "E069" && direction == right` | `checkmark.png` |
| ACH_CHOICE_E084_L | 周末补觉 | 在“周末补觉”里继续补觉。 | 选择 | 单卡选择 | `eventId == "E084" && direction == left` | `health_icon.png` |
| ACH_TEND_SOCIAL_05 | 社交雷达 | 一局内做出 5 次明显偏向人际的选择。 | 倾向 | 选择累计 | `run.relationshipPositiveChoiceCount >= 5` | `people_small.png` |
| ACH_TEND_STUDY_05 | 绩点雷达 | 一局内做出 5 次明显偏向学业的选择。 | 倾向 | 选择累计 | `run.academicsPositiveChoiceCount >= 5` | `tl_idea.png` |
| ACH_TEND_BODY_05 | 身体会记账 | 一局内做出 5 次明显照顾身心的选择。 | 倾向 | 选择累计 | `run.bodyMindPositiveChoiceCount >= 5` | `health_icon.png` |
| ACH_TEND_ECON_05 | 搞钱雷达 | 一局内做出 5 次明显提升经济的选择。 | 倾向 | 选择累计 | `run.economyPositiveChoiceCount >= 5` | `money_small.png` |
| ACH_TEND_SELF_03 | 真实自我三连 | 一局内选择 3 张带“真实自我”标签的卡。 | 倾向 | 标签累计 | `run.tagCount["真实自我"] >= 3` | `star.png` |
| ACH_TEND_EXAM_03 | 考试周幸存者 | 一局内经历 3 张考试周相关卡。 | 倾向 | 标签累计 | `run.tagCount["考试周"] >= 3` | `tl_idea.png` |
| ACH_TEND_DORM_03 | 宿舍生物 | 一局内经历 3 张宿舍相关卡。 | 倾向 | 标签累计 | `run.tagCount["宿舍"] >= 3` | `tl_castle.png` |
| ACH_TEND_HOME_02 | 家庭热线 | 一局内经历 2 张家庭相关卡。 | 倾向 | 标签累计 | `run.tagCount["家庭"] >= 2` | `tl_heart.png` |
| ACH_TEND_REL_NEG_03 | 已读不回练习生 | 一局内连续 3 次选择使人际下降。 | 倾向 | 连续选择 | `run.relationshipNegativeStreak >= 3` | `unclear.png` |
| ACH_TEND_MONEY_SPEND_03 | 花钱买心情 | 一局内 3 次选择使经济下降但身心或人际上升。 | 倾向 | 组合选择 | `economyDelta < 0 && (bodyMindDelta > 0 || relationshipsDelta > 0)` 累计 3 次 | `tl_money.png` |

## 推荐优先级

第一批实际落地建议先做 24 个：

1. 8 个数值结局成就。
2. 6 个主线和真结局成就。
3. 4 个天数和 GameOver 累计成就。
4. 6 个最有记忆点的单卡选择成就：`E001_L`、`E005_L`、`E006_L`、`E049_R`、`E057_R`、`E084_L`。

第二批再做倾向型成就。倾向型更依赖选择记录和标签统计，工程成本略高，但能更好体现“玩家这一局到底偏向什么”。
