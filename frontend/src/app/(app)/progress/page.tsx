import type { Metadata } from "next";
import { ProgressHubContent } from "@/features/progress/components/progress-hub-content";

export const metadata: Metadata = {
  title: "Progreso | Entrenate",
};

export default function ProgressPage() {
  return <ProgressHubContent />;
}
