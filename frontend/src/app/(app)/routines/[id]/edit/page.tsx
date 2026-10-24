import type { Metadata } from "next";
import { EditRoutineContent } from "@/features/routines/components/edit-routine-content";

export const metadata: Metadata = {
  title: "Editar rutina | Entrenate",
};

export default async function EditRoutinePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <EditRoutineContent id={id} />;
}
