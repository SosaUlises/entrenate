"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ChevronRight, Plus } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
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
      <header className="sm:flex sm:items-start sm:justify-between sm:gap-6">
        <div className="min-w-0">
          <h1 className="font-brand text-2xl font-bold text-text-primary" id="routines-title">
            Mis rutinas
          </h1>
          <p className="mt-1.5 text-sm leading-6 text-text-secondary">
            Organizá y prepará tus entrenamientos.
          </p>
        </div>
        {state.status === "ready" && state.routines.length > 0 ? (
          <div className="mt-4 sm:mt-0">
            <CreateRoutineLink label="Nueva rutina" variant="header" />
          </div>
        ) : null}
      </header>

      {state.status === "loading" ? (
        <RoutinesSkeleton />
      ) : state.status === "error" ? (
        <div className="mt-8">
          <p className="text-sm text-text-primary">No pudimos cargar tus rutinas.</p>
          <Button
            className="mt-4"
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
        <div className="mt-10 max-w-sm">
          <h2 className="font-brand text-2xl font-bold leading-tight text-text-primary">
            Todavía no tenés rutinas.
          </h2>
          <p className="mt-3 text-sm leading-6 text-text-secondary">
            Creá una para organizar tus días y ejercicios.
          </p>
          <div className="mt-6">
            <CreateRoutineLink label="Crear mi primera rutina" variant="empty" />
          </div>
        </div>
      ) : (
        <ul className="mt-7 divide-y divide-border/60 border-y border-border/60">
          {state.routines.map((routine) => (
            <li key={routine.id}>
              <Link
                className="group flex min-h-24 min-w-0 items-center gap-3 rounded-control-sm px-1 py-5 transition-[background-color,transform] duration-150 hover:bg-surface/30 active:scale-[0.99] active:bg-surface/50 focus-visible:outline-2 focus-visible:outline-primary motion-reduce:transition-none motion-reduce:active:scale-100"
                href={`/routines/${routine.id}`}
              >
                <div className="min-w-0 flex-1">
                  <h2 className="break-words font-brand text-[1.125rem] font-bold leading-6 text-text-primary">
                    {routine.nombre}
                  </h2>
                  <p className="mt-1 text-[0.8125rem] font-semibold leading-5 text-text-secondary">
                    {routine.cantidadDias} {routine.cantidadDias === 1 ? "día" : "días"}
                  </p>
                  <p className="mt-0.5 line-clamp-2 break-words text-[0.8125rem] leading-5 text-text-secondary">
                    {formatDayNames(routine.nombresDias)}
                  </p>
                </div>
                <span aria-hidden="true" className="flex size-11 shrink-0 items-center justify-center text-text-secondary/75 transition-colors group-hover:text-primary group-active:text-primary">
                  <ChevronRight size={20} strokeWidth={1.8} />
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function CreateRoutineLink({ label, variant }: { label: string; variant: "header" | "empty" }) {
  return (
    <Link
      className={`inline-flex items-center justify-center gap-1.5 rounded-control-sm bg-primary-strong text-sm font-semibold text-text-primary transition-colors hover:bg-primary active:bg-primary/90 focus-visible:outline-primary ${variant === "header" ? "min-h-11 px-3" : "min-h-12 px-4"}`}
      href="/routines/new"
    >
      <Plus aria-hidden="true" size={variant === "header" ? 17 : 16} strokeWidth={1.9} />
      {label}
    </Link>
  );
}

function formatDayNames(dayNames: string[]): string {
  if (dayNames.length === 0) return "Sin días configurados";
  if (dayNames.length <= 4) return dayNames.join(" · ");
  return `${dayNames.slice(0, 4).join(" · ")} · +${dayNames.length - 4}`;
}

function RoutinesSkeleton() {
  return (
    <div aria-label="Cargando rutinas" className="mt-7 animate-pulse border-y border-border/50" role="status">
      {[0, 1, 2].map((item) => (
        <div className="flex min-h-24 items-center gap-3 border-b border-border/50 py-5 last:border-b-0" key={item}>
          <div className="min-w-0 flex-1">
            <div className="h-5 w-2/3 rounded bg-surface-elevated" />
            <div className="mt-2 h-3 w-16 rounded bg-surface-elevated" />
            <div className="mt-2 h-3 w-4/5 rounded bg-surface-elevated" />
          </div>
          <div className="size-5 rounded bg-surface-elevated" />
        </div>
      ))}
    </div>
  );
}
