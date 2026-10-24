import type { Metadata } from "next";
import { HistoryDetailContent } from "@/features/history/components/history-detail-content";

export const metadata: Metadata = {
  title: "Entrenamiento | Entrenate",
};

export default async function HistoryDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <HistoryDetailContent id={id} />;
}
