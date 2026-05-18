# AgentOps 360 — Autonomous Enterprise Operations Agent

AgentOps 360 is a web-based enterprise AI agent that turns messy business meetings, documents, and audio transcripts into clear execution plans.

It extracts decisions, tasks, risks, owners, priorities, follow-up emails, audit traces, and exportable executive reports using a multi-agent workflow powered by Gemini, Speechmatics, React, ASP.NET Core, and SQL Server.

---

## 🚀 Hackathon

Built for the **AI Agent Olympics Hackathon — Milan AI Week 2026**.

AgentOps 360 addresses the challenge:

> Design and deploy autonomous AI agents that move beyond copilots into real decision-making systems that create measurable enterprise value.

---

## 🎯 Problem

Enterprise teams lose time after meetings because important information is scattered across notes, documents, calls, and follow-up messages.

Common problems:

- Decisions are not clearly documented
- Tasks are not assigned properly
- Risks are discovered too late
- Follow-up emails are written manually
- Managers lack an auditable execution history
- Meeting outcomes are hard to convert into action

---

## 💡 Solution

AgentOps 360 transforms unstructured business input into a structured execution dashboard.

The user can:

- Paste meeting notes
- Upload `.txt` files
- Upload PDFs with extractable text
- Upload audio files for transcription through Speechmatics
- Run an autonomous multi-agent analysis
- Review decisions, tasks, risks, and follow-up emails
- Reopen previous analyses from history
- Export an executive report

---

## 🧠 Multi-Agent Workflow

AgentOps 360 uses a 5-agent execution pipeline:

| Agent | Responsibility |
|---|---|
| Intake Agent | Reads, cleans, and normalizes the input |
| Reasoning Agent | Detects decisions, blockers, and business context |
| Planning Agent | Creates tasks, priorities, owners, and deadlines |
| Communication Agent | Generates professional follow-up emails |
| Audit Agent | Checks risks, confidence, and traceability |

The frontend displays this workflow as an animated live pipeline during analysis.

---

## ✨ Key Features

### Core AI Features

- Real Gemini-powered business analysis
- Structured JSON output
- Executive summary generation
- Decision extraction
- Task creation with owners and priorities
- Risk detection with mitigation suggestions
- Follow-up email generation
- Confidence score
- Transparent agent trace

### Enterprise Features

- Project workspace
- SQL Server persistence
- Analysis history
- Reopen previous runs
- Export executive report as Markdown
- AI mode badge: `Gemini Live` or `Fallback Engine`
- Fallback mode for demo reliability

### Upload Features

- `.txt` file upload
- PDF text extraction
- Audio upload endpoint
- Speechmatics service integration for audio transcription

---

## 🏗️ Architecture

```txt
React + TypeScript Frontend
        |
        v
ASP.NET Core Web API
        |
        +--> ProjectsController
        |
        +--> AgentRunsController
        |       |
        |       v
        |   AgentOrchestrator
        |       |
        |       +--> GeminiService
        |       +--> Fallback Engine
        |
        +--> UploadsController
                |
                +--> TXT extraction
                +--> PDF extraction
                +--> SpeechmaticsService

        |
        v
SQL Server Database
```

---

## 🧩 Tech Stack

### Frontend

- React
- TypeScript
- Tailwind CSS
- Axios
- Lucide React

### Backend

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Gemini API
- Speechmatics API
- PdfPig for PDF text extraction

### AI / APIs

- Gemini 2.5 Flash
- Speechmatics Batch Transcription API

---

## 📁 Project Structure

```txt
AgentOps360/
├── backend/
│   └── AgentOps360.Api/
│       ├── Controllers/
│       │   ├── AgentRunsController.cs
│       │   ├── ProjectsController.cs
│       │   └── UploadsController.cs
│       ├── Data/
│       │   └── AppDbContext.cs
│       ├── DTOs/
│       │   ├── AgentRunResultDto.cs
│       │   ├── CreateAgentRunRequest.cs
│       │   └── FileUploadResultDto.cs
│       ├── Models/
│       │   ├── AgentRun.cs
│       │   ├── AgentTask.cs
│       │   ├── GeneratedEmail.cs
│       │   ├── Project.cs
│       │   └── RiskItem.cs
│       ├── Services/
│       │   ├── AgentOrchestrator.cs
│       │   ├── GeminiService.cs
│       │   └── SpeechmaticsService.cs
│       ├── Program.cs
│       └── appsettings.json
│
├── frontend/
│   └── agentops360-web/
│       ├── src/
│       │   ├── App.tsx
│       │   ├── index.css
│       │   └── main.tsx
│       ├── package.json
│       └── tailwind.config.js
│
└── README.md
```

---

## ⚙️ Backend Setup

Go to the backend project:

```bash
cd backend/AgentOps360.Api
```

Restore packages:

```bash
dotnet restore
```

Apply database migrations:

```bash
dotnet ef database update
```

Run backend:

```bash
dotnet run
```

Backend runs on:

```txt
http://localhost:5134
```

Swagger:

```txt
http://localhost:5134/swagger
```

---

## 🎨 Frontend Setup

Go to the frontend project:

```bash
cd frontend/agentops360-web
```

Install packages:

```bash
npm install
```

Run frontend:

```bash
npm run dev
```

Frontend runs on:

```txt
http://localhost:5173
```

---

## 🔐 Environment Variables

For local development, configure these values in `appsettings.json`.

Before pushing to GitHub, remove real API keys from `appsettings.json`.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AgentOps360Db;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Gemini": {
    "ApiKey": "",
    "Model": "gemini-2.5-flash"
  },
  "Speechmatics": {
    "ApiKey": "",
    "BaseUrl": "https://eu1.asr.api.speechmatics.com/v2"
  },
  "AllowedHosts": "*"
}
```

Recommended production environment variables:

```txt
Gemini__ApiKey
Gemini__Model
Speechmatics__ApiKey
Speechmatics__BaseUrl
ConnectionStrings__DefaultConnection
```

---

## 🧪 Demo Scenarios

### SaaS Launch

```txt
We need to launch the SaaS beta in three weeks. The backend authentication API is not fully stable. Stripe payment still needs testing. Marketing needs to prepare the launch email. We also need someone from operations to own customer support before the beta starts.
```

### HR Onboarding

```txt
The HR team discussed onboarding 25 new employees next month. The laptop order is delayed, training materials are incomplete, and nobody has confirmed the mentor assignment list. Emma will contact the IT supplier tomorrow. The HR manager wants the onboarding plan finalized by Monday.
```

### Sales Recovery

```txt
The sales team discussed a drop in conversion rate after the new pricing page launch. Laura will analyze the funnel data tomorrow. The design team must prepare two new landing page variations. The CEO wants a recovery plan before Friday.
```

---

## 📤 Exported Report

AgentOps 360 can export a structured Markdown report containing:

- Project name
- Generated date
- AI mode
- Confidence score
- Executive summary
- Decisions
- Tasks
- Risks
- Follow-up email
- Agent trace

This allows managers to immediately share the result after the analysis.

---

## 🛡️ Fallback Engine

AgentOps 360 includes a fallback analysis engine.

If Gemini is temporarily unavailable or overloaded, the system switches from:

```txt
AI Mode: Gemini Live
```

to:

```txt
AI Mode: Fallback Engine
```

This keeps demos reliable and prevents failed user experiences.

---

## 🧠 Why It Is Agentic

AgentOps 360 is not a simple chatbot.

It performs a structured workflow:

```txt
Input ingestion
→ Reasoning
→ Planning
→ Risk analysis
→ Communication generation
→ Audit trace
→ Persistence
→ Export
```

The system coordinates multiple specialized responsibilities and produces business-ready outputs.

---

## 🏆 Hackathon Value

AgentOps 360 creates measurable enterprise value by reducing the time between meetings and execution.

Potential business impact:

- Faster follow-up after meetings
- Clearer task ownership
- Better risk visibility
- Less manual reporting
- More reliable execution history
- Improved manager productivity

---

## 🛣️ Roadmap

Planned improvements:

- Full Speechmatics production transcription flow
- DOCX extraction
- PDF OCR support for scanned documents
- Calendar integration
- Email sending integration
- Team collaboration
- Role-based access
- Vultr deployment
- PostgreSQL production database
- PDF executive report export

---

## 📜 License

MIT License.

---

## 👤 Author

Built by Farzad Bagheri.

Portfolio: https://farzadbagheri.fr  
GitHub: https://github.com/BAGHERIFarzad  
LinkedIn: https://www.linkedin.com/in/farzad-bagheri-0b333a67/
