"use client";

import { usePathname, useRouter } from "next/navigation";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import {
  checkTrainingProfileAction,
  type CheckTrainingProfileActionResult,
} from "./check-training-profile.action";
import {
  getDestinationForTrainingProfile,
  type TrainingProfilePresence,
} from "./training-profile-navigation";

type TrainingProfileGateState =
  | CheckTrainingProfileActionResult
  | { status: "checking" | "unknown" };

type TrainingProfileGateContextValue = {
  allowCurrentSummary: boolean;
  check: () => Promise<CheckTrainingProfileActionResult>;
  clearSummaryAllowance: () => void;
  markProfileCreated: () => void;
  recordPresence: (presence: TrainingProfilePresence) => void;
  state: TrainingProfileGateState;
};

const TrainingProfileGateContext =
  createContext<TrainingProfileGateContextValue | null>(null);

export function TrainingProfileGateProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [state, setState] = useState<TrainingProfileGateState>({
    status: "unknown",
  });
  const [allowCurrentSummary, setAllowCurrentSummary] = useState(false);
  const stateRef = useRef(state);
  const pendingCheckRef =
    useRef<Promise<CheckTrainingProfileActionResult> | null>(null);

  const updateState = useCallback((nextState: TrainingProfileGateState) => {
    stateRef.current = nextState;
    setState(nextState);
  }, []);

  const recordPresence = useCallback(
    (presence: TrainingProfilePresence) => {
      setAllowCurrentSummary(false);
      updateState({ presence, status: "resolved" });
    },
    [updateState],
  );

  const markProfileCreated = useCallback(() => {
    setAllowCurrentSummary(true);
    updateState({ presence: "exists", status: "resolved" });
  }, [updateState]);

  const clearSummaryAllowance = useCallback(() => {
    setAllowCurrentSummary(false);
  }, []);

  const check = useCallback(async () => {
    const currentState = stateRef.current;

    if (currentState.status === "resolved") {
      return currentState;
    }

    if (pendingCheckRef.current) {
      return pendingCheckRef.current;
    }

    updateState({ status: "checking" });

    const pendingCheck = checkTrainingProfileAction().catch(
      (): CheckTrainingProfileActionResult => ({ status: "error" }),
    );
    pendingCheckRef.current = pendingCheck;

    const result = await pendingCheck;
    pendingCheckRef.current = null;
    updateState(result);

    return result;
  }, [updateState]);

  return (
    <TrainingProfileGateContext
      value={{
        allowCurrentSummary,
        check,
        clearSummaryAllowance,
        markProfileCreated,
        recordPresence,
        state,
      }}
    >
      {children}
    </TrainingProfileGateContext>
  );
}

export function useTrainingProfileGate() {
  const context = useContext(TrainingProfileGateContext);

  if (!context) {
    throw new Error(
      "useTrainingProfileGate debe utilizarse dentro de TrainingProfileGateProvider.",
    );
  }

  return context;
}

export function usePostAuthTrainingProfileGate() {
  const router = useRouter();
  const { check, recordPresence } = useTrainingProfileGate();
  const [isProfileReady, setIsProfileReady] = useState(false);

  const complete = useCallback(
    ({
      destination,
      presence,
    }: {
      destination: string | null;
      presence: TrainingProfilePresence;
    }) => {
      recordPresence(presence);

      if (destination) {
        router.replace(destination);
        return;
      }

      setIsProfileReady(true);
    },
    [recordPresence, router],
  );

  const retry = useCallback(async () => {
    const result = await check();

    if (result.status !== "resolved") {
      if (result.status === "unauthenticated") {
        router.replace("/login");
      }

      return result.status;
    }

    complete({
      destination: getDestinationForTrainingProfile(result.presence),
      presence: result.presence,
    });

    return "resolved" as const;
  }, [check, complete, router]);

  return { complete, isProfileReady, retry };
}

export function TrainingProfileGate({
  access,
  children,
}: {
  access: "onboarding" | "profile";
  children: ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const {
    allowCurrentSummary,
    check,
    clearSummaryAllowance,
    state,
  } = useTrainingProfileGate();
  const canShowCreatedSummary =
    allowCurrentSummary && pathname === "/onboarding/summary";

  useEffect(() => {
    if (state.status === "unknown") {
      void check();
    }
  }, [check, state.status]);

  useEffect(() => {
    if (allowCurrentSummary && pathname !== "/onboarding/summary") {
      clearSummaryAllowance();
    }
  }, [allowCurrentSummary, clearSummaryAllowance, pathname]);

  useEffect(() => {
    if (state.status === "unauthenticated") {
      router.replace("/login");
      return;
    }

    if (state.status !== "resolved") {
      return;
    }

    if (access === "onboarding" && state.presence === "exists") {
      const destination = getDestinationForTrainingProfile("exists");

      if (destination && !canShowCreatedSummary) {
        router.replace(destination);
      }
      return;
    }

    if (access === "profile" && state.presence === "missing") {
      router.replace("/onboarding");
    }
  }, [access, canShowCreatedSummary, router, state]);

  if (state.status === "error") {
    return <TrainingProfileGateStatus onRetry={() => void check()} type="error" />;
  }

  if (state.status !== "resolved") {
    return <TrainingProfileGateStatus type="loading" />;
  }

  if (access === "onboarding") {
    if (state.presence === "missing" || canShowCreatedSummary) {
      return children;
    }

    const destination = getDestinationForTrainingProfile("exists");

    return destination ? (
      <TrainingProfileGateStatus type="loading" />
    ) : (
      <TrainingProfileGateStatus type="ready" />
    );
  }

  return state.presence === "exists" ? (
    children
  ) : (
    <TrainingProfileGateStatus type="loading" />
  );
}

export function TrainingProfileGateStatus({
  onRetry,
  type,
}: {
  onRetry?: () => void;
  type: "error" | "loading" | "ready";
}) {
  if (type === "loading") {
    return (
      <div className="m-auto flex min-h-64 flex-col items-center justify-center gap-3 text-center">
        <Spinner className="size-6 text-primary" label="Verificando tu perfil" />
        <p className="text-sm text-text-secondary">Verificando tu perfil...</p>
      </div>
    );
  }

  if (type === "ready") {
    return (
      <div className="m-auto flex min-h-64 max-w-sm flex-col items-center justify-center text-center">
        <h1 className="font-brand text-2xl font-bold text-text-primary">
          Tu perfil ya está listo.
        </h1>
        <p className="mt-3 text-sm leading-6 text-text-secondary">
          Entrenate ya está preparado para continuar.
        </p>
      </div>
    );
  }

  return (
    <div className="m-auto flex min-h-64 max-w-sm flex-col items-center justify-center text-center">
      <h1 className="font-brand text-2xl font-bold text-text-primary">
        No pudimos verificar tu perfil.
      </h1>
      <p className="mt-3 text-sm leading-6 text-text-secondary">
        Intentá nuevamente.
      </p>
      <Button className="mt-6 min-w-36" onClick={onRetry} variant="secondary">
        Reintentar
      </Button>
    </div>
  );
}
