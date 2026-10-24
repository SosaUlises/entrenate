import type { Metadata } from "next";
import { RoutinesContent } from "@/features/routines/components/routines-content";

export const metadata: Metadata = {
  title: "Mis rutinas | Entrenate",
};

export default function RoutinesPage() {
  return <RoutinesContent />;
}
