"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ChevronRight, Plus } from "lucide-react";
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
      <div className="flex items-center justify-between gap-3">
        <h1 className="font-brand text-2xl font-bold text-text-primary" id="routines-title">
          Mis rutinas
        </h1>
        {state.status === "ready" && state.routines.length > 0 ? <CreateRoutineLink label="Crear" variant="header" /> : null}
      </div>
      <p className="mt-1.5 text-sm leading-6 text-text-secondary">
        Organizá y prepará tus entrenamientos.
      </p>

      {state.status === "loading" ? (
        <div className="flex min-h-48 items-center justify-center">
          <Spinner className="size-6 text-primary" label="Cargando rutinas" />
        </div>
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
            No tenés rutinas todavía.
          </h2>
          <p className="mt-3 text-sm leading-6 text-text-secondary">
            Creá tu primera rutina y empezá a organizar tus entrenamientos.
          </p>
          <div className="mt-6">
            <CreateRoutineLink label="Crear mi primera rutina" variant="empty" />
          </div>
        </div>
      ) : (
        <ul className="mt-7 divide-y divide-border/50 border-y border-border/50">
          {state.routines.map((routine) => (
            <li key={routine.id}>
              <Link
                className="group flex min-h-20 min-w-0 items-center gap-3 rounded-control-sm px-1 py-4 transition-colors hover:bg-surface/35 active:bg-surface/60 focus-visible:outline-2 focus-visible:outline-primary"
                href={`/routines/${routine.id}`}
              >
                <div className="min-w-0 flex-1">
                  <h2 className="break-words font-brand text-[1.125rem] font-bold leading-6 text-text-primary">
                    {routine.nombre}
                  </h2>
                  <p className="mt-1 text-sm font-medium leading-5 text-text-secondary">
                    {routine.cantidadDias} {routine.cantidadDias === 1 ? "día" : "días"} · {routine.cantidadEjercicios} {routine.cantidadEjercicios === 1 ? "ejercicio" : "ejercicios"}
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
      className={`inline-flex items-center justify-center gap-1.5 rounded-control-sm text-sm font-semibold transition-colors focus-visible:outline-primary ${variant === "header" ? "min-h-11 px-2 text-primary hover:bg-primary/8 hover:text-text-primary active:bg-primary/12" : "min-h-12 bg-primary-strong px-4 text-text-primary hover:bg-primary active:bg-primary/90"}`}
      href="/routines/new"
    >
      <Plus aria-hidden="true" size={variant === "header" ? 17 : 16} strokeWidth={1.9} />
      {label}
    </Link>
  );
}
