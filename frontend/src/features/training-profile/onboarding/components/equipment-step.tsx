"use client";

import { useCallback, useEffect, useState } from "react";
import { Check, PackageX, RefreshCw } from "lucide-react";
import Image from "next/image";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { getEquipmentAction } from "../actions/get-equipment.action";
import { useOnboarding } from "../context/onboarding-context";
import { getEquipmentImagePath } from "../equipment-images";
import { equipmentCategoryDefinitions } from "../../shared/training-profile-options";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import { type Equipment } from "../types/equipment.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

type LoadState = "error" | "loading" | "success";

export function EquipmentStep() {
  const { draft, setEquipamientoIds } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding/environment",
    nextPath: "/onboarding/age",
  });
  const [equipment, setEquipment] = useState<Equipment[]>([]);
  const [loadState, setLoadState] = useState<LoadState>("loading");
  const selectedIds = draft.equipamientoIds;

  const loadEquipment = useCallback(async () => {
    setLoadState("loading");
    const result = await getEquipmentAction();

    if (!result.ok) {
      setLoadState("error");
      return;
    }

    setEquipment(result.equipment);
    setLoadState("success");

    if (result.equipment.length === 0) {
      setEquipamientoIds([]);
    }
  }, [setEquipamientoIds]);

  useEffect(() => {
    let isActive = true;

    void getEquipmentAction().then((result) => {
      if (!isActive) {
        return;
      }

      if (!result.ok) {
        setLoadState("error");
        return;
      }

      setEquipment(result.equipment);
      setLoadState("success");

      if (result.equipment.length === 0) {
        setEquipamientoIds([]);
      }
    });

    return () => {
      isActive = false;
    };
  }, [setEquipamientoIds]);

  const toggleEquipment = (equipmentId: string) => {
    const currentIds = selectedIds ?? [];

    if (currentIds.includes(equipmentId)) {
      const nextIds = currentIds.filter((id) => id !== equipmentId);
      setEquipamientoIds(nextIds.length > 0 ? nextIds : undefined);
      return;
    }

    setEquipamientoIds([...currentIds, equipmentId]);
  };

  const toggleNoEquipment = () => {
    setEquipamientoIds(selectedIds?.length === 0 ? undefined : []);
  };

  const selectedCount = selectedIds?.length ?? 0;
  const canContinue = loadState === "success" && selectedIds !== undefined;

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)]">
      <OnboardingStepHeader currentStep={7} onBack={goBack} />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Qué equipamiento tenés disponible?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Seleccioná todo lo que puedas usar para entrenar.
        </p>
        <p className="mt-1 text-sm leading-6 text-text-secondary">
          Podés elegir varias opciones.
        </p>
      </div>

      <div className="mt-6 pb-3 md:mt-8">
        {loadState === "loading" ? <EquipmentSkeleton /> : null}

        {loadState === "error" ? (
          <Alert title="No pudimos cargar el equipamiento." variant="error">
            <p>Intentá nuevamente.</p>
            <Button
              className="mt-3 min-h-10 px-4"
              onClick={() => void loadEquipment()}
              variant="secondary"
            >
              <RefreshCw aria-hidden="true" size={16} />
              Reintentar
            </Button>
          </Alert>
        ) : null}

        {loadState === "success" && equipment.length === 0 ? (
          <div
            className="flex items-center gap-3 rounded-control border border-border bg-surface px-4 py-3 text-sm leading-6 text-text-secondary"
            role="status"
          >
            <PackageX aria-hidden="true" className="shrink-0" size={20} />
            <p>No hay equipamiento disponible para seleccionar.</p>
          </div>
        ) : null}

        {loadState === "success" && equipment.length > 0 ? (
          <div className="space-y-[1.125rem]">
            {equipmentCategoryDefinitions.map((category) => {
              const categoryEquipment = equipment.filter(
                (item) => item.categoria === category.value,
              );

              if (categoryEquipment.length === 0) {
                return null;
              }

              return (
                <fieldset key={category.value}>
                  <legend className="mb-2 font-brand text-[0.8125rem] font-bold tracking-[0.02em] text-text-primary">
                    {category.label}
                  </legend>
                  <div className="grid grid-cols-2 items-start gap-2.5 md:grid-cols-3">
                    {categoryEquipment.map((item) => {
                      const isSelected = selectedIds?.includes(item.id) ?? false;
                      const imagePath = getEquipmentImagePath(item.nombre);

                      return (
                        <label className="block cursor-pointer" key={item.id}>
                          <input
                            checked={isSelected}
                            className="peer sr-only"
                            onChange={() => toggleEquipment(item.id)}
                            type="checkbox"
                          />
                          <span
                            className={cn(
                              "relative flex overflow-hidden rounded-card border text-left text-sm font-medium leading-5 transition-[background-color,border-color,box-shadow,transform] duration-200 active:scale-[0.99] peer-focus-visible:ring-2 peer-focus-visible:ring-thermal-accent peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                              imagePath
                                ? "h-44 flex-col"
                                : "min-h-12 items-center justify-between gap-2 px-3 py-2.5",
                              isSelected
                                ? "border-thermal-accent/40 bg-surface-active text-text-primary shadow-[inset_0_0_0_1px_rgba(225,115,78,0.18)]"
                                : "border-border bg-surface-interactive text-text-secondary hover:border-border-strong",
                            )}
                          >
                            {imagePath ? (
                              <>
                                <span
                                  aria-hidden="true"
                                  className={cn(
                                    "relative h-28 w-full shrink-0 border-b transition-[background-color,border-color] duration-200",
                                    isSelected
                                      ? "border-thermal-accent/15 bg-surface-elevated/50"
                                      : "border-border/60 bg-surface/50",
                                  )}
                                >
                                  <Image
                                    alt=""
                                    className="object-contain p-2 brightness-[1.16] contrast-[1.08]"
                                    fill
                                    sizes="(max-width: 767px) calc((100vw - 3.25rem) / 2), 170px"
                                    src={imagePath}
                                  />
                                </span>
                                <span className="flex min-h-0 w-full flex-1 items-center px-3.5 py-3 font-brand text-[0.8125rem] font-bold leading-[1.3] tracking-tight text-text-primary wrap-break-word sm:text-sm">
                                  {item.nombre}
                                </span>
                                <span
                                  aria-hidden="true"
                                  className={cn(
                                    "absolute right-2 top-2 flex size-5 items-center justify-center rounded-full border transition-[background-color,border-color,color,transform] duration-200",
                                    isSelected
                                      ? "scale-100 border-thermal-accent/60 bg-thermal-accent/15 text-thermal-accent"
                                      : "scale-95 border-border-strong/70 bg-surface/85 text-transparent",
                                  )}
                                >
                                  <Check
                                    className={cn(
                                      "transition-transform duration-200",
                                      isSelected ? "scale-100" : "scale-0",
                                    )}
                                    size={11}
                                    strokeWidth={2.25}
                                  />
                                </span>
                              </>
                            ) : (
                              <>
                                <span className="min-w-0 wrap-break-word">
                                  {item.nombre}
                                </span>
                                {isSelected ? (
                                  <Check aria-hidden="true" className="shrink-0 text-thermal-accent" size={16} strokeWidth={2.5} />
                                ) : null}
                              </>
                            )}
                          </span>
                        </label>
                      );
                    })}
                  </div>
                </fieldset>
              );
            })}

            {selectedCount > 0 ? (
              <p
                aria-live="polite"
                className="text-[0.8125rem] font-medium leading-5 text-text-muted"
              >
                {selectedCount} {selectedCount === 1 ? "seleccionado" : "seleccionados"}
              </p>
            ) : null}

            <label className="block cursor-pointer">
              <input
                checked={selectedIds?.length === 0}
                className="peer sr-only"
                onChange={toggleNoEquipment}
                type="checkbox"
              />
              <span
                className={cn(
                  "flex min-h-16 items-center gap-3 border-y px-1 py-3 transition-[background-color,border-color] duration-200 hover:bg-surface-interactive peer-focus-visible:ring-2 peer-focus-visible:ring-thermal-accent peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  selectedIds?.length === 0
                    ? "border-thermal-accent/35 bg-surface-active/60"
                    : "border-border/70 bg-transparent",
                )}
              >
                <PackageX
                  aria-hidden="true"
                  className={cn(
                    "shrink-0",
                    selectedIds?.length === 0
                      ? "text-thermal-accent"
                      : "text-text-muted",
                  )}
                  size={22}
                />
                <span className="min-w-0 flex-1">
                  <span className="block text-sm font-semibold text-text-primary">
                    No tengo equipamiento
                  </span>
                  <span className="mt-0.5 block text-[13px] leading-5 text-text-secondary">
                    Entreno sin equipamiento disponible
                  </span>
                </span>
                <span
                  aria-hidden="true"
                  className={cn(
                    "flex size-6 shrink-0 items-center justify-center rounded-full border transition-[background-color,border-color,color,transform] duration-200",
                    selectedIds?.length === 0
                      ? "scale-100 border-thermal-accent/60 bg-thermal-accent/15 text-thermal-accent"
                      : "scale-95 border-border-strong bg-surface text-transparent",
                  )}
                >
                  <Check
                    className={cn(
                      "transition-transform duration-200",
                      selectedIds?.length === 0 ? "scale-100" : "scale-0",
                    )}
                    size={13}
                    strokeWidth={2.25}
                  />
                </span>
              </span>
            </label>
          </div>
        ) : null}
      </div>

      <div className="sticky -bottom-6 z-20 -mx-5 mt-5 border-t border-border/70 bg-background/95 px-5 pb-[calc(0.75rem+env(safe-area-inset-bottom))] pt-3 backdrop-blur-md sm:-bottom-8 sm:-mx-6 sm:px-6 sm:pb-[calc(1rem+env(safe-area-inset-bottom))]">
        <div className="mx-auto w-full max-w-[520px]">
          <Button
            disabled={!canContinue}
            fullWidth
            onClick={goNext}
            size="lg"
            type="button"
            variant="primary"
          >
            Continuar
          </Button>
        </div>
      </div>
    </article>
  );
}

function EquipmentSkeleton() {
  return (
    <div aria-label="Cargando equipamiento" className="space-y-[1.125rem]" role="status">
      {[3, 4, 2].map((itemCount, sectionIndex) => (
        <div key={sectionIndex}>
          <div className="mb-2.5 h-4 w-28 animate-pulse rounded bg-surface-elevated" />
          <div className="grid grid-cols-2 gap-2.5 md:grid-cols-3">
            {Array.from({ length: itemCount }, (_, itemIndex) => (
              <div
                className="h-44 animate-pulse rounded-card border border-border bg-surface"
                key={itemIndex}
              />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
