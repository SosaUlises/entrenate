import type { Metadata } from "next";
import { RoutinePlannerProposalPreview } from "@/features/coach/components/routine-planner-proposal-preview";

export const metadata: Metadata = {
  title: "Propuesta de rutina | Entrenate",
};

export default function RoutinePlannerProposalPage() {
  return <RoutinePlannerProposalPreview />;
}
