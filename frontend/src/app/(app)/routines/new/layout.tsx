import type { ReactNode } from "react";
import { RoutineDraftProvider } from "@/features/routines/context/routine-draft-context";

export default function NewRoutineLayout({ children }: { children: ReactNode }) {
  return <RoutineDraftProvider>{children}</RoutineDraftProvider>;
}
