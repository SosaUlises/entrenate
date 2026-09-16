import type { Metadata } from "next";
import { HomeContent } from "@/features/home/components/home-content";

export const metadata: Metadata = {
  title: "Inicio | Entrenate",
};

export default function HomePage() {
  return <HomeContent />;
}
