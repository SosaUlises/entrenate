"use client";

import Image from "next/image";
import Link from "next/link";
import { ArrowUpRight } from "lucide-react";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { getHomeStateAction, type HomeState } from "../actions/get-home-state.action";

type HomeViewState = { status: "loading" } | HomeState;

const primaryLinkClass = "inline-flex min-h-11 items-center gap-2 rounded-control bg-primary-strong px-4 text-sm font-semibold text-text-primary transition-colors hover:bg-primary focus-visible:outline-primary";

export function HomeContent() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<HomeViewState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);

  useEffect(() => {
    let active = true;

    void getHomeStateAction().then(
      (result) => {
        if (!active) return;

        if (result.status === "unauthenticated") {
          invalidateSession();
          router.replace("/login");
          return;
        }

        setState(result);
      },
      () => {
        if (active) setState({ status: "error" });
      },
    );

    return () => {
      active = false;
    };
  }, [invalidateSession, requestKey, router]);

  if (state.status === "loading" || state.status === "unauthenticated") {
    return (
      <div className="grid min-h-48 place-items-center" role="status">
        <Spinner className="size-6 text-primary" label="Cargando tu entrenamiento" />
      </div>
    );
  }

  if (state.status === "error") {
    return (
      <section aria-labelledby="home-error-title" className="py-5">
        <h2 className="font-brand text-xl font-bold text-text-primary" id="home-error-title">
          No pudimos actualizar tu estado
        </h2>
        <p className="mt-2 max-w-sm text-sm leading-6 text-text-secondary">
          No podemos confirmar si tenés un entrenamiento en curso. Intentá nuevamente.
        </p>
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
      </section>
    );
  }

  if (state.status === "active-session") {
    return <ActiveSessionState state={state} />;
  }

  if (state.status === "no-routines") {
    return <NoRoutinesState />;
  }

  return <RoutinesAvailableState routineCount={state.routineCount} />;
}

function ActiveSessionState({ state }: { state: Extract<HomeState, { status: "active-session" }> }) {
  return (
    <section aria-labelledby="active-training-title" className="rounded-card border border-info/20 bg-surface px-6 py-6 sm:px-7">
      <p className="text-xs font-semibold tracking-wide text-info">ENTRENAMIENTO EN CURSO</p>
      <h2 className="mt-3 font-brand text-2xl font-bold leading-tight text-text-primary" id="active-training-title">
        {state.readyToFinish ? "Entrenamiento listo" : state.exerciseName}
      </h2>
      {state.readyToFinish ? (
        <p className="mt-3 max-w-sm text-sm leading-6 text-text-secondary">
          Todas las series objetivo están registradas. Solo falta finalizar la sesión.
        </p>
      ) : (
        <p className="mt-3 text-sm font-semibold text-text-primary">
          Ejercicio {state.exercisePosition} de {state.exerciseCount}
          <span className="text-text-secondary"> · </span>
          Serie {state.setNumber} de {state.targetSetCount}
        </p>
      )}
      <Link className={`${primaryLinkClass} mt-6`} href="/training">
        Continuar entrenamiento
        <ArrowUpRight aria-hidden="true" size={17} />
      </Link>
    </section>
  );
}

function NoRoutinesState() {
  return (
    <div className="space-y-6">
      <section aria-labelledby="first-routine-title" className="py-1">
        <h2 className="font-brand text-2xl font-bold leading-tight text-text-primary" id="first-routine-title">
          Tu entrenamiento empieza acá
        </h2>
        <p className="mt-3 max-w-sm text-sm leading-6 text-text-secondary">
          Creá tu primera rutina para empezar a registrar tus entrenamientos.
        </p>
        <Link className={`${primaryLinkClass} mt-6`} href="/routines/new">
          Crear mi primera rutina
          <ArrowUpRight aria-hidden="true" size={17} />
        </Link>
      </section>

      <section aria-label="GymBro" className="flex items-center gap-4 border-t border-border/60 pt-4">
        <Image
          alt="GymBro"
          className="h-25 w-20 shrink-0 object-contain"
          height={100}
          src="/gymbro/half-body/gymbro-halfbody-power.png"
          width={80}
        />
        <div className="min-w-0">
          <h2 className="font-brand text-base font-bold text-text-primary">Todo listo.</h2>
          <p className="mt-1 text-sm leading-5 text-text-secondary">
            Armá una rutina con tus días y ejercicios para empezar a entrenar.
          </p>
        </div>
      </section>
    </div>
  );
}

function RoutinesAvailableState({ routineCount }: { routineCount: number }) {
  return (
    <section aria-labelledby="available-routines-title" className="py-1">
      <h2 className="font-brand text-2xl font-bold leading-tight text-text-primary" id="available-routines-title">
        Tu entrenamiento
      </h2>
      <p className="mt-3 text-sm font-semibold text-text-primary">
        {routineCount} {routineCount === 1 ? "rutina disponible" : "rutinas disponibles"}
      </p>
      <p className="mt-1 max-w-sm text-sm leading-6 text-text-secondary">
        Elegí una rutina y el día que querés entrenar.
      </p>
      <Link className={`${primaryLinkClass} mt-6`} href="/routines">
        Elegir entrenamiento
        <ArrowUpRight aria-hidden="true" size={17} />
      </Link>
    </section>
  );
}
