# AgentOps 360 — Architecture

## Overview

AgentOps 360 is an autonomous enterprise operations agent that transforms meetings, documents, and audio transcripts into structured execution plans.

The system is built around a multi-agent workflow that extracts decisions, tasks, risks, follow-up emails, confidence scores, and audit traces.

---

## High-Level Architecture

```txt
User
 |
 | Paste notes / Upload file / Upload audio
 v
React + TypeScript Frontend
 |
 | HTTP requests
 v
ASP.NET Core Web API
 |
 +--> ProjectsController
 |
 +--> UploadsController
 |       |
 |       +--> TXT extraction
 |       +--> PDF extraction with PdfPig
 |       +--> Audio transcription with Speechmatics
 |
 +--> AgentRunsController
         |
         v
     AgentOrchestrator
         |
         +--> GeminiService
         |
         +--> Fallback Engine
         |
         v
     SQL Server Persistence
```

---

## Frontend

The frontend is built with React, TypeScript, Tailwind CSS, Axios, and Lucide icons.

Main responsibilities:

- Project workspace
- Meeting notes input
- File upload
- Animated agent pipeline
- Display of structured AI results
- Analysis history
- Report export

---

## Backend

The backend is built with ASP.NET Core Web API and Entity Framework Core.

Main responsibilities:

- Project management
- Agent run creation
- Gemini integration
- Speechmatics integration
- PDF text extraction
- SQL Server persistence
- Structured API responses

---

## AI Workflow

AgentOps 360 uses a 5-step agent workflow:

1. Intake Agent
2. Reasoning Agent
3. Planning Agent
4. Communication Agent
5. Audit Agent

Each agent has a specialized responsibility and contributes to the final structured report.

---

## Data Flow

```txt
1. User submits meeting input
2. Backend validates project
3. AgentOrchestrator sends prompt to Gemini
4. Gemini returns structured JSON
5. Backend maps JSON to database entities
6. SQL Server stores the run
7. Frontend displays the dashboard
8. User can reopen or export the analysis
```

---

## Persistence Model

Main database entities:

- Project
- AgentRun
- AgentTask
- RiskItem
- GeneratedEmail

Each AgentRun belongs to one Project and contains the complete execution output.

---

## Reliability

AgentOps 360 includes a fallback engine.

If Gemini is unavailable, overloaded, or returns an error, the app switches to:

```txt
AI Mode: Fallback Engine
```

If Gemini succeeds, the app displays:

```txt
AI Mode: Gemini Live
```

This improves demo reliability and production resilience.

---

## External Services

### Gemini

Used for:

- Business reasoning
- Task extraction
- Risk detection
- Email generation
- Agent trace generation

### Speechmatics

Used for:

- Audio transcription
- Meeting recording processing
- Voice-to-execution workflow

### PdfPig

Used for:

- PDF text extraction
- Business document ingestion

---

## Why This Is Agentic

AgentOps 360 is not a chatbot. It performs a structured autonomous workflow:

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

The result is business-ready execution output, not only conversation.
