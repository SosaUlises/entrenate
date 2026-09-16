"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ChevronRight, ClipboardList, Plus } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import {
  getRoutinesAction,
  type GetRoutinesActionResult,
} from "../actions/get-routines.action";
import type { RoutineListItem } from "../types/routine.types";

type RoutinesState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; routines: RoutineListItem[] };

export function RoutinesContent() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<RoutinesState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);

  useEffect(() => {
    let active = true;

    const handleResult = (result: GetRoutinesActionResult) => {
      if (!active) return;

      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }

      setState(result);
    };

    void getRoutinesAction().then(handleResult, () => {
      if (active) setState({ status: "error" });
    });

    return () => {
      active = false;
    };
  }, [invalidateSession, requestKey, router]);

  return (
    <section aria-labelledby="routines-title" className="mx-auto w-full max-w-xl">
      <div className="flex flex-wrap items-start justify-between gap-5">
        <div>
          <h1 className="font-brand text-2xl font-bold text-text-primary" id="routines-title">
            Mis rutinas
          </h1>
          <p className="mt-2 text-sm leading-6 text-text-secondary">
            Organizá y prepará tus entrenamientos.
          </p>
        </div>
        {state.status === "ready" && state.routines.length > 0 ? (
          <CreateRoutineLink label="Crear rutina" withIcon />
        ) : null}
      </div>

      {state.status === "loading" ? (
        <div className="flex min-h-48 items-center justify-center">
          <Spinner className="size-6 text-primary" label="Cargando rutinas" />
        </div>
      ) : state.status === "error" ? (
        <div className="mt-8 rounded-card border border-border bg-surface p-5">
          <p className="text-sm text-text-primary">No pudimos cargar tus rutinas.</p>
          <Button
            className="mt-5"
            onClick={() => {
              setState({ status: "loading" });
              setRequestKey((key) => key + 1);
            }}
            variant="secondary"
          >
            Reintentar
          </Button>
        </div>
      ) : state.routines.length === 0 ? (
        <div className="mt-6 rounded-card border border-border/60 bg-surface/45 px-4 py-5 sm:px-5">
          <ClipboardList aria-hidden="true" className="text-primary/60" size={19} />
          <h2 className="mt-3 font-brand text-lg font-bold text-text-primary">
            Todavía no tenés rutinas
          </h2>
          <p className="mt-1 max-w-sm text-sm leading-6 text-text-secondary">
            Creá tu primera rutina y empezá a organizar tus entrenamientos.
          </p>
          <div className="mt-4">
            <CreateRoutineLink label="Crear mi primera rutina" />
          </div>
        </div>
      ) : (
        <ul className="mt-8 space-y-3">
          {state.routines.map((routine) => (
            <li
              className="flex min-h-22 items-center justify-between gap-4 rounded-card border border-border bg-surface px-5 py-4"
              key={routine.id}
            >
              <div className="min-w-0">
                <h2 className="font-brand text-base font-bold text-text-primary">
                  {routine.nombre}
                </h2>
                <p className="mt-1 text-sm text-text-secondary">
                  {routine.cantidadEjercicios} {routine.cantidadEjercicios === 1 ? "ejercicio" : "ejercicios"}
                </p>
              </div>
              <ChevronRight aria-hidden="true" className="shrink-0 text-text-secondary/65" size={20} />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function CreateRoutineLink({ label, withIcon = false }: { label: string; withIcon?: boolean }) {
  return (
    <Link
      className="inline-flex min-h-10 items-center justify-center gap-1.5 rounded-control-sm border border-primary/30 bg-primary/10 px-3.5 text-sm font-semibold text-primary transition-colors hover:border-primary/60 hover:bg-primary/15 focus-visible:outline-primary"
      href="/routines/new"
    >
      {withIcon ? <Plus aria-hidden="true" size={16} /> : null}
      {label}
    </Link>
  );
}
