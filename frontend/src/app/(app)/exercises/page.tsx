import type { Metadata } from "next";
import { ExerciseCatalog } from "@/features/exercises/components/exercise-catalog";

export const metadata: Metadata = {
  title: "Ejercicios | Entrenate",
};

export default function ExercisesPage() {
  return <ExerciseCatalog />;
}
