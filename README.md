# MMMedical

Casualties Unknown — 医疗训练游戏模式模组

## 安装
1. 安装 [BepInEx](https://github.com/BepInEx/BepInEx) 到 Casualties Unknown Demo
2. 将 `MMMedical.dll` 放入 `BepInEx/plugins/`

## 功能
- 教程课程列表新增 **MMM** 选项，无需解锁
- 按 B 吠叫开始 → 随机创伤施加 → 地上生成5件随机医疗装备
- 偶数轮弹出**许愿面板**（像素风黑白UI），从23种装备中选1件，100%出现在本轮补给中
- 治疗完毕按 B 吠叫 → 100倍速校验5秒 → 评分
- 评分 >5 有50%概率难度 N+1
- 开局 INT=20，饥饿/口渴/精力自动维持

## 构建
```bash
dotnet build -c Release
```
需要 .NET SDK 9.0+ 和 `net472` 目标框架。
