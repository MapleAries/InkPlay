# InkPlay

AI辅助写作与短剧创作工具 — Windows原生应用

## 功能

- **AI写作助手** — 智能续写、重写、润色、扩写、风格转换
- **短剧创作** — 剧本生成、分集大纲、场景编辑、对话生成
- **角色管理** — 角色档案、人物关系、性格设定
- **多模型支持** — Claude、OpenAI GPT、通义千问，用户可自由切换
- **导出** — Markdown、Word、PDF

## 技术栈

- WinUI 3 + .NET 8
- CommunityToolkit.Mvvm (MVVM)
- LiteDB (本地数据库)
- HttpClient + SSE (AI流式对话)

## 快速开始

### 环境要求

- Windows 10/11 (10.0.22621+)
- .NET 8 SDK
- Visual Studio 2022 (推荐) 或 VS Code + C# Dev Kit

### 安装与运行

```bash
git clone <repo-url>
cd InkPlay
dotnet restore
dotnet build
dotnet run --project src/InkPlay.App
```

### 配置AI

1. 启动应用后进入「设置」页面
2. 选择AI提供商（Claude/OpenAI/通义千问）
3. 填入API Key和Base URL
4. 返回首页，创建项目开始写作

## 项目结构

```
InkPlay/
├── src/
│   ├── InkPlay.Core/       # 领域模型、接口
│   ├── InkPlay.Services/   # AI、数据、导出服务
│   └── InkPlay.App/        # WinUI 3 应用
└── tests/
    └── InkPlay.Services.Tests/
```

## 开发阶段

- [x] Phase 0: 项目初始化
- [ ] Phase 1: 基础框架 — 项目管理、编辑器、AI集成
- [ ] Phase 2: 富编辑 + 多模型支持
- [ ] Phase 3: 剧本工坊 + 导出功能
- [ ] Phase 4: 关系图谱 + 世界观设定

## License

MIT
