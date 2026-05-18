import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import {
  Activity,
  AlertTriangle,
  ArrowRight,
  Brain,
  CheckCircle2,
  CircleCheck,
  ClipboardList,
  Copy,
  Download,
  FileText,
  Gauge,
  Loader2,
  Mail,
  Plus,
  Rocket,
  ShieldCheck,
  Sparkles,
  Users,
  UploadCloud,
  Zap,
} from "lucide-react";

type Project = {
  id: number;
  name: string;
  description?: string;
  createdAt: string;
};

type AgentTask = {
  id: number;
  title: string;
  owner?: string;
  priority: string;
  status: string;
  dueDate?: string;
  sourceEvidence: string;
};

type RiskItem = {
  id: number;
  title: string;
  severity: string;
  mitigation: string;
  sourceEvidence: string;
};

type GeneratedEmail = {
  id: number;
  subject: string;
  body: string;
  tone: string;
};

type AgentRunResult = {
  id: number;
  projectId: number;
  inputType: string;
  originalInput: string;
  transcript: string;
  executiveSummary: string;
  decisionsJson: string;
  agentTraceJson: string;
  confidenceScore: number;
  aiMode: string;
  createdAt: string;
  tasks: AgentTask[];
  risks: RiskItem[];
  generatedEmails: GeneratedEmail[];
};

type Decision = {
  title: string;
  evidence: string;
};

type TraceStep = {
  agent: string;
  step: string;
  status: string;
};

type FileUploadResult = {
  fileName: string;
  contentType: string;
  size: number;
  extractedText: string;
};

const API_URL = "http://localhost:5134/api";

const samplePrompts = [
  {
    label: "SaaS launch",
    value:
      "We need to launch the SaaS beta in three weeks. The backend authentication API is not fully stable. Stripe payment still needs testing. Marketing needs to prepare the launch email. We also need someone from operations to own customer support before the beta starts.",
  },
  {
    label: "HR onboarding",
    value:
      "The HR team discussed onboarding 25 new employees next month. The laptop order is delayed, training materials are incomplete, and nobody has confirmed the mentor assignment list. Emma will contact the IT supplier tomorrow. The HR manager wants the onboarding plan finalized by Monday.",
  },
  {
    label: "Sales recovery",
    value:
      "The sales team discussed a drop in conversion rate after the new pricing page launch. Laura will analyze the funnel data tomorrow. The design team must prepare two new landing page variations. The CEO wants a recovery plan before Friday.",
  },
];

const agentPipeline = [
  {
    name: "Intake Agent",
    description: "Reads and cleans meeting input",
  },
  {
    name: "Reasoning Agent",
    description: "Detects decisions and blockers",
  },
  {
    name: "Planning Agent",
    description: "Creates tasks and priorities",
  },
  {
    name: "Communication Agent",
    description: "Drafts the follow-up email",
  },
  {
    name: "Audit Agent",
    description: "Checks risks and confidence",
  },
];

function App() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState<number | null>(null);

  const [activeAgentIndex, setActiveAgentIndex] = useState(-1);
  const [completedAgents, setCompletedAgents] = useState<number[]>([]);

  const [runHistory, setRunHistory] = useState<AgentRunResult[]>([]);
  const [loadingHistory, setLoadingHistory] = useState(false);

  const [name, setName] = useState("Demo SaaS Launch");
  const [description, setDescription] = useState(
    "Autonomous meeting-to-action workflow for the hackathon demo."
  );

  const [meetingNotes, setMeetingNotes] = useState(samplePrompts[0].value);

  const [agentResult, setAgentResult] = useState<AgentRunResult | null>(null);
  const [loadingProjects, setLoadingProjects] = useState(false);
  const [runningAgent, setRunningAgent] = useState(false);
  const [error, setError] = useState("");
  const [copiedEmail, setCopiedEmail] = useState(false);

  const handleFileUpload = async (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const file = event.target.files?.[0];

    if (!file) return;

    const fileName = file.name.toLowerCase();

    const isText = fileName.endsWith(".txt");
    const isPdf = fileName.endsWith(".pdf");

    const isAudio =
      fileName.endsWith(".mp3") ||
      fileName.endsWith(".wav") ||
      fileName.endsWith(".m4a") ||
      fileName.endsWith(".mp4") ||
      fileName.endsWith(".webm") ||
      fileName.endsWith(".ogg") ||
      fileName.endsWith(".flac");

    if (!isText && !isPdf && !isAudio) {
      setError("Please upload a .txt, .pdf, or supported audio file.");
      return;
    }

    try {
      setError("");

      const formData = new FormData();
      formData.append("file", file);

      let endpoint = `${API_URL}/uploads/text`;

      if (isPdf) {
        endpoint = `${API_URL}/uploads/pdf`;
      }

      if (isAudio) {
        endpoint = `${API_URL}/uploads/audio`;
      }

      const response = await axios.post<FileUploadResult>(
        endpoint,
        formData,
        {
          headers: {
            "Content-Type": "multipart/form-data",
          },
        }
      );

      if (!response.data.extractedText.trim()) {
        setError("The uploaded file is empty or no readable content was found.");
        return;
      }

      setMeetingNotes(response.data.extractedText);
    } catch (err) {
      console.error("Error uploading file:", err);
      setError("Could not upload or extract content from the file.");
    } finally {
      event.target.value = "";
    }
  };

  const exportExecutiveReport = () => {
    if (!agentResult) return;

    const currentDecisions = parseDecisions();
    const currentTrace = parseTrace();
    const email = agentResult.generatedEmails[0];

    const report = `# AgentOps 360 — Executive Report

  ## Project
  ${selectedProject?.name || "Unknown project"}

  ## Generated At
  ${new Date(agentResult.createdAt).toLocaleString()}

  ## AI Mode
  ${agentResult.aiMode}

  ## Confidence Score
  ${agentResult.confidenceScore}%

  ---

  ## Executive Summary

  ${agentResult.executiveSummary}

  ---

  ## Decisions

  ${
    currentDecisions.length > 0
      ? currentDecisions
          .map(
            (decision, index) =>
              `### ${index + 1}. ${decision.title}

  Evidence: ${decision.evidence}`
          )
          .join("\n\n")
      : "No decisions detected."
  }

  ---

  ## Action Plan

  ${
    agentResult.tasks.length > 0
      ? agentResult.tasks
          .map(
            (task, index) =>
              `### ${index + 1}. ${task.title}

  Owner: ${task.owner || "Not assigned"}  
  Priority: ${task.priority}  
  Status: ${task.status}  
  Due date: ${task.dueDate ? new Date(task.dueDate).toLocaleDateString() : "Not specified"}  

  Evidence: ${task.sourceEvidence}`
          )
          .join("\n\n")
      : "No tasks detected."
  }

  ---

  ## Risk Matrix

  ${
    agentResult.risks.length > 0
      ? agentResult.risks
          .map(
            (risk, index) =>
              `### ${index + 1}. ${risk.title}

  Severity: ${risk.severity}  
  Mitigation: ${risk.mitigation}  

  Evidence: ${risk.sourceEvidence}`
          )
          .join("\n\n")
      : "No risks detected."
  }

  ---

  ## Generated Follow-up Email

  ${
    email
      ? `Subject: ${email.subject}

  ${email.body}`
      : "No email generated."
  }

  ---

  ## Agent Trace

  ${
    currentTrace.length > 0
      ? currentTrace
          .map(
            (traceStep, index) =>
              `### ${index + 1}. ${traceStep.agent}

  Status: ${traceStep.status}  
  Step: ${traceStep.step}`
          )
          .join("\n\n")
      : "No agent trace available."
  }

  ---

  Generated by AgentOps 360
  `;

    const blob = new Blob([report], {
      type: "text/markdown;charset=utf-8",
    });

    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");

    const safeProjectName = (selectedProject?.name || "agentops-report")
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/(^-|-$)/g, "");

    link.href = url;
    link.download = `${safeProjectName}-executive-report.md`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const selectedProject = useMemo(
    () => projects.find((project) => project.id === selectedProjectId),
    [projects, selectedProjectId]
  );

  const loadRunHistory = async (projectId: number) => {
    try {
      setLoadingHistory(true);

      const response = await axios.get<AgentRunResult[]>(
        `${API_URL}/agentruns/project/${projectId}`
      );

      setRunHistory(response.data);
    } catch (err) {
      console.error("Error loading run history:", err);
    } finally {
      setLoadingHistory(false);
    }
  };

  const loadProjects = async () => {
    try {
      setLoadingProjects(true);
      setError("");

      const response = await axios.get<Project[]>(`${API_URL}/projects`);
      setProjects(response.data);

      if (response.data.length > 0 && selectedProjectId === null) {
        setSelectedProjectId(response.data[0].id);
      }
    } catch (err) {
      console.error("Error loading projects:", err);
      setError("Could not load projects. Check backend URL and CORS.");
    } finally {
      setLoadingProjects(false);
    }
  };

  const createProject = async () => {
    try {
      setError("");

      const response = await axios.post<Project>(`${API_URL}/projects`, {
        name,
        description,
      });

      await loadProjects();
      setSelectedProjectId(response.data.id);
    } catch (err) {
      console.error("Error creating project:", err);
      setError("Could not create project.");
    }
  };

  const startAgentAnimation = () => {
    setCompletedAgents([]);
    setActiveAgentIndex(0);

    agentPipeline.forEach((_, index) => {
      setTimeout(() => {
        setActiveAgentIndex(index);

        if (index > 0) {
          setCompletedAgents((previous) => [...previous, index - 1]);
        }

        if (index === agentPipeline.length - 1) {
          setTimeout(() => {
            setCompletedAgents((previous) => [...previous, index]);
          }, 900);
        }
      }, index * 900);
    });
  };

  const runAgent = async () => {
    if (!selectedProjectId) {
      setError("Please create or select a project first.");
      return;
    }

    if (!meetingNotes.trim()) {
      setError("Please add meeting notes before running the agent.");
      return;
    }

    try {
      setRunningAgent(true);
      setError("");
      setAgentResult(null);
      setCopiedEmail(false);
      startAgentAnimation();

      const response = await axios.post<AgentRunResult>(
        `${API_URL}/agentruns/run`,
        {
          projectId: selectedProjectId,
          inputType: "text",
          originalInput: meetingNotes,
        }
      );

      setAgentResult(response.data);
      await loadRunHistory(selectedProjectId);
    } catch (err) {
      console.error("Error running agent:", err);
      setError("Agent run failed. Check Swagger/backend console.");
    } finally {
      setRunningAgent(false);
      setActiveAgentIndex(-1);
      setCompletedAgents(agentPipeline.map((_, index) => index));
    }
  };

  const parseDecisions = (): Decision[] => {
    if (!agentResult?.decisionsJson) return [];

    try {
      return JSON.parse(agentResult.decisionsJson);
    } catch {
      return [];
    }
  };

  const parseTrace = (): TraceStep[] => {
    if (!agentResult?.agentTraceJson) return [];

    try {
      return JSON.parse(agentResult.agentTraceJson);
    } catch {
      return [];
    }
  };

  const copyEmail = async () => {
    const email = agentResult?.generatedEmails[0];

    if (!email) return;

    await navigator.clipboard.writeText(`Subject: ${email.subject}\n\n${email.body}`);
    setCopiedEmail(true);

    setTimeout(() => setCopiedEmail(false), 1800);
  };

  const getPriorityClass = (priority: string) => {
    const normalized = priority.toLowerCase();

    if (normalized === "high") {
      return "border-red-400/30 bg-red-400/10 text-red-200";
    }

    if (normalized === "medium") {
      return "border-yellow-400/30 bg-yellow-400/10 text-yellow-200";
    }

    return "border-emerald-400/30 bg-emerald-400/10 text-emerald-200";
  };

  const getSeverityClass = (severity: string) => {
    const normalized = severity.toLowerCase();

    if (normalized === "high") {
      return "border-red-400/30 bg-red-400/10 text-red-200";
    }

    if (normalized === "medium") {
      return "border-yellow-400/30 bg-yellow-400/10 text-yellow-200";
    }

    return "border-emerald-400/30 bg-emerald-400/10 text-emerald-200";
  };

  useEffect(() => {
    loadProjects();
  }, []);

  useEffect(() => {
    if (selectedProjectId) {
      loadRunHistory(selectedProjectId);
    }
  }, [selectedProjectId]);

  const decisions = parseDecisions();
  const trace = parseTrace();
  const generatedEmail = agentResult?.generatedEmails[0];

  return (
    <main className="min-h-screen overflow-x-hidden bg-[#050816] text-white">
      <div className="fixed inset-0 -z-10">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_15%_10%,rgba(51,204,204,0.22),transparent_34%),radial-gradient(circle_at_85%_0%,rgba(99,102,241,0.26),transparent_30%),linear-gradient(135deg,#06121d_0%,#050816_45%,#0d1028_100%)]" />
        <div className="absolute left-0 top-0 h-full w-full bg-[linear-gradient(rgba(255,255,255,0.03)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,0.03)_1px,transparent_1px)] bg-[size:56px_56px] opacity-30" />
      </div>

      <section className="mx-auto max-w-[1500px] px-5 py-6 lg:px-8">
        <header className="sticky top-0 z-20 mb-8 rounded-3xl border border-white/10 bg-[#070b19]/80 px-5 py-4 shadow-2xl shadow-black/30 backdrop-blur-xl">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex items-center gap-4">
              <div className="relative flex h-12 w-12 items-center justify-center rounded-2xl bg-cyan-400 text-slate-950 shadow-lg shadow-cyan-400/30">
                <Brain size={25} />
                <span className="absolute -right-1 -top-1 h-3 w-3 rounded-full bg-emerald-400 shadow-lg shadow-emerald-400/50" />
              </div>

              <div>
                <div className="flex items-center gap-3">
                  <h1 className="text-xl font-black tracking-tight">
                    AgentOps 360
                  </h1>
                  <span className="rounded-full border border-cyan-400/30 bg-cyan-400/10 px-3 py-1 text-xs font-bold text-cyan-200">
                    Gemini Live
                  </span>
                </div>
                <p className="text-sm text-slate-400">
                  Autonomous enterprise execution layer for meetings and operations.
                </p>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-3">
              <span className="rounded-full border border-white/10 bg-white/5 px-4 py-2 text-sm text-slate-300">
                Project:{" "}
                <strong className="text-white">
                  {selectedProject?.name || "No project selected"}
                </strong>
              </span>

              <span className="rounded-full border border-cyan-400/30 bg-cyan-400/10 px-4 py-2 text-sm font-semibold text-cyan-200">
                AI Agent Olympics MVP
              </span>
            </div>
          </div>
        </header>

        {error && (
          <div className="mb-6 rounded-3xl border border-red-400/30 bg-red-500/10 px-5 py-4 text-sm text-red-200 shadow-xl shadow-red-950/20">
            {error}
          </div>
        )}

        <div className="grid gap-6 xl:grid-cols-[430px_minmax(0,1fr)]">
          <aside className="space-y-6 xl:sticky xl:top-28 xl:self-start">
            <div className="overflow-hidden rounded-[2rem] border border-white/10 bg-white/[0.06] shadow-2xl shadow-black/30 backdrop-blur-xl">
              <div className="border-b border-white/10 p-6">
                <div className="mb-5 inline-flex items-center gap-2 rounded-full border border-cyan-400/20 bg-cyan-400/10 px-4 py-2 text-sm text-cyan-200">
                  <Rocket size={16} />
                  From discussion to execution
                </div>

                <h2 className="text-4xl font-black leading-tight tracking-tight">
                  Turn business chaos into clear action plans.
                </h2>

                <p className="mt-4 text-base leading-7 text-slate-300">
                  AgentOps 360 extracts decisions, tasks, risks, owners,
                  priorities, and follow-up emails from messy meeting notes.
                </p>
              </div>

              <div className="grid grid-cols-3 border-b border-white/10">
                <div className="border-r border-white/10 p-5">
                  <p className="text-3xl font-black text-cyan-300">5</p>
                  <p className="mt-1 text-xs text-slate-400">Agents</p>
                </div>

                <div className="border-r border-white/10 p-5">
                  <p className="text-3xl font-black text-cyan-300">360°</p>
                  <p className="mt-1 text-xs text-slate-400">Execution</p>
                </div>

                <div className="p-5">
                  <p className="text-3xl font-black text-cyan-300">AI</p>
                  <p className="mt-1 text-xs text-slate-400">Gemini</p>
                </div>
              </div>

              <div className="p-6">
                <h3 className="mb-4 flex items-center gap-2 text-sm font-bold uppercase tracking-[0.2em] text-slate-400">
                  <Activity size={16} className="text-cyan-300" />
                  Multi-agent pipeline
                </h3>

                <div className="space-y-3">
                  {[
                    "Intake Agent",
                    "Reasoning Agent",
                    "Planning Agent",
                    "Communication Agent",
                    "Audit Agent",
                  ].map((agent, index) => (
                    <div
                      key={agent}
                      className="flex items-center gap-3 rounded-2xl border border-white/10 bg-slate-950/60 p-3"
                    >
                      <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-cyan-400/10 text-sm font-black text-cyan-200">
                        {index + 1}
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-bold text-white">
                          {agent}
                        </p>
                        <p className="text-xs text-slate-500">Ready</p>
                      </div>
                      <span className="h-2 w-2 rounded-full bg-emerald-400 shadow-lg shadow-emerald-400/50" />
                    </div>
                  ))}
                </div>
              </div>
            </div>

            <div className="rounded-[2rem] border border-white/10 bg-white p-6 text-slate-950 shadow-2xl">
              <div className="mb-5 flex items-center gap-3">
                <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-950 text-cyan-300">
                  <Plus size={20} />
                </div>
                <div>
                  <h3 className="text-lg font-black">Project workspace</h3>
                  <p className="text-sm text-slate-500">
                    Create or select an analysis context.
                  </p>
                </div>
              </div>

              <div className="space-y-3">
                <input
                  className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm outline-none transition focus:border-cyan-400 focus:bg-white focus:ring-4 focus:ring-cyan-100"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="Project name"
                />

                <textarea
                  className="h-24 w-full resize-none rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm outline-none transition focus:border-cyan-400 focus:bg-white focus:ring-4 focus:ring-cyan-100"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Project description"
                />

                <button
                  onClick={createProject}
                  className="w-full rounded-2xl bg-slate-950 px-5 py-3 text-sm font-black text-white shadow-lg shadow-slate-950/20 transition hover:-translate-y-0.5 hover:bg-cyan-500 hover:text-slate-950"
                >
                  Create Project
                </button>
              </div>

              <div className="mt-6">
                <div className="mb-3 flex items-center gap-2 text-sm font-bold text-slate-600">
                  <ShieldCheck size={16} />
                  Existing projects
                </div>

                {loadingProjects && (
                  <p className="rounded-2xl bg-slate-50 p-4 text-sm text-slate-500">
                    Loading projects...
                  </p>
                )}

                <div className="max-h-64 space-y-3 overflow-y-auto pr-1">
                  {projects.map((project) => (
                    <button
                      key={project.id}
                      onClick={() => setSelectedProjectId(project.id)}
                      className={`w-full rounded-2xl border p-4 text-left transition ${
                        selectedProjectId === project.id
                          ? "border-cyan-400 bg-cyan-50 shadow-lg shadow-cyan-100"
                          : "border-slate-100 bg-slate-50 hover:border-cyan-200 hover:bg-white"
                      }`}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <h4 className="font-black">{project.name}</h4>
                          <p className="mt-1 line-clamp-2 text-sm text-slate-500">
                            {project.description}
                          </p>
                        </div>

                        <span className="rounded-full bg-slate-950 px-2.5 py-1 text-xs font-bold text-white">
                          #{project.id}
                        </span>
                      </div>
                    </button>
                  ))}

                  {projects.length === 0 && !loadingProjects && (
                    <p className="rounded-2xl bg-slate-50 p-4 text-sm text-slate-500">
                      No project yet. Create your first one.
                    </p>
                  )}
                </div>

                <div className="rounded-[2rem] border border-white/10 bg-white/[0.06] p-6 shadow-2xl shadow-black/30 backdrop-blur-xl">
                  <div className="mb-5 flex items-center justify-between gap-3">
                    <div>
                      <h3 className="text-lg font-black text-white">Analysis History</h3>
                      <p className="mt-1 text-sm text-slate-400">
                        Reopen previous agent runs for this project.
                      </p>
                    </div>

                    <span className="rounded-full border border-cyan-400/30 bg-cyan-400/10 px-3 py-1 text-xs font-bold text-cyan-200">
                      {runHistory.length}
                    </span>
                  </div>

                  {loadingHistory && (
                    <p className="rounded-2xl border border-white/10 bg-slate-950/60 p-4 text-sm text-slate-400">
                      Loading history...
                    </p>
                  )}

                  {!loadingHistory && runHistory.length === 0 && (
                    <p className="rounded-2xl border border-white/10 bg-slate-950/60 p-4 text-sm leading-6 text-slate-400">
                      No analysis yet. Run the agent once and the history will appear here.
                    </p>
                  )}

                  <div className="max-h-80 space-y-3 overflow-y-auto pr-1">
                    {runHistory.map((run) => (
                      <button
                        key={run.id}
                        onClick={() => {
                          setAgentResult(run);
                          setCompletedAgents(agentPipeline.map((_, index) => index));
                          setActiveAgentIndex(-1);
                        }}
                        className={`w-full rounded-2xl border p-4 text-left transition ${
                          agentResult?.id === run.id
                            ? "border-cyan-400 bg-cyan-400/10"
                            : "border-white/10 bg-slate-950/60 hover:border-cyan-400/40 hover:bg-white/[0.06]"
                        }`}
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div className="min-w-0">
                            <p className="truncate text-sm font-black text-white">
                              Run #{run.id}
                            </p>
                            <p className="mt-1 text-xs text-slate-500">
                              {new Date(run.createdAt).toLocaleString()}
                            </p>
                          </div>

                          <span className="rounded-full border border-cyan-400/30 bg-cyan-400/10 px-2.5 py-1 text-xs font-bold text-cyan-200">
                            {run.confidenceScore}%
                          </span>
                        </div>

                        <p className="mt-3 line-clamp-2 text-xs leading-5 text-slate-400">
                          {run.executiveSummary || run.originalInput}
                        </p>

                        <div className="mt-3 flex flex-wrap gap-2">
                          <span className="rounded-full bg-white/5 px-2.5 py-1 text-[11px] font-bold text-slate-300">
                            {run.aiMode}
                          </span>
                          <span className="rounded-full bg-white/5 px-2.5 py-1 text-[11px] font-bold text-slate-300">
                            {run.tasks.length} tasks
                          </span>
                          <span className="rounded-full bg-white/5 px-2.5 py-1 text-[11px] font-bold text-slate-300">
                            {run.risks.length} risks
                          </span>
                        </div>
                      </button>
                    ))}
                  </div>
                </div>  

              </div>
            </div>
          </aside>

          <section className="min-w-0 space-y-6">
            <div className="rounded-[2rem] border border-white/10 bg-white/[0.06] p-6 shadow-2xl shadow-black/30 backdrop-blur-xl">
              <div className="mb-5 flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                <div className="flex items-center gap-3">
                  <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-cyan-400 text-slate-950 shadow-lg shadow-cyan-400/20">
                    <Zap size={22} />
                  </div>

                  <div>
                    <h3 className="text-2xl font-black">
                      Run Autonomous Analysis
                    </h3>
                    <p className="text-sm text-slate-400">
                      Paste notes, choose a scenario, and launch the agent workflow.
                    </p>
                  </div>
                </div>

                <div className="flex flex-wrap gap-2">
                  <label className="inline-flex cursor-pointer items-center gap-2 rounded-full border border-cyan-400/30 bg-cyan-400/10 px-4 py-2 text-xs font-bold text-cyan-100 transition hover:bg-cyan-400 hover:text-slate-950">
                    <UploadCloud size={14} />
                    Upload file
                    <input
                      type="file"
                      accept=".txt,.pdf,.mp3,.wav,.m4a,.mp4,.webm,.ogg,.flac,text/plain,application/pdf,audio/*,video/mp4"
                      onChange={handleFileUpload}
                      className="hidden"
                    />
                  </label>

                  {samplePrompts.map((sample) => (
                    <button
                      key={sample.label}
                      onClick={() => setMeetingNotes(sample.value)}
                      className="rounded-full border border-white/10 bg-white/5 px-4 py-2 text-xs font-bold text-slate-300 transition hover:border-cyan-400/40 hover:bg-cyan-400/10 hover:text-cyan-100"
                    >
                      {sample.label}
                    </button>
                  ))}
                </div>
              </div>

              <textarea
                className="h-44 w-full resize-none rounded-3xl border border-white/10 bg-slate-950/80 px-5 py-4 text-sm leading-7 text-slate-100 outline-none transition placeholder:text-slate-500 focus:border-cyan-400 focus:ring-4 focus:ring-cyan-400/10"
                value={meetingNotes}
                onChange={(e) => setMeetingNotes(e.target.value)}
                placeholder="Paste meeting notes here..."
              />

              <div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-slate-500">
                <span className="rounded-full bg-white/5 px-3 py-1.5">
                  Supports typed notes
                </span>
                <span className="rounded-full bg-white/5 px-3 py-1.5">
                  Supports .txt upload
                </span>
                <span className="rounded-full bg-white/5 px-3 py-1.5">
                  Supports PDF text extraction
                </span>
                <span className="rounded-full bg-white/5 px-3 py-1.5">
                  Audio upload ready
                </span>
              </div>

              <div className="mt-4 flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                <div className="flex flex-wrap items-center gap-3 text-xs text-slate-400">
                  <span className="inline-flex items-center gap-2 rounded-full bg-white/5 px-3 py-2">
                    <Sparkles size={14} className="text-cyan-300" />
                    Gemini reasoning
                  </span>
                  <span className="inline-flex items-center gap-2 rounded-full bg-white/5 px-3 py-2">
                    <FileText size={14} className="text-cyan-300" />
                    Structured JSON output
                  </span>
                  <span className="inline-flex items-center gap-2 rounded-full bg-white/5 px-3 py-2">
                    <Users size={14} className="text-cyan-300" />
                    Enterprise workflow
                  </span>
                </div>

                <button
                  onClick={runAgent}
                  disabled={runningAgent}
                  className="flex min-w-64 items-center justify-center gap-2 rounded-2xl bg-cyan-400 px-6 py-3 text-sm font-black text-slate-950 shadow-xl shadow-cyan-400/20 transition hover:-translate-y-0.5 hover:bg-cyan-300 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  <Activity size={18} />
                  {runningAgent ? "Agents are executing workflow..." : "Run Autonomous Analysis"}
                </button>
              </div>
              {(runningAgent || completedAgents.length > 0 || agentResult) && (
                <div className="mt-6 rounded-3xl border border-white/10 bg-slate-950/60 p-5">
                  <div className="mb-4 flex items-center justify-between gap-4">
                    <div>
                      <h4 className="text-sm font-black uppercase tracking-[0.2em] text-cyan-200">
                        Live Agent Pipeline
                      </h4>
                      <p className="mt-1 text-sm text-slate-500">
                        Autonomous workflow execution from raw notes to business action plan.
                      </p>
                    </div>

                    <span className="rounded-full border border-cyan-400/30 bg-cyan-400/10 px-4 py-2 text-xs font-bold text-cyan-100">
                      {runningAgent ? "Running" : "Completed"}
                    </span>
                  </div>

                  <div className="grid gap-3 lg:grid-cols-5">
                    {agentPipeline.map((agent, index) => {
                      const isActive = activeAgentIndex === index && runningAgent;
                      const isCompleted = completedAgents.includes(index) || Boolean(agentResult);

                      return (
                        <div
                          key={agent.name}
                          className={`relative overflow-hidden rounded-3xl border p-4 transition duration-500 ${
                            isActive
                              ? "border-cyan-400/60 bg-cyan-400/10 shadow-xl shadow-cyan-400/10"
                              : isCompleted
                              ? "border-emerald-400/30 bg-emerald-400/10"
                              : "border-white/10 bg-white/[0.03]"
                          }`}
                        >
                          <div className="mb-4 flex items-center justify-between">
                            <div
                              className={`flex h-10 w-10 items-center justify-center rounded-2xl ${
                                isActive
                                  ? "bg-cyan-400 text-slate-950"
                                  : isCompleted
                                  ? "bg-emerald-400/20 text-emerald-200"
                                  : "bg-white/5 text-slate-500"
                              }`}
                            >
                              {isActive ? (
                                <Loader2 size={18} className="animate-spin" />
                              ) : isCompleted ? (
                                <CircleCheck size={18} />
                              ) : (
                                <span className="text-sm font-black">{index + 1}</span>
                              )}
                            </div>

                            {index < agentPipeline.length - 1 && (
                              <ArrowRight
                                size={16}
                                className={`hidden lg:block ${
                                  isCompleted ? "text-emerald-300" : "text-slate-600"
                                }`}
                              />
                            )}
                          </div>

                          <h5
                            className={`text-sm font-black ${
                              isActive
                                ? "text-cyan-100"
                                : isCompleted
                                ? "text-emerald-100"
                                : "text-slate-400"
                            }`}
                          >
                            {agent.name}
                          </h5>

                          <p className="mt-2 text-xs leading-5 text-slate-500">
                            {agent.description}
                          </p>

                          <div className="mt-4 h-1.5 overflow-hidden rounded-full bg-white/5">
                            <div
                              className={`h-full rounded-full transition-all duration-700 ${
                                isActive
                                  ? "w-2/3 bg-cyan-400"
                                  : isCompleted
                                  ? "w-full bg-emerald-400"
                                  : "w-0 bg-slate-700"
                              }`}
                            />
                          </div>

                          {isActive && (
                            <div className="absolute inset-0 -z-0 bg-[radial-gradient(circle_at_top,rgba(51,204,204,0.22),transparent_55%)]" />
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}
            </div>

            {!agentResult && (
              <div className="rounded-[2rem] border border-dashed border-white/15 bg-white/[0.04] p-10 text-center shadow-2xl shadow-black/20">
                <div className="mx-auto mb-5 flex h-16 w-16 items-center justify-center rounded-3xl bg-cyan-400/10 text-cyan-300">
                  <Brain size={32} />
                </div>
                <h3 className="text-2xl font-black">No analysis yet</h3>
                <p className="mx-auto mt-3 max-w-xl text-sm leading-7 text-slate-400">
                  Run the autonomous workflow to generate an executive summary,
                  decisions, tasks, risks, email, and a full agent trace.
                </p>
              </div>
            )}

            {agentResult && (
              <div className="space-y-6">
                <div className="flex flex-col gap-4 rounded-[2rem] border border-cyan-400/20 bg-cyan-400/10 p-5 shadow-2xl shadow-cyan-950/20 md:flex-row md:items-center md:justify-between">
                  <div>
                    <h3 className="text-xl font-black text-white">
                      Executive report is ready
                    </h3>
                    <p className="mt-1 text-sm text-cyan-100/80">
                      Export a structured Markdown report including summary, tasks, risks,
                      email, and agent trace.
                    </p>
                  </div>

                  <button
                    onClick={exportExecutiveReport}
                    className="inline-flex items-center justify-center gap-2 rounded-2xl bg-cyan-400 px-5 py-3 text-sm font-black text-slate-950 shadow-xl shadow-cyan-400/20 transition hover:-translate-y-0.5 hover:bg-cyan-300"
                  >
                    <Download size={18} />
                    Export Report
                  </button>
                </div>
                <div className="grid gap-4 md:grid-cols-5">
                  <MetricCard
                    icon={<Gauge size={20} />}
                    label="Confidence"
                    value={`${agentResult.confidenceScore}%`}
                  />
                  <MetricCard
                    icon={<Sparkles size={20} />}
                    label="AI Mode"
                    value={agentResult.aiMode}
                  />
                  <MetricCard
                    icon={<CheckCircle2 size={20} />}
                    label="Decisions"
                    value={decisions.length}
                  />
                  <MetricCard
                    icon={<ClipboardList size={20} />}
                    label="Tasks"
                    value={agentResult.tasks.length}
                  />
                  <MetricCard
                    icon={<AlertTriangle size={20} />}
                    label="Risks"
                    value={agentResult.risks.length}
                  />
                </div>

                <Panel
                  icon={<ClipboardList size={20} />}
                  title="Executive Summary"
                  subtitle="High-level business interpretation created by the agent."
                >
                  <p className="text-base leading-8 text-slate-300">
                    {agentResult.executiveSummary}
                  </p>
                </Panel>

                <Panel
                  icon={<CheckCircle2 size={20} />}
                  title="Decisions"
                  subtitle="Business decisions inferred from the meeting."
                >
                  <div className="grid gap-3 lg:grid-cols-2">
                    {decisions.map((decision, index) => (
                      <div
                        key={index}
                        className="rounded-3xl border border-white/10 bg-slate-950/70 p-5"
                      >
                        <div className="mb-3 flex h-9 w-9 items-center justify-center rounded-2xl bg-cyan-400/10 text-sm font-black text-cyan-200">
                          {index + 1}
                        </div>

                        <h4 className="text-base font-black text-white">
                          {decision.title}
                        </h4>

                        <p className="mt-3 text-sm leading-6 text-slate-400">
                          <span className="font-bold text-slate-300">Evidence:</span>{" "}
                          {decision.evidence}
                        </p>
                      </div>
                    ))}
                  </div>
                </Panel>

                <Panel
                  icon={<ClipboardList size={20} />}
                  title="Action Plan"
                  subtitle="Concrete next steps with owners and priorities."
                >
                  <div className="grid gap-3 xl:grid-cols-3">
                    {agentResult.tasks.map((task) => (
                      <div
                        key={task.id}
                        className="rounded-3xl border border-white/10 bg-slate-950/70 p-5"
                      >
                        <div className="mb-5 flex items-start justify-between gap-4">
                          <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-cyan-400/10 text-cyan-200">
                            <ClipboardList size={18} />
                          </div>

                          <span
                            className={`rounded-full border px-3 py-1 text-xs font-black ${getPriorityClass(
                              task.priority
                            )}`}
                          >
                            {task.priority}
                          </span>
                        </div>

                        <h4 className="text-base font-black text-white">
                          {task.title}
                        </h4>

                        <p className="mt-2 text-sm text-cyan-100">
                          Owner: {task.owner || "Not assigned"}
                        </p>

                        <p className="mt-4 text-sm leading-6 text-slate-400">
                          <span className="font-bold text-slate-300">Evidence:</span>{" "}
                          {task.sourceEvidence}
                        </p>
                      </div>
                    ))}
                  </div>
                </Panel>

                <Panel
                  icon={<AlertTriangle size={20} />}
                  title="Risk Matrix"
                  subtitle="Operational risks and mitigation strategy."
                >
                  <div className="grid gap-3 lg:grid-cols-2">
                    {agentResult.risks.map((risk) => (
                      <div
                        key={risk.id}
                        className="rounded-3xl border border-white/10 bg-slate-950/70 p-5"
                      >
                        <div className="mb-4 flex items-start justify-between gap-4">
                          <h4 className="text-base font-black text-white">
                            {risk.title}
                          </h4>

                          <span
                            className={`rounded-full border px-3 py-1 text-xs font-black ${getSeverityClass(
                              risk.severity
                            )}`}
                          >
                            {risk.severity}
                          </span>
                        </div>

                        <p className="text-sm leading-6 text-slate-300">
                          <span className="font-bold text-yellow-100">
                            Mitigation:
                          </span>{" "}
                          {risk.mitigation}
                        </p>

                        <p className="mt-3 text-sm leading-6 text-slate-500">
                          Evidence: {risk.sourceEvidence}
                        </p>
                      </div>
                    ))}
                  </div>
                </Panel>

                {generatedEmail && (
                  <Panel
                    icon={<Mail size={20} />}
                    title="Generated Follow-up Email"
                    subtitle="Ready-to-send communication drafted by the agent."
                    action={
                      <button
                        onClick={copyEmail}
                        className="inline-flex items-center gap-2 rounded-full border border-cyan-400/30 bg-cyan-400/10 px-4 py-2 text-xs font-bold text-cyan-100 transition hover:bg-cyan-400 hover:text-slate-950"
                      >
                        <Copy size={14} />
                        {copiedEmail ? "Copied" : "Copy email"}
                      </button>
                    }
                  >
                    <div className="rounded-3xl border border-white/10 bg-slate-950/80 p-5">
                      <p className="mb-4 text-sm font-black text-cyan-100">
                        Subject: {generatedEmail.subject}
                      </p>

                      <pre className="whitespace-pre-wrap font-sans text-sm leading-7 text-slate-300">
                        {generatedEmail.body}
                      </pre>
                    </div>
                  </Panel>
                )}

                <Panel
                  icon={<Activity size={20} />}
                  title="Agent Trace"
                  subtitle="Transparent multi-agent reasoning pipeline."
                >
                  <div className="relative">
                    <div className="absolute left-5 top-5 hidden h-[calc(100%-2.5rem)] w-px bg-cyan-400/20 md:block" />

                    <div className="space-y-3">
                      {trace.map((traceStep, index) => (
                        <div
                          key={index}
                          className="relative rounded-3xl border border-white/10 bg-slate-950/70 p-5 md:ml-8"
                        >
                          <div className="absolute -left-[2.15rem] top-5 hidden h-10 w-10 items-center justify-center rounded-2xl border border-cyan-400/30 bg-[#08111f] text-sm font-black text-cyan-200 md:flex">
                            {index + 1}
                          </div>

                          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                            <h4 className="font-black text-cyan-100">
                              {traceStep.agent}
                            </h4>

                            <span className="w-fit rounded-full border border-emerald-400/30 bg-emerald-400/10 px-3 py-1 text-xs font-black text-emerald-200">
                              {traceStep.status}
                            </span>
                          </div>

                          <p className="mt-3 text-sm leading-6 text-slate-400">
                            {traceStep.step}
                          </p>
                        </div>
                      ))}
                    </div>
                  </div>
                </Panel>
              </div>
            )}
          </section>
        </div>
      </section>
    </main>
  );
}

function MetricCard({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
  label: string;
  value: string | number;
}) {
  return (
    <div className="rounded-3xl border border-white/10 bg-white/[0.06] p-5 shadow-2xl shadow-black/20 backdrop-blur-xl">
      <div className="mb-4 flex h-10 w-10 items-center justify-center rounded-2xl bg-cyan-400/10 text-cyan-300">
        {icon}
      </div>
      <p className="text-sm text-slate-400">{label}</p>
      <p className="mt-1 text-4xl font-black text-cyan-300">{value}</p>
    </div>
  );
}

function Panel({
  icon,
  title,
  subtitle,
  children,
  action,
}: {
  icon: React.ReactNode;
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  action?: React.ReactNode;
}) {
  return (
    <section className="rounded-[2rem] border border-white/10 bg-white/[0.06] p-6 shadow-2xl shadow-black/20 backdrop-blur-xl">
      <div className="mb-5 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex items-start gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl bg-cyan-400/10 text-cyan-300">
            {icon}
          </div>

          <div>
            <h3 className="text-2xl font-black tracking-tight text-white">
              {title}
            </h3>
            {subtitle && (
              <p className="mt-1 text-sm text-slate-400">{subtitle}</p>
            )}
          </div>
        </div>

        {action}
      </div>

      {children}
    </section>
  );
}

export default App;