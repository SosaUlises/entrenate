import type { ReactNode } from "react";
import { CreateRoutineDraftProvider } from "@/features/routines/context/routine-draft-persistence";

export default function NewRoutineLayout({ children }: { children: ReactNode }) {
  return <CreateRoutineDraftProvider>{children}</CreateRoutineDraftProvider>;
}
