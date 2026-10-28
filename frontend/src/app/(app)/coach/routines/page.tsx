import type { Metadata } from "next";
import { RoutinePlannerEntry } from "@/features/coach/components/routine-planner-entry";

export const metadata: Metadata = {
  title: "Planificador de rutinas | Entrenate",
};

export default function RoutinePlannerPage() {
  return <RoutinePlannerEntry />;
}
