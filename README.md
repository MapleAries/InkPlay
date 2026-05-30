# InkPlay (墨戏)

[English](#english) | [中文](#中文)

---

<a name="english"></a>

## English

### AI-Powered Creative Writing Studio for Web Novels

InkPlay is a Windows native application that uses a multi-agent AI pipeline to automate web novel creation. It features a 9-agent system that collaboratively writes, reviews, and refines chapters with minimal human intervention.

### Key Features

- **One-Click Writing** — 9-agent pipeline automatically writes chapters: Context Analysis → Rule Compilation → Architecture Design → Writing → Proofreading → Quality Audit → Data Extraction
- **Batch Writing** — Write multiple chapters in sequence with automatic context updates
- **Smart Context** — Tiered context window: full content for recent chapters, summaries for older ones
- **Genre-Aware AI** — Specialized prompts for 7 genres: Fantasy, Urban, Mystery, Xianxia, Historical, Romance, Sci-Fi
- **Auto-Sync** — After writing, automatically updates outlines, characters, relationships, foreshadowing, and locations
- **Revision Loop** — Automatic quality audit with up to 3 revision rounds if issues are found
- **Character Management** — AI-assisted character creation with personality, backstory, and voice generation
- **Script Conversion** — Convert novel chapters to screenplay format
- **Video Generation** — AI video generation via Kling API
- **Multi-Model Support** — Claude, OpenAI, Qwen, and any OpenAI-compatible API
- **Version Control** — Automatic version snapshots on every edit
- **Local Storage** — Projects saved as browsable Markdown/JSON files

### Tech Stack

- **UI**: WinUI 3 + .NET 8
- **MVVM**: CommunityToolkit.Mvvm
- **Database**: LiteDB (embedded NoSQL)
- **AI**: HttpClient + SSE streaming
- **Architecture**: 9-agent pipeline with orchestrator

### Quick Start

#### Prerequisites

- Windows 10/11 (10.0.22621+)
- .NET 8 SDK
- Visual Studio 2022 or VS Code + C# Dev Kit

#### Install & Run

```bash
git clone https://github.com/MapleAries/InkPlay.git
cd InkPlay
dotnet restore
dotnet build
dotnet run --project src/InkPlay.App
```

#### Configure AI

1. Launch the app and go to **Settings**
2. Add a Text API key (Claude/OpenAI/Qwen) for writing features
3. Add a Video API key for video generation
4. Return to home, create a project and start writing

### Agent Pipeline

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Context   │ ──→ │ Screenwriter│ ──→ │  Architect  │
│   Agent     │     │    Agent    │     │    Agent    │
└─────────────┘     └─────────────┘     └─────────────┘
                                              │
                                              ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Reviser   │ ←── │  Auditor    │ ←── │   Writer    │
│    Agent    │     │    Agent    │     │    Agent    │
└─────────────┘     └─────────────┘     └─────────────┘
       │                  │
       │            ┌─────┴─────┐
       │            │  PASS?    │
       │            └─────┬─────┘
       │                  │ YES
       │                  ▼
       │            ┌─────────────┐
       └──────────→ │    Data     │ ──→ Auto-Sync
                    │    Agent    │     (outlines, characters, etc.)
                    └─────────────┘
```

### Project Structure

```
InkPlay/
├── src/
│   ├── InkPlay.Core/           # Domain models, interfaces, enums
│   ├── InkPlay.Services/       # Agents, AI providers, repositories
│   │   ├── Agents/             # 9 AI agents + Orchestrator
│   │   ├── Ai/Providers/       # Claude, OpenAI, Qwen, Kling
│   │   ├── Data/Repositories/  # LiteDB repositories
│   │   └── Export/             # Markdown export
│   └── InkPlay.App/            # WinUI 3 application
│       ├── ViewModels/         # MVVM view models
│       ├── Views/Pages/        # XAML pages
│       └── Converters/         # Value converters
└── tests/
    └── InkPlay.Services.Tests/
```

### License

MIT

---

<a name="中文"></a>

## 中文

### AI 驱动的网文创作工作室

墨戏是一款 Windows 原生应用，使用多智能体 AI 管线自动化网文创作。它拥有 9 个专业智能体，能够协作完成章节的写作、审核和润色，最大程度减少人工干预。

### 核心功能

- **一键写作** — 9 智能体管线自动写作：上下文分析 → 规则编译 → 架构设计 → 写作 → 校对 → 质量审计 → 数据提取
- **批量写作** — 连续写作多个章节，自动更新上下文
- **智能上下文** — 分层上下文窗口：最近章节全文、早期章节摘要、更早章节仅标题
- **类型感知** — 7 种小说类型的专属提示词：玄幻、都市、悬疑、仙侠、历史、言情、科幻
- **自动同步** — 写作完成后自动更新大纲、角色、关系、伏笔、地点
- **返工机制** — 自动质量审计，发现问题最多返工 3 轮
- **角色管理** — AI 辅助创建角色，支持性格、背景、音色生成
- **剧本转换** — 将小说章节转换为剧本格式
- **视频生成** — 通过 Kling API 生成 AI 视频
- **多模型支持** — Claude、OpenAI、通义千问及任何 OpenAI 兼容 API
- **版本控制** — 每次编辑自动创建版本快照
- **本地存储** — 项目以可浏览的 Markdown/JSON 文件保存

### 技术栈

- **UI**: WinUI 3 + .NET 8
- **MVVM**: CommunityToolkit.Mvvm
- **数据库**: LiteDB（嵌入式 NoSQL）
- **AI**: HttpClient + SSE 流式传输
- **架构**: 9 智能体管线 + 编排器

### 快速开始

#### 环境要求

- Windows 10/11 (10.0.22621+)
- .NET 8 SDK
- Visual Studio 2022 或 VS Code + C# Dev Kit

#### 安装运行

```bash
git clone https://github.com/MapleAries/InkPlay.git
cd InkPlay
dotnet restore
dotnet build
dotnet run --project src/InkPlay.App
```

#### 配置 AI

1. 启动应用，进入**设置**
2. 添加文本 API Key（Claude/OpenAI/通义千问）用于写作功能
3. 添加视频 API Key 用于视频生成
4. 返回首页，创建项目开始创作

### 智能体管线

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  上下文智能体 │ ──→ │   编剧智能体  │ ──→ │  架构师智能体 │
│  (分析上下文) │     │ (编译规则栈)  │     │ (设计章节骨架)│
└─────────────┘     └─────────────┘     └─────────────┘
                                              │
                                              ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  修订员智能体 │ ←── │  审计员智能体 │ ←── │  作家智能体   │
│ (修复问题)   │     │ (质量审计)   │     │ (生成正文)   │
└─────────────┘     └─────────────┘     └─────────────┘
       │                  │
       │            ┌─────┴─────┐
       │            │  是否通过？ │
       │            └─────┬─────┘
       │                  │ 通过
       │                  ▼
       │            ┌─────────────┐
       └──────────→ │ 数据智能体   │ ──→ 自动同步
                    │ (提取结构数据)│    (大纲、角色等)
                    └─────────────┘
```

### 项目结构

```
InkPlay/
├── src/
│   ├── InkPlay.Core/           # 领域模型、接口、枚举
│   ├── InkPlay.Services/       # 智能体、AI 提供者、仓储
│   │   ├── Agents/             # 9 个 AI 智能体 + 编排器
│   │   ├── Ai/Providers/       # Claude、OpenAI、通义千问、Kling
│   │   ├── Data/Repositories/  # LiteDB 仓储
│   │   └── Export/             # Markdown 导出
│   └── InkPlay.App/            # WinUI 3 应用
│       ├── ViewModels/         # MVVM 视图模型
│       ├── Views/Pages/        # XAML 页面
│       └── Converters/         # 值转换器
└── tests/
    └── InkPlay.Services.Tests/
```

### 开源协议

MIT
