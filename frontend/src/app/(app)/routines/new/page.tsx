import type { Metadata } from "next";
import { NewRoutineForm } from "@/features/routines/components/new-routine-form";

export const metadata: Metadata = {
  title: "Nueva rutina | Entrenate",
};

export default function NewRoutinePage() {
  return <NewRoutineForm />;
}
