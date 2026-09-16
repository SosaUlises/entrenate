"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { getRoutineDetailAction } from "../actions/get-routine-detail.action";
import { useRoutineDraft } from "../context/routine-draft-context";
import { RoutineForm } from "./new-routine-form";

type LoadState = "loading" | "error" | "not-found";

export function EditRoutineContent({ id }: { id: string }) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const { hydratedRoutineId, hydrateDraft } = useRoutineDraft();
  const [loadState, setLoadState] = useState<LoadState>("loading");
  const [requestKey, setRequestKey] = useState(0);

  useEffect(() => {
    if (hydratedRoutineId === id) return;
    let active = true;

    void getRoutineDetailAction(id).then((result) => {
      if (!active) return;
      if (result.status === "ready") {
        hydrateDraft(result.routine);
      } else if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
      } else {
        setLoadState(result.status);
      }
    }, () => {
      if (active) setLoadState("error");
    });

    return () => { active = false; };
  }, [hydratedRoutineId, hydrateDraft, id, invalidateSession, requestKey, router]);

  if (hydratedRoutineId === id) return <RoutineForm routineId={id} />;

  if (loadState === "not-found") {
    return (
      <section className="mx-auto w-full max-w-xl rounded-card border border-border bg-surface p-5">
        <p className="text-sm text-text-primary">No encontramos esta rutina.</p>
        <Link className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-primary focus-visible:outline-primary" href="/routines">
          Volver a mis rutinas
        </Link>
      </section>
    );
  }

  if (loadState === "error") {
    return (
      <section className="mx-auto w-full max-w-xl rounded-card border border-border bg-surface p-5">
        <p className="text-sm text-text-primary">No pudimos cargar la rutina.</p>
        <Button className="mt-5" onClick={() => {
          setLoadState("loading");
          setRequestKey((key) => key + 1);
        }} variant="secondary">Reintentar</Button>
      </section>
    );
  }

  return (
    <div className="flex min-h-48 items-center justify-center">
      <Spinner className="size-6 text-primary" label="Cargando rutina" />
    </div>
  );
}
