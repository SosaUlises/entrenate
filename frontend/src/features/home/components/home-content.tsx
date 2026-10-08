"use client";

import { ArrowRight } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { isStaleActiveSession, formatActiveSessionStart } from "@/features/training/active-session-time";
import { ActiveSessionRecoveryDialog } from "@/features/training/components/active-session-recovery-dialog";
import { getHomeStateAction, type HomeState } from "../actions/get-home-state.action";

type HomeViewState = { status: "loading" } | HomeState;

const primaryLinkClass = "flex min-h-[3.25rem] min-w-0 w-full max-w-sm items-center justify-between overflow-hidden rounded-control-sm bg-primary-strong text-[0.9375rem] font-bold text-text-primary transition-[background-color,transform] duration-200 hover:bg-primary active:scale-[0.99] focus-visible:outline-primary";
const sectionLabelClass = "text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.12em] text-text-secondary";

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
      <div className="grid min-h-64 max-w-lg place-items-center" role="status">
        <Spinner className="size-6 text-primary" label="Cargando tu entrenamiento" />
      </div>
    );
  }

  if (state.status === "error") {
    return (
      <section aria-labelledby="home-error-title" className="max-w-lg py-2">
        <p className={sectionLabelClass}>TU ENTRENAMIENTO</p>
        <h2 className="mt-5 max-w-md font-brand text-3xl font-bold leading-tight text-text-primary" id="home-error-title">
          No pudimos actualizar tu estado
        </h2>
        <p className="mt-3 max-w-sm text-[0.9375rem] leading-6 text-text-secondary">
          No podemos confirmar si tenés un entrenamiento en curso. Intentá nuevamente.
        </p>
        <Button
          className="mt-7 min-h-[3.25rem] w-full max-w-sm rounded-control-sm text-[0.9375rem] font-bold"
          onClick={() => {
            setState({ status: "loading" });
            setRequestKey((key) => key + 1);
          }}
        >
          Reintentar
        </Button>
      </section>
    );
  }

  if (state.status === "active-session") {
    return (
      <ActiveSessionState
        onResolved={() => {
          setState({ status: "loading" });
          setRequestKey((key) => key + 1);
        }}
        onUnauthenticated={() => {
          invalidateSession();
          router.replace("/login");
        }}
        state={state}
      />
    );
  }

  if (state.status === "no-routines") {
    return <NoRoutinesState />;
  }

  return <RoutinesAvailableState routineCount={state.routineCount} />;
}

function ActiveSessionState({
  onResolved,
  onUnauthenticated,
  state,
}: {
  onResolved: () => void;
  onUnauthenticated: () => void;
  state: Extract<HomeState, { status: "active-session" }>;
}) {
  const [confirmingCancellation, setConfirmingCancellation] = useState(false);

  if (isStaleActiveSession(state.horaInicio)) {
    return (
      <section aria-labelledby="active-training-title" className="max-w-lg py-2">
        <p className="text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.12em] text-info">
          ENTRENAMIENTO PENDIENTE
        </p>
        <h2 className="mt-5 break-words font-brand text-3xl font-bold leading-[1.08] text-text-primary sm:text-4xl" id="active-training-title">
          {state.exerciseName ?? "Tu entrenamiento"}
        </h2>
        <p className="mt-3 text-[0.9375rem] font-medium leading-6 text-text-primary">
          {formatActiveSessionStart(state.horaInicio)}
        </p>
        <p className="mt-1 text-sm leading-6 text-text-secondary">
          {state.registeredSetCount} {state.registeredSetCount === 1 ? "serie registrada" : "series registradas"}
        </p>
        <PrimaryLink className="mt-7" href="/training">
          Continuar entrenamiento
        </PrimaryLink>
        <button
          className="mt-2 inline-flex min-h-11 items-center text-sm font-semibold text-text-secondary transition-colors hover:text-error focus-visible:outline-primary"
          onClick={() => setConfirmingCancellation(true)}
          type="button"
        >
          Cancelar sesión
        </button>
        {confirmingCancellation ? (
          <ActiveSessionRecoveryDialog
            initialStep="cancel"
            onClose={() => setConfirmingCancellation(false)}
            onResolved={onResolved}
            onUnauthenticated={onUnauthenticated}
            session={{ id: state.sessionId, horaInicio: state.horaInicio }}
          />
        ) : null}
      </section>
    );
  }

  if (state.readyToFinish) {
    return (
      <section aria-labelledby="active-training-title" className="max-w-lg py-2">
        <p className="text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.12em] text-success">
          ENTRENAMIENTO LISTO
        </p>
        <h2 className="mt-5 font-brand text-4xl font-bold leading-[1.05] text-text-primary" id="active-training-title">
          Objetivos completados.
        </h2>
        <p className="mt-4 max-w-sm text-[0.9375rem] leading-6 text-text-secondary">
          Tu sesión está lista para finalizar.
        </p>
        <PrimaryLink className="mt-8" href="/training">
          Continuar entrenamiento
        </PrimaryLink>
        <HistoryLink />
      </section>
    );
  }

  return (
    <section aria-labelledby="active-training-title" className="max-w-lg py-2">
      <p className="text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.12em] text-info">
        ENTRENAMIENTO EN CURSO
      </p>
      <h2 className="mt-5 break-words font-brand text-3xl font-bold leading-[1.08] text-text-primary sm:text-4xl" id="active-training-title">
        {state.exerciseName}
      </h2>

      <dl className="mt-7 grid max-w-sm grid-cols-2 border-y border-border/60 py-4">
        <div className="min-w-0 border-r border-border/60 pr-4">
          <dt className="text-xs font-semibold uppercase leading-4 tracking-[0.1em] text-text-secondary">EJERCICIO</dt>
          <dd className="mt-2 font-brand text-3xl font-bold leading-none tabular-nums text-info">
            {formatMetric(state.exercisePosition)} <span className="text-xl text-text-secondary">/ {formatMetric(state.exerciseCount)}</span>
          </dd>
        </div>
        <div className="min-w-0 pl-5">
          <dt className="text-xs font-semibold uppercase leading-4 tracking-[0.1em] text-text-secondary">SERIE</dt>
          <dd className="mt-2 font-brand text-3xl font-bold leading-none tabular-nums text-info">
            {formatMetric(state.setNumber)} <span className="text-xl text-text-secondary">/ {formatMetric(state.targetSetCount)}</span>
          </dd>
        </div>
      </dl>

      <PrimaryLink className="mt-8" href="/training">
        Continuar entrenamiento
      </PrimaryLink>
      <HistoryLink />
    </section>
  );
}

function NoRoutinesState() {
  return (
    <section aria-labelledby="first-routine-title" className="min-w-0 w-full max-w-lg overflow-x-clip py-2">
      <p className={sectionLabelClass}>TU ENTRENAMIENTO</p>
      <div className="mt-5 flex min-w-0 w-full max-w-full items-end gap-2 sm:gap-5">
        <div className="min-w-0 flex-1 pb-1">
          <h2 className="font-brand text-4xl font-bold leading-none text-text-primary" id="first-routine-title">
            Empecemos.
          </h2>
          <p className="mt-4 break-words text-[0.9375rem] leading-6 text-text-secondary">
            Creá tu primera rutina para registrar tus entrenamientos.
          </p>
        </div>
        <div className="w-22 max-w-[28%] shrink-0 sm:w-28">
          <Image
            alt="GymBro listo para ayudarte a empezar"
            className="block h-auto max-h-28 w-full object-contain object-bottom sm:max-h-36"
            height={1402}
            priority
            src="/gymbro/half-body/gymbro-halfbody-power.png"
            width={1122}
          />
        </div>
      </div>

      <PrimaryLink className="mt-6 max-w-full" compactOnMobile href="/routines/new">
        Crear mi primera rutina
      </PrimaryLink>
      <HistoryLink />
    </section>
  );
}

function RoutinesAvailableState({ routineCount }: { routineCount: number }) {
  return (
    <section aria-labelledby="available-routines-title" className="max-w-lg py-2">
      <p className={sectionLabelClass}>TU ENTRENAMIENTO</p>

      <div className="mt-6 flex min-w-0 items-end gap-4">
        <p className="shrink-0 font-brand text-[4.5rem] font-extrabold leading-[0.8] tabular-nums text-text-primary">
          {formatMetric(routineCount)}
        </p>
        <h2 className="max-w-40 pb-0.5 text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.1em] text-info" id="available-routines-title">
          {routineCount === 1 ? "Rutina disponible" : "Rutinas disponibles"}
        </h2>
      </div>

      <p className="mt-7 max-w-xs text-[0.9375rem] leading-6 text-text-secondary">
        Elegí una rutina y el día que querés entrenar.
      </p>
      <PrimaryLink className="mt-8" href="/routines">
        Elegir entrenamiento
      </PrimaryLink>
      <HistoryLink />
    </section>
  );
}

function HistoryLink() {
  return (
    <Link className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-text-secondary transition-colors hover:text-primary focus-visible:outline-primary" href="/history">
      Ver historial
    </Link>
  );
}

function PrimaryLink({ children, className, compactOnMobile = false, href }: { children: string; className?: string; compactOnMobile?: boolean; href: string }) {
  return (
    <Link className={`${primaryLinkClass} ${compactOnMobile ? "gap-2 px-3 sm:gap-4 sm:px-5" : "gap-4 px-5"} ${className ?? ""}`} href={href}>
      <span className="min-w-0 break-words leading-5">{children}</span>
      <ArrowRight aria-hidden="true" className="shrink-0" size={18} strokeWidth={2} />
    </Link>
  );
}

function formatMetric(value: number): string {
  return String(value).padStart(2, "0");
}
