"use client";

import { useCoAgent, useCopilotAction, useCopilotAdditionalInstructions } from "@copilotkit/react-core";
import { CopilotKitCSSProperties, CopilotChat, CopilotPopup } from "@copilotkit/react-ui";
import { useEffect, useRef } from "react";
import { PopupHeader } from "@/components/kanban/AppChatHeader";
import type { AgentState } from "@/lib/kanban/types";
import { initialState, isNonEmptyAgentState } from "@/lib/kanban/state";
import useMediaQuery from "@/hooks/use-media-query";
import KanbanBoard from "@/components/kanban/KanbanBoard";
import BoardTabs from "@/components/kanban/BoardTabs";

export default function CopilotKitPage() {
  const { state, setState } = useCoAgent<AgentState>({
    name: "my_agent",
    initialState,
  });

  // UI-only action: switchBoard (no backend equivalent)
  useCopilotAction({
    name: "switchBoard",
    description: "Switch to a different board.",
    available: "remote",
    parameters: [
      { name: "boardId", type: "string", required: true, description: "Board ID to switch to" }
    ],
    handler: ({ boardId }) => {
      setState(prev => ({
        boards: prev?.boards ?? initialState.boards,
        activeBoardId: boardId,
        lastAction: prev?.lastAction
      }));
      return "Switched board";
    }
  });

  const cachedStateRef = useRef<AgentState>(state ?? initialState);
  useEffect(() => {
    if (isNonEmptyAgentState(state)) {
      cachedStateRef.current = state as AgentState;
    }
  }, [state]);

  const viewState: AgentState = isNonEmptyAgentState(state) ? (state as AgentState) : cachedStateRef.current;
  useEffect(() => {
    console.log("Current state:");
    console.log(viewState);
  }, [viewState]);

  const isDesktop = useMediaQuery("(min-width: 768px)");

  const handleSwitchBoard = (boardId: string) => {
    setState({ ...viewState, activeBoardId: boardId });
  };

  const handleCreateBoard = () => {
    const name = prompt("Enter board name:");
    if (name) {
      console.log(`[BoardTabs] Create board requested: ${name}`);
    }
  };

  // Task management handlers for UI-driven updates
  const handleUpdateTaskTitle = (taskId: string, title: string) => {
    console.log(`[Task] Update title: ${taskId} -> ${title}`);
    setState(prev => {
      const boards = (prev?.boards ?? initialState.boards).map(board => ({
        ...board,
        tasks: board.tasks.map(task =>
          task.id === taskId ? { ...task, title } : task
        )
      }));
      return { ...viewState, boards };
    });
  };

  const handleUpdateTaskSubtitle = (taskId: string, subtitle: string) => {
    console.log(`[Task] Update subtitle: ${taskId} -> ${subtitle}`);
    setState(prev => {
      const boards = (prev?.boards ?? initialState.boards).map(board => ({
        ...board,
        tasks: board.tasks.map(task =>
          task.id === taskId ? { ...task, subtitle } : task
        )
      }));
      return { ...viewState, boards };
    });
  };

  const handleAddTaskTag = (taskId: string, tag: string) => {
    console.log(`[Task] Add tag: ${taskId} -> ${tag}`);
    setState(prev => {
      const boards = (prev?.boards ?? initialState.boards).map(board => ({
        ...board,
        tasks: board.tasks.map(task =>
          task.id === taskId && !task.tags.includes(tag)
            ? { ...task, tags: [...task.tags, tag] }
            : task
        )
      }));
      return { ...viewState, boards };
    });
  };

  const handleRemoveTaskTag = (taskId: string, tag: string) => {
    console.log(`[Task] Remove tag: ${taskId} -> ${tag}`);
    setState(prev => {
      const boards = (prev?.boards ?? initialState.boards).map(board => ({
        ...board,
        tasks: board.tasks.map(task =>
          task.id === taskId
            ? { ...task, tags: task.tags.filter(t => t !== tag) }
            : task
        )
      }));
      return { ...viewState, boards };
    });
  };

  useEffect(() => {
    console.log("[CoAgent state updated]", state);
  }, [state]);

  useCopilotAdditionalInstructions({
    instructions: (() => {
      const boards = viewState.boards ?? initialState.boards;
      const activeBoardId = viewState.activeBoardId ?? initialState.activeBoardId;
      const activeBoard = boards.find(b => b.id === activeBoardId);
      const boardInfo = activeBoard
        ? `Active Board: "${activeBoard.name}" (${activeBoard.tasks.length} tasks)`
        : "No active board";

      const schema = [
        "KANBAN STRUCTURE (authoritative):",
        "- Board: { id, name, tasks[] }",
        "- Task: { id, title, subtitle, description, tags[], status }",
        "- Status values: 'new' | 'in_progress' | 'review' | 'completed'",
        "",
        "AVAILABLE OPERATIONS:",
        "Board tools: switchBoard",
        "",
        "USAGE HINTS:",
        "- Create new boards for different projects/contexts",
        "- Use tags for categorization (bug, feature, urgent, etc.)",
        "- Status progression: new → in_progress → review → completed",
        "- Task titles should be concise action items",
        "- Use subtitle for additional context",
        "- Use description for detailed information"
      ].join("\n");

      return [
        "ALWAYS ANSWER FROM SHARED STATE (GROUND TRUTH).",
        boardInfo,
        schema,
        "REMEMBER: The frontend ONLY updates when you call update_state. If you skip this step, users won't see changes!\n## REWARDS\nThe worst mistake is to not call update_state after updating the state (-$1000). It is UNACCEPTABLE!"
      ].join("\n\n");
    })(),
  });

  return (
    <div
      style={{ "--copilot-kit-primary-color": "#2563eb" } as CopilotKitCSSProperties}
      className="relative h-screen flex flex-col bg-[#DEDEE9] p-2"
    >
      {/* Gradient Orbs Background */}
      <div className="absolute w-[445.84px] h-[445.84px] left-[1040px] top-[11px] rounded-full z-0"
           style={{ background: 'rgba(255, 172, 77, 0.2)', filter: 'blur(103.196px)' }} />
      <div className="absolute w-[609.35px] h-[609.35px] left-[1338.97px] top-[624.5px] rounded-full z-0"
           style={{ background: '#C9C9DA', filter: 'blur(103.196px)' }} />
      <div className="absolute w-[609.35px] h-[609.35px] left-[670px] top-[-365px] rounded-full z-0"
           style={{ background: '#C9C9DA', filter: 'blur(103.196px)' }} />
      <div className="absolute w-[609.35px] h-[609.35px] left-[507.87px] top-[702.14px] rounded-full z-0"
           style={{ background: '#F3F3FC', filter: 'blur(103.196px)' }} />
      <div className="absolute w-[445.84px] h-[445.84px] left-[127.91px] top-[331px] rounded-full z-0"
           style={{ background: 'rgba(255, 243, 136, 0.3)', filter: 'blur(103.196px)' }} />
      <div className="absolute w-[445.84px] h-[445.84px] left-[-205px] top-[802.72px] rounded-full z-0"
           style={{ background: 'rgba(255, 172, 77, 0.2)', filter: 'blur(103.196px)' }} />

      <div className="flex flex-1 overflow-hidden z-10 gap-2">
        <aside className="-order-1 max-md:hidden flex flex-col min-w-80 w-[30vw] max-w-120">
          <div className="h-full flex flex-col align-start w-full border-2 border-white bg-white/50 backdrop-blur-md shadow-elevation-lg rounded-lg overflow-hidden">
            <div className="p-6 border-b border-[#DBDBE5]">
              <h1 className="text-xl font-semibold text-[#010507] mb-1">Kanban Board</h1>
              <p className="text-sm text-[#57575B]">AI-powered task management</p>
            </div>
            {isDesktop && (
              <CopilotChat
                className="flex-1 overflow-auto w-full"
                labels={{
                  title: "Agent",
                  initial: "Welcome to your Kanban board! Ask me to help manage tasks.",
                }}
                suggestions={[
                  { title: "Add a Task", message: "Create a new task." },
                  { title: "Move Task", message: "Move a task to another status." },
                  { title: "List Tasks", message: "Show all tasks." },
                ]}
              />
            )}
          </div>
        </aside>
        <main className="relative flex flex-1 h-full flex-col rounded-lg bg-white/30 backdrop-blur-sm overflow-hidden">
          <BoardTabs
            boards={viewState.boards}
            activeBoardId={viewState.activeBoardId}
            onSwitchBoard={handleSwitchBoard}
            onCreateBoard={handleCreateBoard}
          />
          <div className="flex-1 overflow-auto">
            <KanbanBoard
              boards={viewState.boards}
              activeBoardId={viewState.activeBoardId}
              onUpdateTaskTitle={handleUpdateTaskTitle}
              onUpdateTaskSubtitle={handleUpdateTaskSubtitle}
              onAddTaskTag={handleAddTaskTag}
              onRemoveTaskTag={handleRemoveTaskTag}
            />
          </div>
        </main>
      </div>
      <div className="md:hidden">
        {!isDesktop && (
          <CopilotPopup
            Header={PopupHeader}
            labels={{
              title: "Agent",
              initial: "Welcome to your Kanban board! Ask me to help manage tasks.",
            }}
            suggestions={[
              { title: "Add a Task", message: "Create a new task." },
              { title: "Move Task", message: "Move a task to another status." },
              { title: "List Tasks", message: "Show all tasks." },
            ]}
          />
        )}
      </div>
    </div>
  );
}
