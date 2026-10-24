import type { Metadata } from "next";
import { ProgressCoachEntry } from "@/features/coach/components/progress-coach-entry";

export const metadata: Metadata = {
  title: "Coach de progreso | Entrenate",
};

export default function ProgressCoachPage() {
  return <ProgressCoachEntry />;
}
