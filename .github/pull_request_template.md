## 改动内容

- 

## 改动类型

- [ ] 文档
- [ ] 事件文本 / 策划数据
- [ ] C# 脚本
- [ ] Unity 场景 / Prefab / ScriptableObject
- [ ] 美术 / 音频 / 大文件
- [ ] 包依赖 / Git 配置

## 高风险项检查

- [ ] 未删除、重排 `valueDefinitions.cs` 里的旧 enum 值
- [ ] Unity 文件对应的 `.meta` 已一起提交
- [ ] 未直接破坏 `Assets/Kings/` 原资源包
- [ ] 如新增大文件，已确认 Git LFS 规则命中
- [ ] 如改 `Packages/manifest.json`，已说明是否需要 Unity 重新解析 lock
- [ ] 如改 CardStack / Prefab / 场景，已说明验证方式

## 验证

- [ ] 已在 Unity 打开项目，无新增 Console 编译错误
- [ ] 已 Play Mode 测试核心流程
- [ ] 未能验证的内容已在下方说明

未验证说明：

