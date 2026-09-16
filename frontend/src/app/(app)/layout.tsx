import type { ReactNode } from "react";
import { AppShell } from "@/components/layout/app-shell";
import { TrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";

export default function AuthenticatedLayout({ children }: { children: ReactNode }) {
  return (
    <TrainingProfileGate access="profile">
      <AppShell>{children}</AppShell>
    </TrainingProfileGate>
  );
}
