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
  const isHomePage = pathname === "/" || pathname === "/home";
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
      <div className="mx-auto w-full max-w-2xl px-4 sm:px-12">
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
        className="fixed bottom-[calc(0.75rem+env(safe-area-inset-bottom))] left-1/2 z-10 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-full border border-border/40 bg-surface-elevated/90 p-1 shadow-sm backdrop-blur-md"
      >
        <div className="grid grid-cols-4 gap-1">
          <Link
            aria-current={isHomePage ? "page" : undefined}
            className={`group flex min-h-12 min-w-0 items-center justify-center rounded-full text-xs font-semibold transition-colors focus-visible:outline-primary ${isHomePage ? "text-primary" : "text-text-primary/75 hover:text-text-primary"}`}
            href="/home"
          >
            <span className={`flex flex-col items-center gap-0.5 rounded-full px-3 py-1 transition-colors ${isHomePage ? "bg-primary/10" : "group-hover:bg-surface"}`}>
              <Home aria-hidden="true" size={18} strokeWidth={2} />
              <span>Inicio</span>
            </span>
          </Link>
          <Link
            aria-current={isRoutinesPage ? "page" : undefined}
            className={`group flex min-h-12 min-w-0 items-center justify-center rounded-full text-xs font-semibold transition-colors focus-visible:outline-primary ${isRoutinesPage ? "text-primary" : "text-text-primary/75 hover:text-text-primary"}`}
            href="/routines"
          >
            <span className={`flex flex-col items-center gap-0.5 rounded-full px-3 py-1 transition-colors ${isRoutinesPage ? "bg-primary/10" : "group-hover:bg-surface"}`}>
              <ClipboardList aria-hidden="true" size={18} strokeWidth={2} />
              <span>Rutinas</span>
            </span>
          </Link>
          <DisabledDestination icon={<History size={18} />} label="Historial" />
          <DisabledDestination icon={<TrendingUp size={18} />} label="Progreso" />
        </div>
      </nav>
    </div>
  );
}

function DisabledDestination({ icon, label }: { icon: ReactNode; label: string }) {
  return (
    <span
      aria-disabled="true"
      className="flex min-h-12 min-w-0 flex-col items-center justify-center gap-0.5 rounded-full text-text-secondary/60"
      title={`${label}: próximamente`}
    >
      <span aria-hidden="true">{icon}</span>
      <span className="text-xs font-medium">{label}</span>
    </span>
  );
}
