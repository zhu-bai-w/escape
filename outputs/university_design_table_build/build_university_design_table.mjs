import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const projectRoot = "E:/escape";
const sourceCsvPath = `${projectRoot}/Assets/UniversitySimulator/Data/cards_v1_program.csv`;
const outputDir = `${projectRoot}/outputs/university_design_table`;
const outputPath = `${outputDir}/大学生模拟器_策划配表_v0.3.xlsx`;

const palette = {
  ink: "#1F2937",
  muted: "#6B7280",
  line: "#D1D5DB",
  dark: "#0F766E",
  blue: "#1D4ED8",
  amber: "#B45309",
  green: "#15803D",
  rose: "#BE123C",
  paleTeal: "#E6F4F1",
  paleBlue: "#EAF2FF",
  paleAmber: "#FFF7E6",
  paleGreen: "#ECFDF3",
  paleRose: "#FFF1F2",
  paleGray: "#F3F4F6",
  white: "#FFFFFF",
};

function parseCsv(text) {
  const rows = [];
  let row = [];
  let cell = "";
  let inQuotes = false;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    const next = text[i + 1];
    if (ch === '"') {
      if (inQuotes && next === '"') {
        cell += '"';
        i++;
      } else {
        inQuotes = !inQuotes;
      }
    } else if (ch === "," && !inQuotes) {
      row.push(cell);
      cell = "";
    } else if ((ch === "\n" || ch === "\r") && !inQuotes) {
      if (ch === "\r" && next === "\n") i++;
      row.push(cell);
      if (row.some((v) => v !== "")) rows.push(row);
      row = [];
      cell = "";
    } else {
      cell += ch;
    }
  }
  if (cell.length > 0 || row.length > 0) {
    row.push(cell);
    if (row.some((v) => v !== "")) rows.push(row);
  }
  const headers = rows[0];
  return rows.slice(1).map((values) => Object.fromEntries(headers.map((h, i) => [h, values[i] ?? ""])));
}

function colLetter(n) {
  let s = "";
  while (n > 0) {
    const m = (n - 1) % 26;
    s = String.fromCharCode(65 + m) + s;
    n = Math.floor((n - 1) / 26);
  }
  return s;
}

function rangeFor(row, col, rowCount, colCount) {
  const c1 = colLetter(col);
  const c2 = colLetter(col + colCount - 1);
  return `${c1}${row}:${c2}${row + rowCount - 1}`;
}

function setTitle(sheet, title, description, colCount) {
  sheet.showGridLines = false;
  const endCol = colLetter(Math.max(4, colCount));
  sheet.getRange(`A1:${endCol}1`).merge();
  sheet.getRange("A1").values = [[title]];
  sheet.getRange("A1").format = {
    fill: palette.dark,
    font: { bold: true, color: palette.white, size: 16 },
    horizontalAlignment: "left",
    verticalAlignment: "center",
  };
  sheet.getRange("A1").format.rowHeightPx = 36;
  sheet.getRange(`A2:${endCol}2`).merge();
  sheet.getRange("A2").values = [[description]];
  sheet.getRange("A2").format = {
    fill: palette.paleTeal,
    font: { color: palette.ink, size: 10 },
    wrapText: true,
    verticalAlignment: "top",
  };
  sheet.getRange("A2").format.rowHeightPx = 44;
}

function writeTable(sheet, startRow, headers, rows, options = {}) {
  const startCol = 1;
  const colCount = headers.length;
  sheet.getRange(rangeFor(startRow, startCol, 1, colCount)).values = [headers];
  sheet.getRange(rangeFor(startRow, startCol, 1, colCount)).format = {
    fill: options.headerFill ?? palette.ink,
    font: { bold: true, color: palette.white, size: 10 },
    horizontalAlignment: "center",
    verticalAlignment: "center",
    wrapText: true,
    borders: { preset: "all", style: "thin", color: palette.line },
  };
  if (rows.length > 0) {
    sheet.getRange(rangeFor(startRow + 1, startCol, rows.length, colCount)).values = rows;
    sheet.getRange(rangeFor(startRow + 1, startCol, rows.length, colCount)).format = {
      fill: options.bodyFill ?? palette.white,
      font: { color: palette.ink, size: 10 },
      verticalAlignment: "top",
      wrapText: true,
      borders: { preset: "inside", style: "thin", color: "#E5E7EB" },
    };
  }
  try {
    sheet.tables.add(rangeFor(startRow, startCol, Math.max(2, rows.length + 1), colCount), true, options.tableName);
  } catch {
    // Tables are convenience only; the workbook remains usable without them.
  }
  try {
    sheet.freezePanes.freezeRows(startRow);
  } catch {}
}

function setWidths(sheet, widths) {
  widths.forEach((width, idx) => {
    sheet.getRangeByIndexes(0, idx, 1, 1).format.columnWidthPx = width;
  });
}

function addValidation(sheet, range, values) {
  try {
    sheet.getRange(range).dataValidation = { rule: { type: "list", values } };
  } catch {}
}

function numberOrBlank(v) {
  if (v === undefined || v === null || String(v).trim() === "") return "";
  const n = Number(v);
  return Number.isFinite(n) ? n : v;
}

const sourceText = await fs.readFile(sourceCsvPath, "utf8");
const cards = parseCsv(sourceText);
const pools = [
  ["main", "主卡池", "常驻", "始终开启", 1, 1.0, 999, "", "普通日常卡池，保证永远有牌可抽。"],
  ["true_self", "真实自我", "剧情/倾向", "bodyMind<=45 OR selfChoice>=2", 2, 0.8, 1, "main", "当玩家开始偏向自我选择或身心偏低时出现。"],
  ["sickness_recovery", "生病恢复", "状态触发", "bodyMind<=40", 3, 1.2, 1, "main", "身心低时提高出现率，用于拉回或加剧身心线。"],
  ["story", "主线剧情", "主线", "按主线阶段或 flags 开启", 4, 1.0, 1, "main", "主线入口、转折、收束卡。"],
  ["exam_week", "考试周", "阶段/状态触发", "round>=15 OR academics<=40 OR academics>=70", 3, 1.2, 1, "main", "考试周或学业危险时出现。"],
  ["family_pressure", "家庭压力", "隐藏变量触发", "familyExpectation>=2 OR danger(academics) OR danger(economy)", 3, 1.1, 1, "main", "家庭期待、成绩或经济压力相关。"],
  ["low_economy", "低经济", "状态触发", "economy<=35", 3, 1.2, 1, "main", "经济过低时的兼职、借钱、消费压力事件。"],
  ["academic_pressure", "学业压力", "状态触发", "academics<=40 OR academics>=75", 3, 1.1, 1, "main", "学业低/高两端失衡都可触发。"],
  ["relationship_pressure", "人际压力", "状态触发", "relationships<=35 OR relationships>=70", 3, 1.1, 1, "main", "人际低或过载时出现。"],
  ["life_management", "生活管理", "状态触发", "bodyMind<=45 OR economy<=45", 2, 1.0, 1, "main", "作息、卫生、饮食、拖延相关。"],
  ["real_connection", "真实连接", "隐藏变量触发", "realConnection>=2", 2, 1.0, 1, "main", "真实连接积累后的关系深化事件。"],
];

const hiddenVars = [
  ["selfChoice", "自我选择", "玩家是否越来越按自己的意愿生活", 0, 10, 0, "", ">=6 可进入自我线后段", "主线、真实自我卡池"],
  ["familyExpectation", "家庭期待压力", "玩家是否被家庭目标牵引", 0, 10, 0, "", ">=5 开启家庭压力高段", "家庭压力卡池、结局前置"],
  ["avoidance", "逃避倾向", "玩家是否倾向拖延、回避冲突和责任", 0, 10, 0, "", ">=6 开启逃避相关卡", "低身心、学业崩盘线"],
  ["realConnection", "真实连接", "玩家是否建立了真实而非表面的人际连接", 0, 10, 0, "", ">=4 开启真实连接卡", "人际线、主线分支"],
  ["involution", "内卷倾向", "玩家是否把生活收缩成竞争和指标", 0, 10, 0, "", ">=6 开启绩点机器倾向", "学业高端失衡"],
  ["consumptionPressure", "消费压力", "玩家是否被消费、攀比或短期快感牵引", 0, 10, 0, "", ">=5 开启经济压力卡", "经济线、低经济卡池"],
];

const endings = [
  ["END_BODY_LOW", "透支崩溃", "bodyMind<=0", 100, "失衡结局", "身心", "你终于把所有事情都扛完了，只是再也没有力气走出宿舍。", "低身心"],
  ["END_BODY_HIGH", "安全壳", "bodyMind>=100", 90, "失衡结局", "身心", "你把自己照顾得很好，也把所有需要碰撞的事情挡在了门外。", "高身心"],
  ["END_ACADEMICS_LOW", "学业崩盘", "academics<=0", 100, "失衡结局", "学业", "课程、作业和考试像潮水退去，露出一张空白的成绩单。", "低学业"],
  ["END_ACADEMICS_HIGH", "绩点机器", "academics>=100 OR involution>=8", 90, "失衡结局", "学业", "你拥有完美的绩点，也失去了所有无法量化的下午。", "高学业/内卷"],
  ["END_RELATION_LOW", "社交孤岛", "relationships<=0", 100, "失衡结局", "人际", "消息列表安静得像从未有人来过，你也终于不用回复任何人。", "低人际"],
  ["END_RELATION_HIGH", "所有人的好人", "relationships>=100", 90, "失衡结局", "人际", "每个人都说你很好，只有你自己不知道还剩下什么。", "高人际"],
  ["END_ECONOMY_LOW", "生活失控", "economy<=0", 100, "失衡结局", "经济", "下一笔账单来的时候，你发现生活已经不是靠意志能解决的问题。", "低经济"],
  ["END_ECONOMY_HIGH", "被金钱吞没", "economy>=100 OR consumptionPressure>=8", 90, "失衡结局", "经济", "你终于不用担心钱了，也开始只用钱衡量所有事情。", "高经济/消费压力"],
];

const storyRows = [
  ["main_self", "真实自我主线", 1, "E005", "", "selfChoice+1", "selfChoice>=2", "玩家第一次在他人需要和自己计划之间摇摆。"],
  ["main_family", "家庭期待主线", 1, "", "", "familyExpectation+1", "familyExpectation>=2", "家庭目标逐渐进入玩家日常选择。"],
  ["main_connection", "真实连接主线", 1, "", "", "realConnection+1", "realConnection>=2", "从普通社交转向真正互相看见。"],
  ["main_pressure", "内卷压力主线", 1, "", "", "involution+1", "academics>=70 OR involution>=3", "当学业高压或内卷倾向累积时进入。"],
];

const cardHeaders = [
  "eventId", "cardName", "cardType", "storyArcId", "storyStage", "storyRole", "poolId",
  "titleText", "questionText", "answerLeft", "answerRight", "leftFeedback", "rightFeedback",
  "left_bodyMind", "left_academics", "left_relationships", "left_economy",
  "right_bodyMind", "right_academics", "right_relationships", "right_economy",
  "leftHiddenDelta", "rightHiddenDelta", "conditionExpression", "roundMin", "roundMax",
  "bodyMindMin", "bodyMindMax", "academicsMin", "academicsMax", "relationshipsMin", "relationshipsMax", "economyMin", "economyMax",
  "requiredFlags", "blockedFlags", "setFlagsLeft", "setFlagsRight", "nextLeftCardId", "nextRightCardId",
  "weight", "cooldown", "maxDraws", "unique", "isDrawable", "isHighPriority", "tags", "artPath", "designerNotes", "status", "originalRow",
];

const cardRows = cards.map((c) => {
  const cardType = c.eventType === "story" ? "story" : "normal";
  const status = [c.left_hiddenDelta, c.right_hiddenDelta].some((v) => v && v.trim())
    ? "可程序导入"
    : "待补隐藏变量/反馈";
  return [
    c.eventId,
    c.cardName,
    cardType,
    "",
    "",
    cardType === "story" ? "entry" : "none",
    c.poolId,
    c.titleText,
    c.questionText,
    c.answerLeft,
    c.answerRight,
    c.leftFeedback,
    c.rightFeedback,
    numberOrBlank(c.left_bodyMind),
    numberOrBlank(c.left_academics),
    numberOrBlank(c.left_relationships),
    numberOrBlank(c.left_economy),
    numberOrBlank(c.right_bodyMind),
    numberOrBlank(c.right_academics),
    numberOrBlank(c.right_relationships),
    numberOrBlank(c.right_economy),
    c.left_hiddenDelta,
    c.right_hiddenDelta,
    c.conditionExpression,
    numberOrBlank(c.condition_round_min),
    "",
    numberOrBlank(c.condition_bodyMind_min),
    numberOrBlank(c.condition_bodyMind_max),
    numberOrBlank(c.condition_academics_min),
    numberOrBlank(c.condition_academics_max),
    numberOrBlank(c.condition_relationships_min),
    numberOrBlank(c.condition_relationships_max),
    numberOrBlank(c.condition_economy_min),
    numberOrBlank(c.condition_economy_max),
    "",
    "",
    "",
    "",
    "",
    "",
    numberOrBlank(c.weight),
    numberOrBlank(c.cooldown),
    numberOrBlank(c.maxDraws),
    c.unique,
    c.isDrawable,
    c.isHighPriority,
    c.tags,
    c.artPath,
    c.designerNotes || c.triggerNotes,
    status,
    numberOrBlank(c.originalRow),
  ];
});

const fieldRows = [
  ["卡片主表", "eventId", "文本ID", "必填", "唯一事件编号，不要重复。", "E001"],
  ["卡片主表", "cardName", "Unity资源名", "必填", "生成 prefab 的资源名，程序可自动生成，不建议手改。", "US_E001"],
  ["卡片主表", "cardType", "枚举", "必填", "normal/story/branch/ending/tutorial/system。决定需要填写哪些逻辑字段。", "normal"],
  ["卡片主表", "storyArcId", "文本ID", "主线卡必填", "所属主线 ID，普通卡可空。", "main_self"],
  ["卡片主表", "storyStage", "整数", "主线卡建议填", "主线阶段，便于控制入口、转折、收束。", "1"],
  ["卡片主表", "storyRole", "枚举", "主线卡必填", "none/entry/beat/branch/follow_up/climax/ending_gate。", "entry"],
  ["卡片主表", "poolId", "枚举", "必填", "卡池 ID，必须能在卡池表找到。", "main"],
  ["卡片主表", "titleText", "文本", "必填", "卡片标题，建议 2-8 字。", "早八迟到"],
  ["卡片主表", "questionText", "文本", "必填", "卡面正文，建议 20-60 字。", "你睁开眼，距离早八上课还有十二分钟。"],
  ["卡片主表", "answerLeft/answerRight", "文本", "必填", "左右选项，短句，不写解释。", "打车冲过去"],
  ["卡片主表", "leftFeedback/rightFeedback", "文本", "建议填", "选择后给玩家的短反馈，后续可用于日志或过渡。", "你在车后座背完了最后两个单词。"],
  ["卡片主表", "left_* / right_*", "数字", "必填", "四轴数值变化，建议使用 -15/-10/-5/0/5/10/15。", "-10"],
  ["卡片主表", "leftHiddenDelta/rightHiddenDelta", "表达式", "主线/倾向卡必填", "隐藏变量变化，用 | 分隔。", "selfChoice+1|avoidance-1"],
  ["卡片主表", "conditionExpression", "表达式", "条件卡必填", "复杂抽卡条件，支持 AND/OR 和比较。", "bodyMind<=45 OR selfChoice>=2"],
  ["卡片主表", "requiredFlags/blockedFlags", "列表", "分支卡建议填", "必须已有/禁止已有的剧情标记，用 | 分隔。", "met_roommate|joined_club"],
  ["卡片主表", "setFlagsLeft/setFlagsRight", "列表", "主线卡建议填", "玩家选择后设置的剧情标记。", "helped_friend"],
  ["卡片主表", "nextLeftCardId/nextRightCardId", "eventId", "强剧情必填", "选择后指定后续卡；普通随机卡可空。", "E023"],
  ["卡片主表", "weight", "数字", "必填", "符合条件后的抽取权重。", "1.2"],
  ["卡片主表", "cooldown", "整数", "必填", "抽到后多少轮内不再出现。", "4"],
  ["卡片主表", "maxDraws", "整数", "必填", "每局最大出现次数；唯一卡填 1。", "100"],
  ["卡池表", "enabledCondition", "表达式", "条件卡池必填", "卡池开启条件。", "economy<=35"],
  ["主线表", "entryCardId", "eventId", "建议填", "主线入口卡。", "E005"],
  ["隐藏变量表", "hiddenVarId", "文本ID", "必填", "隐藏变量 ID，卡片隐藏变化和条件会引用它。", "selfChoice"],
  ["结局表", "triggerCondition", "表达式", "必填", "结局触发条件，优先级高的先判定。", "economy<=0"],
];

const typeRows = [
  ["normal", "普通随机卡", "title/question/answer/四轴数值/poolId/weight/cooldown/maxDraws", "conditionExpression/隐藏变量/反馈", "nextLeftCardId/nextRightCardId 通常不填", "主卡池日常事件"],
  ["story", "主线入口或阶段卡", "storyArcId/storyStage/storyRole/requiredFlags/setFlags/poolId", "nextLeftCardId/nextRightCardId/hiddenDelta", "不要只填自然语言触发备注", "朋友失恋、家庭来电"],
  ["branch", "分支承接卡", "requiredFlags/blockedFlags/nextLeftCardId/nextRightCardId/setFlags", "四轴数值/隐藏变量", "不能没有入口 flag，否则永远抽不到或乱入", "加入社团后的连续事件"],
  ["ending", "结局卡或结局前置", "endingId 或 ending_gate、triggerCondition、priority", "bodyText/artPath", "不要放进普通 main 池随机抽", "透支崩溃"],
  ["tutorial", "教学卡", "固定触发条件、简短文本、无强数值惩罚", "next card", "不要混入普通抽卡池", "第一张引导卡"],
  ["system", "系统/占位卡", "用途说明、isDrawable=false", "fallback 配置", "不要写剧情推进", "无卡可抽 fallback"],
];

const workbook = Workbook.create();
workbook.comments.setSelf({ displayName: "User" });

const overview = workbook.worksheets.add("README");
setTitle(overview, "大学生模拟器 策划配表 v0.3", "本工作簿用于策划填写卡片文本、左右选项、四轴数值、卡池、主线、隐藏变量和结局。程序导入时以卡片主表为主，其余表为条件、枚举和校验依据。", 8);
const overviewRows = [
  ["填写顺序", "先补卡片主表的反馈和隐藏变量，再补主线表/卡池表/结局表。"],
  ["当前底稿", `已从 cards_v1_program.csv 导入 ${cards.length} 张卡。`],
  ["关键规则", "普通卡不需要 nextCard；主线/分支卡才需要 requiredFlags、setFlags、nextLeftCardId。"],
  ["数值范围", "四轴建议保持 -15/-10/-5/0/5/10/15，避免一次选择改变过大。"],
  ["程序边界", "Kings 原生文本导入只管卡面文字；完整玩法导入需要读取这份新版策划配表。"],
];
writeTable(overview, 4, ["项目", "说明"], overviewRows, { tableName: "OverviewTable", headerFill: palette.dark, bodyFill: palette.paleTeal });
setWidths(overview, [160, 760]);

const cardSheet = workbook.worksheets.add("卡片主表");
setTitle(cardSheet, "卡片主表", "策划主要填写区域：每一行是一张卡。浅色字段可先空，程序字段建议按说明书填写，避免只写自然语言。", cardHeaders.length);
writeTable(cardSheet, 4, cardHeaders, cardRows, { tableName: "CardMainTable", headerFill: palette.ink });
setWidths(cardSheet, [80, 95, 90, 110, 80, 100, 110, 110, 300, 120, 120, 180, 180, 80, 80, 90, 80, 80, 80, 90, 80, 180, 180, 260, 75, 75, 85, 85, 85, 85, 95, 95, 85, 85, 180, 180, 180, 180, 110, 110, 70, 70, 75, 75, 80, 90, 150, 150, 220, 120, 80]);
addValidation(cardSheet, "C5:C200", ["normal", "story", "branch", "ending", "tutorial", "system"]);
addValidation(cardSheet, "F5:F200", ["none", "entry", "beat", "branch", "follow_up", "climax", "ending_gate"]);
addValidation(cardSheet, "G5:G200", pools.map((p) => p[0]));
addValidation(cardSheet, "X5:X200", ["", "bodyMind<=45 OR selfChoice>=2", "economy<=35", "round>=15", "academics<=40", "relationships>=65"]);
addValidation(cardSheet, "AR5:AR200", ["true", "false"]);
addValidation(cardSheet, "AS5:AS200", ["true", "false"]);
addValidation(cardSheet, "AT5:AT200", ["true", "false"]);

const poolSheet = workbook.worksheets.add("卡池表");
setTitle(poolSheet, "卡池表", "定义每个卡池什么时候开启、优先级和 fallback。程序抽卡会先判断卡池是否开启，再按权重抽卡。", 9);
writeTable(poolSheet, 4, ["poolId", "poolName", "poolType", "enabledCondition", "priority", "baseWeight", "maxCardsPerCycle", "fallbackPool", "notes"], pools, { tableName: "CardPoolsTable", headerFill: palette.blue, bodyFill: palette.paleBlue });
setWidths(poolSheet, [140, 140, 120, 300, 80, 90, 130, 120, 360]);

const storySheet = workbook.worksheets.add("主线表");
setTitle(storySheet, "主线表", "定义主线 arc、入口卡、阶段推进和阶段解锁条件。主线卡仍然在卡片主表填写正文和选项。", 8);
writeTable(storySheet, 4, ["storyArcId", "storyName", "stage", "entryCardId", "requiredFlags", "setFlagsOnEnter", "nextStageCondition", "notes"], storyRows, { tableName: "StoryArcsTable", headerFill: palette.amber, bodyFill: palette.paleAmber });
setWidths(storySheet, [150, 180, 80, 110, 220, 180, 260, 380]);

const hiddenSheet = workbook.worksheets.add("隐藏变量表");
setTitle(hiddenSheet, "隐藏变量表", "隐藏变量用于主线、卡池和结局条件，不直接展示给玩家。卡片主表的 hiddenDelta 和 conditionExpression 会引用这里的 ID。", 9);
writeTable(hiddenSheet, 4, ["hiddenVarId", "displayName", "meaning", "min", "max", "initial", "dangerLow", "dangerHigh", "usedFor"], hiddenVars, { tableName: "HiddenVarsTable", headerFill: palette.green, bodyFill: palette.paleGreen });
setWidths(hiddenSheet, [160, 130, 320, 60, 60, 70, 120, 220, 240]);

const endingSheet = workbook.worksheets.add("结局表");
setTitle(endingSheet, "结局表", "结局按 priority 从高到低判定。四轴 0/100 是硬结局，隐藏变量可以做特殊结局或结局变体。", 8);
writeTable(endingSheet, 4, ["endingId", "title", "triggerCondition", "priority", "endingType", "relatedValue", "bodyText", "notes"], endings, { tableName: "EndingsTable", headerFill: palette.rose, bodyFill: palette.paleRose });
setWidths(endingSheet, [160, 130, 280, 80, 120, 100, 420, 160]);

const fieldsSheet = workbook.worksheets.add("字段说明");
setTitle(fieldsSheet, "字段说明", "说明每个关键字段的类型、是否必填、填写方式和示例。策划修改字段名之前应先确认程序导入器是否也要同步修改。", 6);
writeTable(fieldsSheet, 4, ["所在表", "字段", "类型", "要求", "填写说明", "示例"], fieldRows, { tableName: "FieldGuideTable", headerFill: palette.ink, bodyFill: palette.paleGray });
setWidths(fieldsSheet, [120, 160, 110, 120, 460, 260]);

const typeSheet = workbook.worksheets.add("卡片类型说明");
setTitle(typeSheet, "卡片类型说明", "不同类型卡片需要填写不同字段。普通随机卡最简单；主线卡、分支卡和结局卡必须补条件或 flags。", 6);
writeTable(typeSheet, 4, ["cardType", "用途", "必须填写", "建议填写", "不要这样填", "例子"], typeRows, { tableName: "CardTypeGuideTable", headerFill: palette.amber, bodyFill: palette.paleAmber });
setWidths(typeSheet, [110, 150, 360, 300, 300, 180]);

const validationSheet = workbook.worksheets.add("校验清单");
setTitle(validationSheet, "校验清单", "导入前人工自查：程序也会做自动检查，但策划先按这张表过一遍，可以减少返工。", 4);
const checkRows = [
  ["唯一性", "eventId/cardName 是否重复", "每张卡必须唯一", "重复会覆盖 prefab 或导致主线跳错。"],
  ["卡池", "poolId 是否存在于卡池表", "所有卡都必须挂卡池", "不存在的卡池会让卡永远抽不到。"],
  ["普通卡", "是否误填 nextLeftCardId/nextRightCardId", "普通随机卡通常不填", "除非你明确要做 follow-up。"],
  ["主线卡", "storyArcId/storyRole/requiredFlags/setFlags 是否完整", "主线卡必须能进入也能退出", "避免主线断链。"],
  ["数值", "左右选项是否都有代价", "不要出现明显最优解", "四轴变化建议总量接近。"],
  ["隐藏变量", "主线倾向是否用 hiddenDelta 记录", "不要只写在备注里", "程序不能稳定读取自然语言备注。"],
  ["结局", "triggerCondition 和 priority 是否明确", "硬结局优先级高", "多结局同时触发时按 priority。"],
  ["文本", "标题、正文、选项是否短而具体", "标题 2-8 字；正文 20-60 字", "避免说明书式长文本。"],
];
writeTable(validationSheet, 4, ["检查项", "要检查什么", "通过标准", "风险"], checkRows, { tableName: "ValidationGuideTable", headerFill: palette.green, bodyFill: palette.paleGreen });
setWidths(validationSheet, [130, 260, 300, 360]);

const enumSheet = workbook.worksheets.add("枚举值");
setTitle(enumSheet, "枚举值", "下拉字段和程序枚举参考。新增枚举前先确认程序是否支持。", 4);
const enumRows = [
  ["cardType", "normal", "普通随机卡"],
  ["cardType", "story", "主线卡"],
  ["cardType", "branch", "分支承接卡"],
  ["cardType", "ending", "结局卡或结局前置"],
  ["cardType", "tutorial", "教学卡"],
  ["cardType", "system", "系统/占位卡"],
  ["storyRole", "none", "无主线作用"],
  ["storyRole", "entry", "主线入口"],
  ["storyRole", "beat", "主线过程节点"],
  ["storyRole", "branch", "主线分支节点"],
  ["storyRole", "follow_up", "被上一张卡指定出现"],
  ["storyRole", "climax", "主线高潮节点"],
  ["storyRole", "ending_gate", "结局前置"],
  ["valueAxis", "bodyMind", "身心"],
  ["valueAxis", "academics", "学业"],
  ["valueAxis", "relationships", "人际"],
  ["valueAxis", "economy", "经济"],
];
writeTable(enumSheet, 4, ["字段", "枚举值", "含义"], enumRows, { tableName: "EnumTable", headerFill: palette.blue, bodyFill: palette.paleBlue });
setWidths(enumSheet, [150, 160, 280]);

// Basic number formatting for the editable numeric ranges.
cardSheet.getRange("N5:U200").format.numberFormat = "0";
cardSheet.getRange("Y5:AH200").format.numberFormat = "0";
cardSheet.getRange("AO5:AQ200").format.numberFormat = "0.0";
cardSheet.getRange("AY5:AY200").format.numberFormat = "0";
poolSheet.getRange("E5:G40").format.numberFormat = "0.0";
hiddenSheet.getRange("D5:F40").format.numberFormat = "0";
endingSheet.getRange("D5:D40").format.numberFormat = "0";

await fs.mkdir(outputDir, { recursive: true });

// Compact verification artifacts.
await workbook.inspect({ kind: "sheet", include: "id,name", maxChars: 4000 });
await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 300 },
  summary: "formula error scan",
});

const previewSheets = ["README", "卡片主表", "卡池表", "主线表", "隐藏变量表", "结局表", "字段说明", "卡片类型说明", "校验清单", "枚举值"];
for (const sheetName of previewSheets) {
  const preview = await workbook.render({ sheetName, autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(`${outputDir}/${sheetName}.png`, new Uint8Array(await preview.arrayBuffer()));
}

const xlsx = await SpreadsheetFile.exportXlsx(workbook);
await xlsx.save(outputPath);
console.log(JSON.stringify({ outputPath, cardCount: cards.length, sheetCount: previewSheets.length }, null, 2));
