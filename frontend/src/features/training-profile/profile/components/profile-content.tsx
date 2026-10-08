"use client";

import Link from "next/link";
import { ChevronLeft, ChevronRight, KeyRound, LogOut, RefreshCw } from "lucide-react";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { logoutAction } from "@/features/auth/actions/logout.action";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import {
  environmentLabels,
  experienceLabels,
  formatPreferredDays,
  formatWeight,
  objectiveLabels,
  sexLabels,
} from "../../shared/training-profile-options";
import type {
  TrainingProfileResponse,
  UpdateTrainingProfileRequest,
} from "../../shared/training-profile.types";
import { getProfilePageAction } from "../actions/get-profile-page.action";
import { updateTrainingProfileAction } from "../actions/update-training-profile.action";
import { ProfileEditorDialog, type ProfileEditor } from "./profile-editor-dialog";

type ReadyData = Extract<
  Awaited<ReturnType<typeof getProfilePageAction>>,
  { status: "ready" }
>;

type LoadState =
  | { status: "error" }
  | { status: "loading" }
  | ({ status: "ready" } & Omit<ReadyData, "status">);

export type ProfilePatch = Partial<
  Pick<
    UpdateTrainingProfileRequest,
    | "diasEntrenamientoPorSemana"
    | "diasPreferidos"
    | "duracionSesionMinutos"
    | "edad"
    | "entornoEntrenamiento"
    | "nivelExperiencia"
    | "objetivo"
    | "pesoKg"
    | "sexo"
  >
> & { equipamientoIds?: string[] };

export type ProfileSaveResult = { message?: string; ok: boolean };

export function ProfileContent() {
  const router = useRouter();
  const { invalidateSession, recordPresence } = useTrainingProfileGate();
  const [state, setState] = useState<LoadState>({ status: "loading" });
  const [activeEditor, setActiveEditor] = useState<ProfileEditor | null>(null);
  const [announcement, setAnnouncement] = useState("");
  const [isSigningOut, setIsSigningOut] = useState(false);
  const [logoutError, setLogoutError] = useState(false);

  const handleLoadResult = useCallback(
    (result: Awaited<ReturnType<typeof getProfilePageAction>>) => {
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

    void getProfilePageAction().then((result) => {
      if (active) handleLoadResult(result);
    });

    return () => {
      active = false;
    };
  }, [handleLoadResult]);

  const retry = async () => {
    setState({ status: "loading" });
    handleLoadResult(await getProfilePageAction());
  };

  const handleLogout = async () => {
    if (isSigningOut) return;
    setIsSigningOut(true);
    setLogoutError(false);

    try {
      await logoutAction();
      invalidateSession();
      router.replace("/login");
    } catch {
      setLogoutError(true);
      setIsSigningOut(false);
    }
  };

  if (state.status === "loading") return <ProfileSkeleton />;

  if (state.status === "error") {
    return (
      <div className="mx-auto flex min-h-[55dvh] max-w-sm flex-col items-center justify-center text-center">
        <h1 className="font-brand text-2xl font-bold text-text-primary">
          No pudimos cargar tu perfil.
        </h1>
        <p className="mt-3 text-sm leading-6 text-text-secondary">
          Revisá tu conexión e intentá nuevamente.
        </p>
        <Button className="mt-6" onClick={() => void retry()} variant="secondary">
          <RefreshCw aria-hidden="true" size={17} />
          Reintentar
        </Button>
      </div>
    );
  }

  const { equipment, profile, user } = state;
  const activeEquipmentIds = new Set(equipment?.map((item) => item.id) ?? []);
  const unavailableEquipment = equipment
    ? profile.equipamientos.filter((item) => !activeEquipmentIds.has(item.id))
    : [];
  const initial = user.nombre.trim().charAt(0).toLocaleUpperCase("es-AR") || "?";

  const saveProfile = async (patch: ProfilePatch): Promise<ProfileSaveResult> => {
    const changesEquipment = patch.equipamientoIds !== undefined;

    if (equipment === null && profile.equipamientos.length > 0 && !changesEquipment) {
      return {
        message:
          "Necesitamos verificar tu equipamiento antes de guardar. Abrí Equipamiento y reintentá la carga.",
        ok: false,
      };
    }

    if (unavailableEquipment.length > 0 && !changesEquipment) {
      return {
        message:
          "Tenés equipamiento que ya no está disponible. Revisalo antes de guardar otros cambios.",
        ok: false,
      };
    }

    const equipmentIds =
      patch.equipamientoIds ?? profile.equipamientos.map((item) => item.id);
    const request: UpdateTrainingProfileRequest = {
      diasEntrenamientoPorSemana:
        patch.diasEntrenamientoPorSemana ?? profile.diasEntrenamientoPorSemana,
      diasPreferidos: patch.diasPreferidos ?? profile.diasPreferidos,
      duracionSesionMinutos:
        patch.duracionSesionMinutos ?? profile.duracionSesionMinutos,
      edad: patch.edad ?? profile.edad,
      entornoEntrenamiento:
        patch.entornoEntrenamiento ?? profile.entornoEntrenamiento,
      equipamientoIds: equipmentIds,
      nivelExperiencia: patch.nivelExperiencia ?? profile.nivelExperiencia,
      objetivo: patch.objetivo ?? profile.objetivo,
      pesoKg: patch.pesoKg !== undefined ? patch.pesoKg : profile.pesoKg,
      sexo: patch.sexo !== undefined ? patch.sexo : profile.sexo,
    };
    const result = await updateTrainingProfileAction(request);

    if (!result.ok) {
      if (result.kind === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
      } else if (result.kind === "missing") {
        recordPresence("missing");
        router.replace("/onboarding");
      }

      return { message: result.message, ok: false };
    }

    const equipmentById = new Map([
      ...profile.equipamientos.map((item) => [item.id, item] as const),
      ...(equipment ?? []).map((item) => [item.id, item] as const),
    ]);
    const nextProfile: TrainingProfileResponse = {
      ...profile,
      ...patch,
      equipamientos: equipmentIds.flatMap((id) => {
        const item = equipmentById.get(id);
        return item ? [item] : [];
      }),
    };

    setState((current) =>
      current.status === "ready" ? { ...current, profile: nextProfile } : current,
    );
    setAnnouncement("Cambios guardados.");
    return { ok: true };
  };

  return (
    <article className="mx-auto w-full max-w-xl pb-4">
      <header className="flex items-center gap-3">
        <Link
          aria-label="Volver a Inicio"
          className="flex size-11 shrink-0 items-center justify-center rounded-full text-text-secondary transition-colors hover:bg-surface hover:text-text-primary focus-visible:outline-primary"
          href="/home"
        >
          <ChevronLeft aria-hidden="true" size={22} />
        </Link>
        <h1 className="font-brand text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
          Perfil
        </h1>
      </header>

      <section aria-label="Datos de cuenta" className="mt-7 flex min-w-0 items-center gap-4">
        <div className="flex size-14 shrink-0 items-center justify-center rounded-full border border-primary/40 bg-primary/12 font-brand text-xl font-bold text-primary">
          {initial}
        </div>
        <div className="min-w-0">
          <p className="wrap-break-word font-brand text-xl font-bold text-text-primary sm:text-2xl">
            {user.nombre}
          </p>
          <p className="mt-1 break-all text-sm leading-5 text-text-secondary sm:text-base">
            {user.email}
          </p>
        </div>
      </section>

      {unavailableEquipment.length > 0 ? (
        <div className="mt-6">
          <Alert title="Revisá tu equipamiento" variant="error">
            {unavailableEquipment.length === 1
              ? `${unavailableEquipment[0].nombre} ya no está disponible. Entrá a Equipamiento para decidir si querés quitarlo.`
              : `${unavailableEquipment.length} elementos ya no están disponibles. Entrá a Equipamiento para revisarlos.`}
          </Alert>
        </div>
      ) : null}

      <div className="mt-9 space-y-9">
        <ProfileSection title="Entrenamiento">
          <ProfileRow
            label="Objetivo"
            onClick={() => setActiveEditor("objective")}
            value={objectiveLabels[profile.objetivo]}
          />
          <ProfileRow
            label="Experiencia"
            onClick={() => setActiveEditor("experience")}
            value={experienceLabels[profile.nivelExperiencia]}
          />
          <ProfileRow
            label="Frecuencia"
            onClick={() => setActiveEditor("schedule")}
            value={`${profile.diasEntrenamientoPorSemana} ${profile.diasEntrenamientoPorSemana === 1 ? "día" : "días"} por semana`}
          />
          <ProfileRow
            label="Días preferidos"
            onClick={() => setActiveEditor("schedule")}
            value={formatPreferredDays(profile.diasPreferidos)}
          />
          <ProfileRow
            label="Duración"
            onClick={() => setActiveEditor("duration")}
            value={`${profile.duracionSesionMinutos} min`}
          />
          <ProfileRow
            label="Entorno"
            onClick={() => setActiveEditor("environment")}
            value={environmentLabels[profile.entornoEntrenamiento]}
          />
          <ProfileRow
            detail={equipment === null ? "No pudimos cargar las opciones" : undefined}
            label="Equipamiento"
            onClick={() => setActiveEditor("equipment")}
            value={`${profile.equipamientos.length} ${profile.equipamientos.length === 1 ? "seleccionado" : "seleccionados"}`}
          />
        </ProfileSection>

        <ProfileSection title="Datos personales">
          <ProfileRow label="Edad" onClick={() => setActiveEditor("age")} value={`${profile.edad} años`} />
          <ProfileRow label="Peso" onClick={() => setActiveEditor("weight")} value={formatWeight(profile.pesoKg)} />
          <ProfileRow
            label="Sexo"
            onClick={() => setActiveEditor("sex")}
            value={profile.sexo === null ? "No informado" : sexLabels[profile.sexo]}
          />
        </ProfileSection>

        <ProfileSection title="Cuenta y seguridad">
          <ProfileRow
            icon={<KeyRound aria-hidden="true" size={19} />}
            label="Cambiar contraseña"
            onClick={() => setActiveEditor("password")}
          />
          <button
            className="flex min-h-14 w-full items-center gap-3 border-b border-border/60 py-3 text-left text-[15px] font-semibold text-text-primary transition-colors last:border-b-0 hover:text-primary focus-visible:outline-primary disabled:opacity-60"
            disabled={isSigningOut}
            onClick={() => void handleLogout()}
            type="button"
          >
            <LogOut aria-hidden="true" className="text-text-secondary" size={19} />
            <span>{isSigningOut ? "Cerrando sesión…" : "Cerrar sesión"}</span>
          </button>
        </ProfileSection>
      </div>

      {logoutError ? (
        <div className="mt-6">
          <Alert variant="error">No pudimos cerrar sesión. Intentá nuevamente.</Alert>
        </div>
      ) : null}

      <p aria-live="polite" className="sr-only">
        {announcement}
      </p>

      {activeEditor ? (
        <ProfileEditorDialog
          editor={activeEditor}
          equipment={equipment}
          onClose={() => setActiveEditor(null)}
          onEquipmentLoaded={(nextEquipment) => {
            setState((current) =>
              current.status === "ready"
                ? { ...current, equipment: nextEquipment }
                : current,
            );
          }}
          onSave={saveProfile}
          profile={profile}
        />
      ) : null}
    </article>
  );
}

function ProfileSection({ children, title }: { children: React.ReactNode; title: string }) {
  return (
    <section>
      <h2 className="mb-2 text-xs font-bold tracking-[0.12em] text-text-secondary uppercase">
        {title}
      </h2>
      <div className="border-t border-border/70">{children}</div>
    </section>
  );
}

function ProfileRow({ detail, icon, label, onClick, value }: {
  detail?: string;
  icon?: React.ReactNode;
  label: string;
  onClick: () => void;
  value?: string;
}) {
  return (
    <button
      className="group flex min-h-16 w-full min-w-0 items-center gap-3 border-b border-border/60 py-3 text-left transition-colors hover:text-primary focus-visible:outline-primary"
      onClick={onClick}
      type="button"
    >
      {icon ? <span className="shrink-0 text-text-secondary">{icon}</span> : null}
      <span className="min-w-0 flex-1">
        <span className="block text-[15px] font-semibold text-text-primary">{label}</span>
        {detail ? <span className="mt-0.5 block text-xs leading-5 text-error">{detail}</span> : null}
      </span>
      {value ? (
        <span className="max-w-[52%] text-right text-sm leading-5 text-text-secondary sm:text-[15px]">
          {value}
        </span>
      ) : null}
      <ChevronRight aria-hidden="true" className="shrink-0 text-text-secondary/70 transition-colors group-hover:text-primary" size={18} />
    </button>
  );
}

function ProfileSkeleton() {
  return (
    <div aria-label="Cargando perfil" className="mx-auto w-full max-w-xl animate-pulse" role="status">
      <div className="flex items-center gap-3"><div className="size-11 rounded-full bg-surface" /><div className="h-7 w-24 rounded bg-surface" /></div>
      <div className="mt-7 flex items-center gap-4"><div className="size-14 rounded-full bg-surface" /><div className="space-y-2"><div className="h-5 w-36 rounded bg-surface" /><div className="h-4 w-48 rounded bg-surface" /></div></div>
      {[6, 3, 2].map((rows, section) => (
        <div className="mt-9" key={section}>
          <div className="mb-3 h-3 w-28 rounded bg-surface" />
          {Array.from({ length: rows }, (_, row) => <div className="h-16 border-t border-border/60" key={row}><div className="mt-5 h-4 w-4/5 rounded bg-surface" /></div>)}
        </div>
      ))}
      <Spinner className="sr-only" />
    </div>
  );
}
