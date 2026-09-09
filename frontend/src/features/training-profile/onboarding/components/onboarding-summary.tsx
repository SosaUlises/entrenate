"use client";

import Image from "next/image";
import { Pencil } from "lucide-react";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { createTrainingProfileAction } from "../actions/create-training-profile.action";
import { useOnboarding } from "../context/onboarding-context";
import {
  createTrainingProfileRequestFromDraft,
  environmentLabels,
  experienceLabels,
  formatPreferredDays,
  formatWeight,
  getFirstIncompleteStepPath,
  objectiveLabels,
  sexLabels,
} from "../onboarding-summary";
import type { Equipment } from "../types/equipment.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

type OnboardingSummaryProps = {
  equipment: Equipment[];
  nombre?: string;
};

type SubmitState = "already-exists" | "error" | "idle" | "submitting" | "success";

const summarySections = {
  training: "summary-training",
  equipment: "summary-equipment",
  personal: "summary-personal",
} as const;

export function OnboardingSummary({
  equipment,
  nombre,
}: OnboardingSummaryProps) {
  const router = useRouter();
  const { draft } = useOnboarding();
  const { markProfileCreated } = useTrainingProfileGate();
  const isSubmittingRef = useRef(false);
  const [submitState, setSubmitState] = useState<SubmitState>("idle");
  const [errorMessage, setErrorMessage] = useState<string>();
  const incompleteStepPath = getFirstIncompleteStepPath(draft);

  useEffect(() => {
    if (incompleteStepPath) {
      router.replace(incompleteStepPath);
    }
  }, [incompleteStepPath, router]);

  if (incompleteStepPath) {
    return (
      <p className="m-auto text-sm text-text-secondary" role="status">
        Volviendo al onboarding...
      </p>
    );
  }

  const normalizedName = nombre?.trim();
  const greeting = normalizedName ? `¡Listo, ${normalizedName}!` : "¡Listo!";
  const selectedEquipment = resolveEquipmentNames(
    draft.equipamientoIds!,
    equipment,
  );

  const handleSubmit = async () => {
    if (isSubmittingRef.current) {
      return;
    }

    const request = createTrainingProfileRequestFromDraft(draft);

    if (!request) {
      return;
    }

    isSubmittingRef.current = true;
    setSubmitState("submitting");
    setErrorMessage(undefined);

    try {
      const result = await createTrainingProfileAction(request);

      if (result.ok) {
        markProfileCreated();
        setSubmitState("success");
        return;
      }

      if (result.kind === "already_exists") {
        markProfileCreated();
        setSubmitState("already-exists");
        return;
      }

      setErrorMessage(result.message);
      setSubmitState("error");
    } catch {
      setSubmitState("error");
    } finally {
      isSubmittingRef.current = false;
    }
  };

  if (submitState === "success" || submitState === "already-exists") {
    return (
      <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[600px] flex-col sm:min-h-[calc(100dvh-4rem)]">
        <OnboardingStepHeader
          currentStep={10}
          label="Listo"
          onBack={() => router.push("/onboarding/sex")}
        />
        <div className="my-auto flex flex-col items-center py-10 text-center">
          <GymBro />
          <h1 className="mt-5 font-brand text-3xl font-bold text-text-primary">
            {submitState === "success"
              ? "¡Perfil creado!"
              : "¡Tu perfil ya está listo!"}
          </h1>
          <p className="mt-3 text-base leading-6 text-text-secondary">
            Entrenate ya está listo para empezar.
          </p>
        </div>
      </article>
    );
  }

  return (
    <article className="mx-auto w-full max-w-[600px] pb-6">
      <OnboardingStepHeader
        currentStep={10}
        label="Listo"
        onBack={() => router.push("/onboarding/sex")}
      />

      <div className="mt-8 flex flex-col items-center text-center sm:mt-9">
        <GymBro />
        <h1 className="mt-5 font-brand text-[28px] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl">
          {greeting}
        </h1>
        <p className="mt-2 max-w-md text-base leading-6 text-text-primary">
          Ya tengo lo necesario para adaptar Entrenate a vos.
        </p>
        <p className="mt-2 text-sm leading-6 text-text-secondary">
          Revisá que esté todo bien.
        </p>
      </div>

      <div className="mt-8 space-y-6">
        <SummarySection id={summarySections.training} title="Entrenamiento">
          <SummaryRow
            editPath="/onboarding/objective"
            label="Objetivo"
            value={objectiveLabels[draft.objetivo!]}
          />
          <SummaryRow
            editPath="/onboarding/experience"
            label="Experiencia"
            value={experienceLabels[draft.nivelExperiencia!]}
          />
          <SummaryRow
            editPath="/onboarding/training-days"
            label="Frecuencia"
            value={`${draft.diasEntrenamientoPorSemana} ${
              draft.diasEntrenamientoPorSemana === 1 ? "día" : "días"
            } por semana`}
          />
          <SummaryRow
            editPath="/onboarding/preferred-days"
            label="Días preferidos"
            value={formatPreferredDays(draft.diasPreferidos!)}
          />
          <SummaryRow
            editPath="/onboarding/duration"
            label="Duración"
            value={`${draft.duracionSesionMinutos} min`}
          />
          <SummaryRow
            editPath="/onboarding/environment"
            label="Entorno"
            value={environmentLabels[draft.entornoEntrenamiento!]}
          />
        </SummarySection>

        <SummarySection id={summarySections.equipment} title="Equipamiento">
          <SummaryRow
            editPath="/onboarding/equipment"
            label="Equipamiento disponible"
            value={selectedEquipment.visibleNames}
          >
            {selectedEquipment.remainingCount > 0 ? (
              <span className="mt-1 block text-[13px] font-medium text-primary">
                + {selectedEquipment.remainingCount} más
              </span>
            ) : null}
          </SummaryRow>
        </SummarySection>

        <SummarySection id={summarySections.personal} title="Sobre vos">
          <SummaryRow
            editPath="/onboarding/age"
            label="Edad"
            value={`${draft.edad} ${draft.edad === 1 ? "año" : "años"}`}
          />
          <SummaryRow
            editPath="/onboarding/weight"
            label="Peso"
            value={formatWeight(draft.pesoKg!)}
          />
          <SummaryRow
            editPath="/onboarding/sex"
            label="Sexo"
            value={sexLabels[draft.sexo!]}
          />
        </SummarySection>
      </div>

      <div className="mt-8">
        {submitState === "error" ? (
          <div className="mb-4" id="profile-submit-error">
            <Alert title="No pudimos crear tu perfil." variant="error">
              {errorMessage ?? "Intentá nuevamente."}
            </Alert>
          </div>
        ) : null}

        <Button
          aria-describedby={submitState === "error" ? "profile-submit-error" : undefined}
          className="min-h-14"
          disabled={submitState === "submitting"}
          fullWidth
          isLoading={submitState === "submitting"}
          onClick={handleSubmit}
          type="button"
          variant="gradient"
        >
          {submitState === "submitting"
            ? "Creando tu perfil..."
            : submitState === "error"
              ? "Reintentar"
              : "Crear mi perfil"}
        </Button>
      </div>
    </article>
  );
}

function GymBro() {
  return (
    <div className="relative mx-auto h-[clamp(10.5rem,30dvh,17rem)] w-full max-w-[19rem] shrink-0 lg:h-[min(52dvh,26.25rem)] lg:max-w-[26.25rem]">
      <Image
        alt="GymBro celebrando"
        className="object-contain"
        fill
        priority
        sizes="(max-width: 640px) 76vw, (max-width: 1023px) 304px, 420px"
        src="/gymbro/half-body/gymbro-halfbody-thumbs-up.png"
      />
    </div>
  );
}

function SummarySection({
  children,
  id,
  title,
}: {
  children: React.ReactNode;
  id: string;
  title: string;
}) {
  return (
    <section aria-labelledby={id}>
      <h2
        className="mb-2 px-0.5 text-sm font-semibold text-text-secondary"
        id={id}
      >
        {title}
      </h2>
      <dl className="overflow-hidden rounded-card border border-border bg-surface">
        {children}
      </dl>
    </section>
  );
}

function SummaryRow({
  children,
  editPath,
  label,
  value,
}: {
  children?: React.ReactNode;
  editPath: string;
  label: string;
  value: string;
}) {
  const router = useRouter();

  return (
    <div className="flex items-center justify-between gap-3 border-b border-border/70 px-4 py-4 last:border-b-0">
      <div className="min-w-0 flex-1">
        <dt className="text-[13px] leading-5 font-medium text-text-secondary">
          {label}
        </dt>
        <dd className="mt-1 text-[15px] leading-6 font-semibold text-text-primary sm:text-base">
          {value}
          {children}
        </dd>
      </div>
      <button
        aria-label={`Editar ${label.toLocaleLowerCase("es-AR")}`}
        className="group inline-flex size-11 shrink-0 items-center justify-center text-text-secondary"
        onClick={() => router.push(`${editPath}?returnTo=summary`)}
        type="button"
      >
        <span className="inline-flex size-9 items-center justify-center rounded-control-sm transition-colors group-hover:bg-surface-elevated group-hover:text-primary group-focus-visible:bg-surface-elevated group-focus-visible:text-primary">
          <Pencil aria-hidden="true" size={17} strokeWidth={2} />
        </span>
      </button>
    </div>
  );
}

function resolveEquipmentNames(
  selectedIds: string[],
  equipment: Equipment[],
): { remainingCount: number; visibleNames: string } {
  if (selectedIds.length === 0) {
    return { remainingCount: 0, visibleNames: "Sin equipamiento" };
  }

  const equipmentNamesById = new Map(
    equipment.map((item) => [item.id, item.nombre]),
  );
  const selectedNames = selectedIds.flatMap((id) => {
    const name = equipmentNamesById.get(id);
    return name ? [name] : [];
  });

  if (selectedNames.length === 0) {
    return {
      remainingCount: 0,
      visibleNames: `${selectedIds.length} ${
        selectedIds.length === 1 ? "elemento seleccionado" : "elementos seleccionados"
      }`,
    };
  }

  return {
    remainingCount: Math.max(0, selectedNames.length - 3),
    visibleNames: selectedNames.slice(0, 3).join(" · "),
  };
}
