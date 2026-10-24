import type { Metadata } from "next";
import { RoutineDetailContent } from "@/features/routines/components/routine-detail-content";

export const metadata: Metadata = {
  title: "Detalle de rutina | Entrenate",
};

export default async function RoutineDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <RoutineDetailContent id={id} />;
}
