# InkPlay (墨戏)

AI-powered creative writing tool for web novels — Windows native application

## Features

- **Outline Planning** — AI-assisted story outline generation, plot expansion, chapter planning
- **Character Design** — Character profiles, personality traits, background stories, AI-generated suggestions
- **Chapter Writing** — AI-assisted writing with continue, rewrite, polish, expand, summarize styles
- **Video Generation** — AI video generation from text prompts
- **Local File Storage** — Projects saved as browsable files (Markdown, JSON) in user-chosen directories
- **Multi-model Support** — Claude, OpenAI GPT, Qwen (Tongyi Qianwen), freely switchable
- **Inspiration to Outline** — Input creative ideas, AI expands them into complete story outlines

## Tech Stack

- WinUI 3 + .NET 8
- CommunityToolkit.Mvvm (MVVM)
- LiteDB (local index database)
- HttpClient + SSE (AI streaming)
- Local file system (project data storage)

## Quick Start

### Prerequisites

- Windows 10/11 (10.0.22621+)
- .NET 8 SDK
- Visual Studio 2022 (recommended) or VS Code + C# Dev Kit

### Install & Run

```bash
git clone <repo-url>
cd InkPlay
dotnet restore
dotnet build
dotnet run --project src/InkPlay.App
```

### Configure AI

1. Launch the app and go to **Settings**
2. Add a Text API key (Claude/OpenAI/Qwen) for writing features
3. Add a Video API key for video generation
4. Return to home, create a project and start writing

## Workflow

```
Home → Create Project (choose save directory, optional AI outline generation)
  ├── Outline Planning — View/edit story outline, AI-assisted expansion
  ├── Character Design — Create/manage characters with AI assistance
  ├── Chapter Writing — Write chapters with AI continue/rewrite/polish
  └── Video Generation — Generate videos from text prompts
```

## Project Structure

```
InkPlay/
├── src/
│   ├── InkPlay.Core/       # Domain models, interfaces
│   ├── InkPlay.Services/   # AI, data, file services
│   └── InkPlay.App/        # WinUI 3 application
└── tests/
    └── InkPlay.Services.Tests/
```

### Project File Structure

Each project is saved as a local directory:

```
ProjectTitle/
├── project.json          # Project metadata
├── 大纲/
│   └── 故事大纲.md        # Story outline (Markdown)
├── 章节/
│   ├── 第1章.md
│   └── 第2章.md
├── 角色/
│   ├── 角色名.json        # Character profile (JSON)
│   └── ...
└── 对话历史/
    └── xxx.json           # AI conversation history
```

## Development Phases

- [x] Phase 0: Project initialization
- [x] Phase 1: Core framework — Project management, outline, characters, AI integration
- [ ] Phase 2: Rich editor + multi-model support
- [ ] Phase 3: Advanced writing tools + export
- [ ] Phase 4: Relationship graph + world building

## License

MIT
