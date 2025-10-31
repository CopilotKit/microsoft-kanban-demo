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

  // useCopilotAction({
  //   name: "createBoard",
  //   description: "Create a new Kanban board.",
  //   available: "remote",
  //   parameters: [
  //     { name: "name", type: "string", required: true, description: "Board name" }
  //   ],
  //   handler: () => {
  //     return "Board created";
  //   }
  // });

  // useCopilotAction({
  //   name: "deleteBoard",
  //   description: "Delete a Kanban board.",
  //   available: "remote",
  //   parameters: [
  //     { name: "boardId", type: "string", required: true, description: "Board ID to delete" }
  //   ],
  //   handler: () => {
  //     return "Board deleted";
  //   }
  // });

  // useCopilotAction({
  //   name: "renameBoard",
  //   description: "Rename a Kanban board.",
  //   available: "remote",
  //   parameters: [
  //     { name: "boardId", type: "string", required: true, description: "Board ID to rename" },
  //     { name: "name", type: "string", required: true, description: "New board name" }
  //   ],
  //   handler: () => {
  //     return "Board renamed";
  //   }
  // });

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

  // // Task Management Actions
  // useCopilotAction({
  //   name: "createTask",
  //   description: "Create a new task on the active board.",
  //   available: "remote",
  //   parameters: [
  //     { name: "title", type: "string", required: true, description: "Title of the task" },
  //     { name: "subtitle", type: "string", required: false, description: "Optional subtitle for the task" },
  //     { name: "description", type: "string", required: false, description: "Optional description for the task" }
  //   ],
  //   handler: () => {
  //     console.log("Task created!");
  //     return "Task created";
  //   }
  // });

  // useCopilotAction({
  //   name: "setTaskTitle",
  //   description: "Update task title.",
  //   available: "remote",
  //   parameters: [
  //     { name: "taskId", type: "string", required: true, description: "ID of the task to update" },
  //     { name: "title", type: "string", required: true, description: "New title for the task" }
  //   ],
  //   handler: () => {
  //     return "Task title updated";
  //   }
  // });

  // useCopilotAction({
  //   name: "setTaskSubtitle",
  //   description: "Update task subtitle.",
  //   available: "remote",
  //   parameters: [
  //     { name: "taskId", type: "string", required: true, description: "ID of the task to update" },
  //     { name: "subtitle", type: "string", required: true, description: "New subtitle for the task" }
  //   ],
  //   handler: () => {
  //     return "Task subtitle updated";
  //   }
  // });

  // useCopilotAction({
  //   name: "setTaskDescription",
  //   description: "Update task description.",
  //   available: "remote",
  //   parameters: [
  //     { name: "taskId", type: "string", required: true, description: "ID of the task to update" },
  //     { name: "description", type: "string", required: true, description: "New description for the task" }
  //   ],
  //   handler: () => {
  //     return "Task description updated";
  //   }
  // });

  // useCopilotAction({
  //   name: "addTaskTag",
  //   description: "Add a tag to a task.",
  //   available: "remote",
  //   parameters: [
  //     { name: "taskId", type: "string", required: true, description: "ID of the task" },
  //     { name: "tag", type: "string", required: true, description: "Tag to add" }
  //   ],
  //   handler: () => {
  //     return "Tag added";
  //   }
  // });

  // useCopilotAction({
  //   name: "removeTaskTag",
  //   description: "Remove a tag from a task.",
  //   available: "remote",
  //   parameters: [
  //     { name: "taskId", type: "string", required: true, description: "ID of the task" },
  //     { name: "tag", type: "string", required: true, description: "Tag to remove" }
  //   ],
  //   handler: () => {
  //     return "Tag removed";
  //   }
  // });

  // useCopilotAction({
  //   name: "setTaskStatus",
  //   description: "Move task to a different status column.",
  //   available: "remote",
  //   parameters: [
  //     { name: "taskId", type: "string", required: true, description: "ID of the task" },
  //     { name: "status", type: "string", required: true, description: "New status (new, in_progress, review, or completed)" }
  //   ],
  //   handler: () => {
  //     return "Task status updated";
  //   }
  // });

  // useCopilotAction({
  //   name: "deleteTask",
  //   description: "Delete a task.",
  //   available: "remote",
  //   parameters: [
  //     { name: "taskId", type: "string", required: true, description: "ID of the task to delete" }
  //   ],
  //   handler: () => {
  //     return "Task deleted";
  //   }
  // });

  const cachedStateRef = useRef<AgentState>(state ?? initialState);
  useEffect(() => {
    if (isNonEmptyAgentState(state)) {
      cachedStateRef.current = state as AgentState;
    }
  }, [state]);
  const viewState: AgentState = isNonEmptyAgentState(state) ? (state as AgentState) : cachedStateRef.current;

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
      className="h-screen flex flex-col"
    >
      <div className="flex flex-1 overflow-hidden">
        <aside className="-order-1 max-md:hidden flex flex-col min-w-80 w-[30vw] max-w-120 p-4 pr-0">
          <div className="h-full flex flex-col align-start w-full shadow-lg rounded-2xl border border-sidebar-border overflow-hidden">
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
        <main className="relative flex flex-1 h-full flex-col">
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
