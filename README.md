# SinmaiLegacyAchievementFrame

将 **SDEZ**（DX1.70）的上框达成率面板还原成旧版外观。

> 不保证双人模式下的效果，未经测试；因为没人陪我打 maimai

## 安装

1. 下载 [Release](https://github.com/s-yh-china/SinmaiLegacyAchievementFrame/releases) 中的产物或 [自行构建](#自行构建)。
2. 将 `SinmaiLegacyAchievementFrame.dll` 复制到游戏根目录的 `Mods` 文件夹。
3. 抚摸你的猫咪。

## 自行构建

### 前置要求

- .NET SDK 8.0 及以上
- .NET Framework 4.8
- 路边捡的 `Assembly-CSharp.dll`

### 依赖

将路边捡到的 `Assembly-CSharp.dll` 放置在 `Libs` 文件夹中

### 构建

```
dotnet build -c Release"
```

构建产物会输出到 `Build/` 目录：

```
Build/SinmaiLegacyAchievementFrame.dll
```

## 素材说明与侵权处理

本 Mod 内嵌了从游戏旧版本资源中提取的少量 UI 贴图（达成率底板、ACHIEVEMENT 标签、菱形衬底），仅用于把新版界面还原成旧版外观，不包含任何音频、曲谱、曲绘或游戏逻辑数据。

这些素材的著作权归 SEGA 及原权利人所有，本仓库不主张任何权利，也不以任何形式出售或授权这些素材。若权利人认为本仓库包含的内容侵犯了其权益，请通过本仓库的 Issues 与我们联系，并注明具体文件与权利依据。我们将在收到有效通知后立即删除相关素材或相关内容，无需任何附加条件。