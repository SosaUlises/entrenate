"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { ClipboardList, History, Home, LogOut, TrendingUp } from "lucide-react";
import { useCallback, useEffect, useState, type ReactNode } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import {
  getCurrentUserAction,
  type GetCurrentUserActionResult,
} from "@/features/auth/actions/get-current-user.action";
import { logoutAction } from "@/features/auth/actions/logout.action";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";

type UserState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; nombre: string };

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const isExercisesPage = pathname === "/exercises";
  const isRoutinesPage = pathname === "/routines" || pathname.startsWith("/routines/");
  const isContextualPage = isExercisesPage || isRoutinesPage || pathname === "/training";
  const { invalidateSession } = useTrainingProfileGate();
  const [userState, setUserState] = useState<UserState>({ status: "loading" });
  const [isSigningOut, setIsSigningOut] = useState(false);
  const [signOutFailed, setSignOutFailed] = useState(false);

  const handleUserResult = useCallback((result: GetCurrentUserActionResult) => {
    if (result.status === "unauthenticated") {
      invalidateSession();
      router.replace("/login");
      return;
    }

    setUserState(
      result.status === "ready"
        ? { status: "ready", nombre: result.user.nombre }
        : { status: "error" },
    );
  }, [invalidateSession, router]);

  useEffect(() => {
    let isActive = true;

    void getCurrentUserAction().then(
      (result) => {
        if (isActive) {
          handleUserResult(result);
        }
      },
      () => {
        if (isActive) {
          setUserState({ status: "error" });
        }
      },
    );

    return () => {
      isActive = false;
    };
  }, [handleUserResult]);

  const handleLogout = async () => {
    if (isSigningOut) {
      return;
    }

    setIsSigningOut(true);
    setSignOutFailed(false);

    try {
      await logoutAction();
      invalidateSession();
      router.replace("/login");
    } catch {
      setSignOutFailed(true);
      setIsSigningOut(false);
    }
  };

  if (userState.status === "loading") {
    return (
      <div className="grid min-h-dvh place-items-center" role="status">
        <Spinner className="size-6 text-primary" label="Cargando inicio" />
      </div>
    );
  }

  if (userState.status === "error") {
    return (
      <main className="mx-auto flex min-h-dvh w-full max-w-md flex-col items-center justify-center gap-5 px-5 text-center">
        <Alert title="No pudimos cargar tu cuenta." variant="error">
          Revisá tu conexión e intentá nuevamente.
        </Alert>
        <div className="flex gap-3">
          <Button
            onClick={() => {
              setUserState({ status: "loading" });
              void getCurrentUserAction().then(handleUserResult, () => {
                setUserState({ status: "error" });
              });
            }}
            variant="secondary"
          >
            Reintentar
          </Button>
          <Button disabled={isSigningOut} onClick={() => void handleLogout()} variant="text">
            Cerrar sesión
          </Button>
        </div>
        {signOutFailed ? (
          <Alert variant="error">No pudimos cerrar sesión.</Alert>
        ) : null}
      </main>
    );
  }

  const nombre = userState.nombre.trim();
  const initial = nombre.charAt(0).toLocaleUpperCase("es-AR") || "?";

  return (
    <div className="min-h-dvh w-full bg-background">
      <div className="mx-auto w-full max-w-2xl px-8">
        {!isContextualPage ? (
        <header className="flex items-start justify-between gap-4 pt-9 sm:pt-12">
          <div className="min-w-0">
            <h1 className="font-brand text-3xl font-bold leading-tight text-text-primary sm:text-4xl">
              Hola, {nombre}
            </h1>
            <p className="mt-2 text-sm text-text-secondary sm:text-base">
              ¿Listo para entrenar?
            </p>
          </div>

          <details className="group relative shrink-0">
            <summary
              aria-label="Abrir menú de cuenta"
              className="flex size-12 cursor-pointer list-none items-center justify-center rounded-full border border-primary/50 bg-primary/15 font-brand text-lg font-bold text-primary transition-colors marker:hidden hover:border-primary/80 hover:bg-primary/20 focus-visible:outline-primary focus-visible:ring-2 focus-visible:ring-primary/30 [&::-webkit-details-marker]:hidden"
            >
              {initial}
            </summary>
            <div className="absolute right-0 z-20 mt-2 w-48 rounded-control border border-border bg-surface-elevated p-1 shadow-elevated">
              <button
                className="flex min-h-11 w-full items-center gap-3 rounded-control-sm px-3 text-left text-sm font-medium text-text-primary hover:bg-surface"
                disabled={isSigningOut}
                onClick={() => void handleLogout()}
                type="button"
              >
                <LogOut aria-hidden="true" size={18} />
                {isSigningOut ? "Saliendo..." : "Cerrar sesión"}
              </button>
            </div>
          </details>
        </header>
        ) : null}

        {signOutFailed ? (
          <div className="mt-5">
            <Alert variant="error">No pudimos cerrar sesión. Intentá nuevamente.</Alert>
          </div>
        ) : null}

        <main className={`mx-auto w-full pb-[calc(7.5rem+env(safe-area-inset-bottom))] ${isContextualPage ? "pt-5 sm:pt-8" : "pt-10 sm:pt-12"}`}>
          {children}
        </main>
      </div>

      <nav
        aria-label="Navegación principal"
        className="fixed inset-x-0 bottom-0 z-10 border-t border-border-strong bg-surface/95 pb-[env(safe-area-inset-bottom)] backdrop-blur-md"
      >
        <div className="mx-auto grid max-w-2xl grid-cols-4 px-2 sm:px-8">
          <Link
            aria-current={isRoutinesPage ? undefined : "page"}
            className={`flex min-h-18 flex-col items-center justify-center gap-1.5 rounded-control-sm ${isRoutinesPage ? "text-text-secondary hover:text-text-primary" : "bg-primary/10 text-primary"}`}
            href="/home"
          >
            <Home aria-hidden="true" size={22} strokeWidth={2} />
            <span className="text-xs font-semibold">Inicio</span>
          </Link>
          <Link
            aria-current={isRoutinesPage ? "page" : undefined}
            className={`flex min-h-18 flex-col items-center justify-center gap-1.5 rounded-control-sm ${isRoutinesPage ? "bg-primary/10 text-primary" : "text-text-secondary hover:text-text-primary"}`}
            href="/routines"
          >
            <ClipboardList aria-hidden="true" size={22} />
            <span className="text-xs font-semibold">Rutinas</span>
          </Link>
          <DisabledDestination icon={<History size={22} />} label="Historial" />
          <DisabledDestination icon={<TrendingUp size={22} />} label="Progreso" />
        </div>
      </nav>
    </div>
  );
}

function DisabledDestination({ icon, label }: { icon: ReactNode; label: string }) {
  return (
    <span
      aria-disabled="true"
      className="flex min-h-18 flex-col items-center justify-center gap-1.5 text-text-secondary/65"
      title={`${label}: próximamente`}
    >
      <span aria-hidden="true">{icon}</span>
      <span className="text-xs font-medium">{label}</span>
    </span>
  );
}
