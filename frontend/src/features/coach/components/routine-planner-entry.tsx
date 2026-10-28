"use client";

import { ArrowLeft, ArrowRight } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState, type ReactNode } from "react";
import { Button, buttonClassName } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import {
  environmentLabels,
  experienceLabels,
  objectiveLabels,
} from "@/features/training-profile/shared/training-profile-options";
import type {
  TrainingProfileEquipment,
  TrainingProfileResponse,
} from "@/features/training-profile/shared/training-profile.types";
import {
  getRoutinePlannerProfileAction,
  type GetRoutinePlannerProfileActionResult,
} from "../actions/get-routine-planner-profile.action";

type PlannerEntryState =
  | { status: "error" }
  | { status: "loading" }
  | { profile: TrainingProfileResponse; status: "ready" };

export function RoutinePlannerEntry() {
  const router = useRouter();
  const { invalidateSession, recordPresence } = useTrainingProfileGate();
  const [state, setState] = useState<PlannerEntryState>({ status: "loading" });

  const handleResult = useCallback(
    (result: GetRoutinePlannerProfileActionResult) => {
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }

      if (result.status === "missing") {
        recordPresence("missing");
        router.replace("/onboarding");
        return;
      }

      setState(result.status === "ready" ? result : { status: "error" });
    },
    [invalidateSession, recordPresence, router],
  );

  useEffect(() => {
    let active = true;

    void getRoutinePlannerProfileAction().then(
      (result) => {
        if (active) handleResult(result);
      },
      () => {
        if (active) setState({ status: "error" });
      },
    );

    return () => {
      active = false;
    };
  }, [handleResult]);

  const retry = async () => {
    setState({ status: "loading" });

    try {
      handleResult(await getRoutinePlannerProfileAction());
    } catch {
      setState({ status: "error" });
    }
  };

  return (
    <section
      aria-busy={state.status === "loading"}
      aria-labelledby="routine-planner-title"
      className="mx-auto w-full min-w-0 max-w-xl"
    >
      <header className="max-w-lg">
        <div className="flex min-w-0 items-center gap-1">
          <Link
            aria-label="Volver a Coach"
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors duration-150 hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
            href="/coach"
          >
            <ArrowLeft aria-hidden="true" size={19} />
          </Link>
          <h1
            className="min-w-0 font-brand text-2xl font-bold leading-tight text-text-primary"
            id="routine-planner-title"
          >
            Planificador de rutinas
          </h1>
        </div>
        <p className="mt-1.5 max-w-lg pl-12 text-sm leading-6 text-text-secondary">
          Generamos una propuesta usando tu objetivo, experiencia,
          disponibilidad y equipamiento actuales.
        </p>
      </header>

      {state.status === "loading" ? (
        <PlannerContextSkeleton />
      ) : state.status === "error" ? (
        <div className="mt-8 border-t border-border/70 pt-6" role="alert">
          <p className="text-sm leading-6 text-text-primary">
            No pudimos cargar tu contexto de entrenamiento.
          </p>
          <Button className="mt-4" onClick={() => void retry()} variant="secondary">
            Reintentar
          </Button>
        </div>
      ) : (
        <PlannerContext profile={state.profile} />
      )}
    </section>
  );
}

function PlannerContext({ profile }: { profile: TrainingProfileResponse }) {
  return (
    <div className="mt-9">
      <section aria-labelledby="planner-context-title">
        <h2
          className="font-brand text-xl font-bold leading-7 text-text-primary"
          id="planner-context-title"
        >
          Tu contexto actual
        </h2>

        <dl className="mt-3 border-y border-border/70">
          <ContextRow label="Objetivo" value={objectiveLabels[profile.objetivo]} />
          <ContextRow
            label="Experiencia"
            value={experienceLabels[profile.nivelExperiencia]}
          />
          <ContextRow
            label="D&iacute;as por semana"
            value={`${profile.diasEntrenamientoPorSemana} ${profile.diasEntrenamientoPorSemana === 1 ? "d\u00eda" : "d\u00edas"}`}
          />
          <ContextRow
            label="Duraci&oacute;n"
            value={`${profile.duracionSesionMinutos} min`}
          />
          <ContextRow
            label="Entorno"
            value={environmentLabels[profile.entornoEntrenamiento]}
          />
          <ContextRow
            label="Equipamiento"
            value={<EquipmentValue equipment={profile.equipamientos} />}
          />
        </dl>
      </section>

      <Link
        className={buttonClassName({
          className: "mt-7 [&_svg]:text-thermal-accent",
          fullWidth: true,
          size: "lg",
          variant: "secondary",
        })}
        href="/coach/routines/proposal"
      >
        Generar propuesta
        <ArrowRight aria-hidden="true" />
      </Link>
    </div>
  );
}

function ContextRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="grid min-h-[3.25rem] grid-cols-[minmax(7.5rem,0.8fr)_minmax(0,1.2fr)] items-center gap-4 border-b border-border/60 px-1 py-2.5 last:border-b-0">
      <dt className="text-[0.8125rem] font-semibold leading-5 text-text-secondary">
        {label}
      </dt>
      <dd className="min-w-0 break-words text-right text-[0.9375rem] font-semibold leading-6 text-text-primary">
        {value}
      </dd>
    </div>
  );
}

function PlannerContextSkeleton() {
  return (
    <div
      aria-label="Cargando contexto de entrenamiento"
      className="mt-9 animate-pulse motion-reduce:animate-none"
      role="status"
    >
      <div className="h-6 w-44 rounded bg-surface-elevated" />
      <div className="mt-3 border-y border-border/50">
        {[0, 1, 2, 3, 4, 5].map((item) => (
          <div
            className="flex min-h-[3.25rem] items-center justify-between gap-4 border-b border-border/50 px-1 py-2.5 last:border-b-0"
            key={item}
          >
            <div className="h-3.5 w-24 rounded bg-surface-elevated" />
            <div className="h-4 w-36 rounded bg-surface-elevated" />
          </div>
        ))}
      </div>
      <div className="mt-7 h-[3.25rem] w-full rounded-control bg-surface-elevated" />
    </div>
  );
}

function EquipmentValue({
  equipment,
}: {
  equipment: TrainingProfileEquipment[];
}) {
  if (equipment.length === 0) return <>Sin equipamiento declarado</>;

  const visibleNames = equipment.slice(0, 3).map((item) => item.nombre);
  const remainingCount = equipment.length - visibleNames.length;

  return (
    <span className="inline-flex flex-wrap justify-end gap-x-1">
      <span>{visibleNames.join(", ")}</span>
      {remainingCount > 0 ? (
        <span className="font-medium text-text-muted">+{remainingCount}</span>
      ) : null}
    </span>
  );
}
