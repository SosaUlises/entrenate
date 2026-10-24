import type { ReactNode } from "react";
import { RoutineDraftProvider } from "@/features/routines/context/routine-draft-context";

export default async function EditRoutineLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return <RoutineDraftProvider key={id}>{children}</RoutineDraftProvider>;
}
