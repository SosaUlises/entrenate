import type { Metadata } from "next";
import { CoachHubContent } from "@/features/coach/components/coach-hub-content";

export const metadata: Metadata = {
  title: "Coach | Entrenate",
};

export default function CoachPage() {
  return <CoachHubContent />;
}
