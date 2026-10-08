"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { Spinner } from "@/components/ui/spinner";
import { useAuthenticatedUser } from "@/features/auth/context/authenticated-user-context";
import { RoutineDraftRecoveryDialog } from "../components/routine-draft-recovery-dialog";
import {
  clearRoutineDraft,
  hasMeaningfulRoutineDraft,
  readRoutineDraft,
  serializeRoutineDraft,
  writeRoutineDraft,
  type StoredRoutineDraft,
} from "../routine-draft-storage";
import { createInitialRoutineDraft, RoutineDraftProvider, useRoutineDraft } from "./routine-draft-context";

type RoutineDraftPersistenceContextValue = {
  clearPersistedDraft: () => void;
};

const RoutineDraftPersistenceContext = createContext<RoutineDraftPersistenceContextValue>({
  clearPersistedDraft: () => undefined,
});

export function CreateRoutineDraftProvider({ children }: { children: ReactNode }) {
  return (
    <RoutineDraftProvider>
      <RoutineDraftPersistenceController>{children}</RoutineDraftPersistenceController>
    </RoutineDraftProvider>
  );
}

export function useRoutineDraftPersistence(): RoutineDraftPersistenceContextValue {
  return useContext(RoutineDraftPersistenceContext);
}

function RoutineDraftPersistenceController({ children }: { children: ReactNode }) {
  const { userId } = useAuthenticatedUser();
  const { nombre, descripcion, dias, replaceDraft, resetDraft } = useRoutineDraft();
  const [initialized, setInitialized] = useState(false);
  const [pendingRecovery, setPendingRecovery] = useState<StoredRoutineDraft | null>(null);
  const lastPersistedDraftRef = useRef<string | null>(null);
  const draft = useMemo(() => ({ nombre, descripcion, dias }), [nombre, descripcion, dias]);
  const initialDraftRef = useRef(draft);

  useEffect(() => {
    let active = true;

    queueMicrotask(() => {
      if (!active) return;

      const storedDraft = readRoutineDraft(userId);
      if (storedDraft) {
        lastPersistedDraftRef.current = serializeRoutineDraft(storedDraft.draft);
        setPendingRecovery(storedDraft);
      } else {
        lastPersistedDraftRef.current = serializeRoutineDraft(initialDraftRef.current);
      }
      setInitialized(true);
    });

    return () => {
      active = false;
    };
  }, [userId]);

  useEffect(() => {
    if (!initialized || pendingRecovery) return;

    const serializedDraft = serializeRoutineDraft(draft);
    if (serializedDraft === lastPersistedDraftRef.current) return;

    if (!hasMeaningfulRoutineDraft(draft)) {
      clearRoutineDraft(userId);
      lastPersistedDraftRef.current = serializedDraft;
      return;
    }

    if (writeRoutineDraft(userId, draft)) {
      lastPersistedDraftRef.current = serializedDraft;
    }
  }, [draft, initialized, pendingRecovery, userId]);

  const clearPersistedDraft = useCallback(() => {
    clearRoutineDraft(userId);
    lastPersistedDraftRef.current = null;
  }, [userId]);

  const persistenceValue = useMemo(
    () => ({ clearPersistedDraft }),
    [clearPersistedDraft],
  );

  function focusRoutineName() {
    requestAnimationFrame(() => document.getElementById("routine-name")?.focus());
  }

  if (!initialized) {
    return (
      <div className="grid min-h-48 place-items-center" role="status">
        <Spinner className="size-6 text-primary" label="Preparando nueva rutina" />
      </div>
    );
  }

  if (pendingRecovery) {
    return (
      <>
        <div className="min-h-48" aria-hidden="true" />
        <RoutineDraftRecoveryDialog
          onContinue={() => {
            lastPersistedDraftRef.current = serializeRoutineDraft(pendingRecovery.draft);
            replaceDraft(pendingRecovery.draft);
            setPendingRecovery(null);
            focusRoutineName();
          }}
          onDiscard={() => {
            const initialDraft = createInitialRoutineDraft();
            clearRoutineDraft(userId);
            lastPersistedDraftRef.current = serializeRoutineDraft(initialDraft);
            resetDraft();
            setPendingRecovery(null);
            focusRoutineName();
          }}
          storedDraft={pendingRecovery}
        />
      </>
    );
  }

  return (
    <RoutineDraftPersistenceContext.Provider value={persistenceValue}>
      {children}
    </RoutineDraftPersistenceContext.Provider>
  );
}
