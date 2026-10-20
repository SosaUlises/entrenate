"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { ClipboardList, Dumbbell, Home, LogOut, TrendingUp, UserRound } from "lucide-react";
import { useCallback, useEffect, useState, type ReactNode } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import {
  getCurrentUserAction,
  type GetCurrentUserActionResult,
} from "@/features/auth/actions/get-current-user.action";
import { logoutAction } from "@/features/auth/actions/logout.action";
import { AuthenticatedUserProvider } from "@/features/auth/context/authenticated-user-context";
import type { CurrentUser } from "@/features/auth/types/auth.types";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";

type UserState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; user: CurrentUser };

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const isHomePage = pathname === "/" || pathname === "/home";
  const isExerciseProgressPage = /^\/exercises\/[^/]+\/progress$/.test(pathname);
  const isProgressPage = pathname === "/progress" || isExerciseProgressPage;
  const isExercisesPage = !isExerciseProgressPage && (pathname === "/exercises" || pathname.startsWith("/exercises/"));
  const isRoutinesPage = pathname === "/routines" || pathname.startsWith("/routines/");
  const isTrainingPage = pathname === "/training" || pathname.startsWith("/training/");
  const isHistoryPage = pathname === "/history" || pathname.startsWith("/history/");
  const isHistoryDetailPage = /^\/history\/[^/]+$/.test(pathname);
  const isProfilePage = pathname === "/profile" || pathname.startsWith("/profile/");
  const isRoutineExercisePickerPage = pathname === "/routines/new/exercises" || /^\/routines\/[^/]+\/edit\/exercises$/.test(pathname);
  const isContextualPage = isExercisesPage || isRoutinesPage || isTrainingPage || isHistoryPage || isProgressPage || isProfilePage;
  const showBottomNav = !isTrainingPage && !isRoutineExercisePickerPage && !isHistoryDetailPage;
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
        ? { status: "ready", user: result.user }
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

  const nombre = userState.user.nombre.trim();
  const initial = nombre.charAt(0).toLocaleUpperCase("es-AR") || "?";

  return (
    <AuthenticatedUserProvider user={userState.user}>
    <div className={`min-h-dvh w-full bg-background ${isTrainingPage ? "overflow-x-clip" : ""}`}>
      <div className="mx-auto min-w-0 w-full max-w-2xl px-4 sm:px-12">
        {!isContextualPage ? (
        <header className="flex items-center justify-between gap-4 pt-9 sm:pt-12">
          <h1 className="min-w-0 break-words font-brand text-2xl font-bold leading-tight text-text-primary sm:text-3xl">
            Hola, {nombre}.
          </h1>

          <details className="group relative shrink-0">
            <summary
              aria-label="Abrir menú de cuenta"
              className="flex size-12 cursor-pointer list-none items-center justify-center rounded-full border border-border-strong bg-surface-interactive font-brand text-lg font-bold text-text-primary transition-colors marker:hidden hover:bg-surface-active focus-visible:outline-focus [&::-webkit-details-marker]:hidden"
            >
              {initial}
            </summary>
            <div className="absolute right-0 z-20 mt-2 w-48 rounded-card border border-border bg-surface-elevated p-1 shadow-elevated">
              <Link
                className="flex min-h-11 w-full items-center gap-3 rounded-control-sm px-3 text-left text-sm font-medium text-text-primary hover:bg-surface-active focus-visible:outline-focus"
                href="/profile"
              >
                <UserRound aria-hidden="true" size={18} />
                Perfil
              </Link>
              <button
                className="flex min-h-11 w-full items-center gap-3 rounded-control-sm px-3 text-left text-sm font-medium text-text-primary hover:bg-surface-active focus-visible:outline-focus"
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

        <main className={`mx-auto min-w-0 w-full max-w-full ${isTrainingPage || isHistoryDetailPage ? "pb-[calc(2rem+env(safe-area-inset-bottom))]" : "pb-[calc(7.5rem+env(safe-area-inset-bottom))]"} ${isContextualPage ? "pt-5 sm:pt-8" : "pt-8 sm:pt-10"}`}>
          {children}
        </main>
      </div>

      {showBottomNav ? <nav
        aria-label="Navegación principal"
        className="fixed bottom-[calc(0.75rem+env(safe-area-inset-bottom))] left-1/2 z-10 w-[calc(100%-2rem)] max-w-xl -translate-x-1/2 rounded-container border border-border bg-surface-elevated p-1 shadow-elevated"
      >
        <div className="grid grid-cols-4 gap-1">
          <Link
            aria-current={isHomePage ? "page" : undefined}
            className={`group relative flex min-h-12 min-w-0 items-center justify-center rounded-control-sm text-[0.8125rem] font-semibold transition-colors focus-visible:outline-focus ${isHomePage ? "text-text-primary after:absolute after:bottom-0.5 after:left-1/2 after:h-0.5 after:w-4 after:-translate-x-1/2 after:rounded-full after:bg-thermal-accent" : "text-text-secondary hover:bg-surface-active hover:text-text-primary"}`}
            href="/home"
          >
            <span className="flex flex-col items-center gap-0.5 px-3 py-1">
              <Home aria-hidden="true" size={18} strokeWidth={2} />
              <span>Inicio</span>
            </span>
          </Link>
          <Link
            aria-current={isRoutinesPage ? "page" : undefined}
            className={`group relative flex min-h-12 min-w-0 items-center justify-center rounded-control-sm text-[0.8125rem] font-semibold transition-colors focus-visible:outline-focus ${isRoutinesPage ? "text-text-primary after:absolute after:bottom-0.5 after:left-1/2 after:h-0.5 after:w-4 after:-translate-x-1/2 after:rounded-full after:bg-thermal-accent" : "text-text-secondary hover:bg-surface-active hover:text-text-primary"}`}
            href="/routines"
          >
            <span className="flex flex-col items-center gap-0.5 px-3 py-1">
              <ClipboardList aria-hidden="true" size={18} strokeWidth={2} />
              <span>Rutinas</span>
            </span>
          </Link>
          <Link
            aria-current={isProgressPage ? "page" : undefined}
            className={`group relative flex min-h-12 min-w-0 items-center justify-center rounded-control-sm text-[0.8125rem] font-semibold transition-colors focus-visible:outline-focus ${isProgressPage ? "text-text-primary after:absolute after:bottom-0.5 after:left-1/2 after:h-0.5 after:w-4 after:-translate-x-1/2 after:rounded-full after:bg-thermal-accent" : "text-text-secondary hover:bg-surface-active hover:text-text-primary"}`}
            href="/progress"
          >
            <span className="flex flex-col items-center gap-0.5 px-2 py-1 sm:px-3">
              <TrendingUp aria-hidden="true" size={18} strokeWidth={2} />
              <span>Progreso</span>
            </span>
          </Link>
          <Link
            aria-current={isExercisesPage ? "page" : undefined}
            className={`group relative flex min-h-12 min-w-0 items-center justify-center rounded-control-sm text-[0.8125rem] font-semibold transition-colors focus-visible:outline-focus ${isExercisesPage ? "text-text-primary after:absolute after:bottom-0.5 after:left-1/2 after:h-0.5 after:w-4 after:-translate-x-1/2 after:rounded-full after:bg-thermal-accent" : "text-text-secondary hover:bg-surface-active hover:text-text-primary"}`}
            href="/exercises"
          >
            <span className="flex flex-col items-center gap-0.5 px-2 py-1 sm:px-3">
              <Dumbbell aria-hidden="true" size={18} strokeWidth={2} />
              <span>Ejercicios</span>
            </span>
          </Link>
        </div>
      </nav> : null}
    </div>
    </AuthenticatedUserProvider>
  );
}
