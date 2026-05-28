# InkPlay（墨戏）

AI 辅助网文创作工具 — Windows 原生应用

## 功能

- **大纲规划** — AI 辅助生成故事大纲、情节扩展、章节规划
- **角色设计** — 角色档案、性格特点、背景故事、AI 生成建议
- **章节写作** — AI 辅助写作，支持续写、重写、润色、扩写、缩写等风格
- **视频生成** — 基于文本提示词的 AI 视频生成
- **本地文件存储** — 项目保存为可浏览的文件（Markdown、JSON），用户可自选目录
- **多模型支持** — Claude、OpenAI GPT、通义千问，用户可自由切换
- **灵感转大纲** — 输入创作灵感，AI 自动扩展为完整故事大纲

## 技术栈

- WinUI 3 + .NET 8
- CommunityToolkit.Mvvm（MVVM 框架）
- LiteDB（本地索引数据库）
- HttpClient + SSE（AI 流式对话）
- 本地文件系统（项目数据存储）

## 快速开始

### 环境要求

- Windows 10/11（10.0.22621+）
- .NET 8 SDK
- Visual Studio 2022（推荐）或 VS Code + C# Dev Kit

### 安装与运行

```bash
git clone <repo-url>
cd InkPlay
dotnet restore
dotnet build
dotnet run --project src/InkPlay.App
```

### 配置 AI

1. 启动应用后进入「设置」页面
2. 添加文本生成 API Key（Claude/OpenAI/通义千问）用于写作功能
3. 添加视频生成 API Key 用于视频生成功能
4. 返回首页，创建项目开始创作

## 创作流程

```
首页 → 创建项目（选择保存目录，可选 AI 生成大纲）
  ├── 大纲规划 — 查看/编辑故事大纲，AI 辅助扩展
  ├── 角色设计 — 创建/管理角色，AI 辅助设计
  ├── 章节写作 — 写作章节，AI 续写/重写/润色
  └── 视频生成 — 基于文本提示词生成视频
```

## 项目结构

```
InkPlay/
├── src/
│   ├── InkPlay.Core/       # 领域模型、接口定义
│   ├── InkPlay.Services/   # AI、数据、文件服务
│   └── InkPlay.App/        # WinUI 3 应用
└── tests/
    └── InkPlay.Services.Tests/
```

### 项目文件结构

每个项目保存为本地目录，便于浏览和管理：

```
项目标题/
├── project.json          # 项目元数据
├── 大纲/
│   └── 故事大纲.md        # 大纲内容（Markdown 格式）
├── 章节/
│   ├── 第1章.md
│   └── 第2章.md
├── 角色/
│   ├── 角色名.json        # 角色档案（JSON 格式）
│   └── ...
└── 对话历史/
    └── xxx.json           # AI 对话记录
```

## 开发阶段

- [x] Phase 0: 项目初始化
- [x] Phase 1: 基础框架 — 项目管理、大纲规划、角色设计、AI 集成、本地文件存储
- [ ] Phase 2: 富编辑器 + 多模型支持
- [ ] Phase 3: 高级写作工具 + 导出功能
- [ ] Phase 4: 关系图谱 + 世界观设定

## 许可证

MIT
