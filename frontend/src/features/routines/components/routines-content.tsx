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
        {state.status === "ready" && state.routines.length > 0 ? <CreateRoutineLink label="Crear rutina" withIcon /> : null}
      </div>
      <p className="mt-2 text-sm leading-6 text-text-secondary">
        Organizá y prepará tus entrenamientos.
      </p>

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
        <div className="mt-7 pt-6">
          <h2 className="font-brand text-lg font-bold text-text-primary">
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
        <ul className="mt-7 divide-y divide-border/60 border-b border-border/60">
          {state.routines.map((routine, index) => (
            <li key={routine.id}>
              <Link
                className="flex min-h-21 items-center gap-3 py-4.5 transition-colors hover:bg-surface/40 focus-visible:outline-primary"
                href={`/routines/${routine.id}`}
              >
                <span aria-hidden="true" className="w-8 shrink-0 self-start pt-1 font-brand text-sm font-semibold text-primary/65">
                  {String(index + 1).padStart(2, "0")}
                </span>
                <div className="min-w-0 flex-1">
                  <h2 className="font-brand text-lg font-bold leading-tight text-text-primary">
                    {routine.nombre}
                  </h2>
                  <p className="mt-1 text-xs leading-5 text-text-secondary">
                    {routine.cantidadDias} {routine.cantidadDias === 1 ? "día" : "días"} · {routine.cantidadEjercicios} {routine.cantidadEjercicios === 1 ? "ejercicio" : "ejercicios"}
                  </p>
                </div>
                <ChevronRight aria-hidden="true" className="shrink-0 text-text-secondary/55" size={18} />
              </Link>
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
      className={`inline-flex min-h-10 items-center justify-center gap-1.5 rounded-control-sm border px-3.5 text-sm font-semibold text-primary transition-colors focus-visible:outline-primary ${withIcon ? "border-primary/20 bg-primary/5 hover:border-primary/40 hover:bg-primary/10" : "border-primary/30 bg-primary/10 hover:border-primary/60 hover:bg-primary/15"}`}
      href="/routines/new"
    >
      {withIcon ? <Plus aria-hidden="true" size={16} /> : null}
      {label}
    </Link>
  );
}
