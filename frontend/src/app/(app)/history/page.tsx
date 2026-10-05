import type { Metadata } from "next";
import { HistoryContent } from "@/features/history/components/history-content";

export const metadata: Metadata = {
  title: "Historial | Entrenate",
};

export default function HistoryPage() {
  return <HistoryContent />;
}
