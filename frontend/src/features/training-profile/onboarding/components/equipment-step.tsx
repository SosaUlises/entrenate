"use client";

import { useCallback, useEffect, useState } from "react";
import { Check, PackageX, RefreshCw } from "lucide-react";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { getEquipmentAction } from "../actions/get-equipment.action";
import { useOnboarding } from "../context/onboarding-context";
import { getEquipmentImagePath } from "../equipment-images";
import {
  CategoriaEquipamiento,
  type CategoriaEquipamiento as CategoriaEquipamientoValue,
  type Equipment,
} from "../types/equipment.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

type LoadState = "error" | "loading" | "success";

const categoryDefinitions: ReadonlyArray<{
  label: string;
  value: CategoriaEquipamientoValue;
}> = [
  { label: "Pesas libres", value: CategoriaEquipamiento.PesasLibres },
  { label: "Barras", value: CategoriaEquipamiento.Barras },
  { label: "Bancos y racks", value: CategoriaEquipamiento.BancosYRacks },
  { label: "Poleas", value: CategoriaEquipamiento.Poleas },
  { label: "Máquinas", value: CategoriaEquipamiento.Maquinas },
  { label: "Calistenia", value: CategoriaEquipamiento.Calistenia },
  { label: "Otros", value: CategoriaEquipamiento.Otros },
];

export function EquipmentStep() {
  const router = useRouter();
  const { draft, setEquipamientoIds } = useOnboarding();
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
      <OnboardingStepHeader
        currentStep={7}
        onBack={() => router.push("/onboarding/environment")}
      />

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

      <div className="mt-6 md:mt-8">
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
          <div className="space-y-6">
            {categoryDefinitions.map((category) => {
              const categoryEquipment = equipment.filter(
                (item) => item.categoria === category.value,
              );

              if (categoryEquipment.length === 0) {
                return null;
              }

              return (
                <fieldset key={category.value}>
                  <legend className="mb-2.5 text-sm font-semibold text-text-primary">
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
                              "relative flex rounded-control-sm border text-left text-sm leading-5 font-medium transition-[background-color,border-color] duration-200 hover:border-border-strong peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                              imagePath
                                ? "min-h-44 flex-col p-2.5"
                                : "min-h-12 items-center justify-between gap-2 px-3 py-2.5",
                              isSelected
                                ? "border-primary bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface))] text-text-primary"
                                : "border-border bg-surface text-text-secondary",
                            )}
                          >
                            {imagePath ? (
                              <>
                                <span
                                  aria-hidden="true"
                                  className="relative h-28 w-full shrink-0"
                                >
                                  <Image
                                    alt=""
                                    className="object-contain p-2"
                                    fill
                                    sizes="(max-width: 767px) calc((100vw - 3.25rem) / 2), 170px"
                                    src={imagePath}
                                  />
                                </span>
                                <span className="flex min-h-10 w-full flex-1 items-end px-1 pb-0.5 pt-2 font-semibold text-text-primary wrap-break-word">
                                  {item.nombre}
                                </span>
                                {isSelected ? (
                                  <span className="absolute top-2 right-2 flex size-6 items-center justify-center rounded-full border border-primary bg-surface-elevated text-primary">
                                    <Check
                                      aria-hidden="true"
                                      size={14}
                                      strokeWidth={2.5}
                                    />
                                  </span>
                                ) : null}
                              </>
                            ) : (
                              <>
                                <span className="min-w-0 wrap-break-word">
                                  {item.nombre}
                                </span>
                                {isSelected ? (
                                  <Check
                                    aria-hidden="true"
                                    className="shrink-0 text-primary"
                                    size={16}
                                    strokeWidth={2.5}
                                  />
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
                className="text-sm leading-5 text-text-secondary"
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
                  "flex min-h-16 items-center gap-3 rounded-control border px-4 py-3 transition-[background-color,border-color] duration-200 hover:border-border-strong peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  selectedIds?.length === 0
                    ? "border-primary bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface))]"
                    : "border-border bg-surface",
                )}
              >
                <PackageX
                  aria-hidden="true"
                  className={cn(
                    "shrink-0",
                    selectedIds?.length === 0
                      ? "text-primary"
                      : "text-text-secondary",
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
                {selectedIds?.length === 0 ? (
                  <Check
                    aria-hidden="true"
                    className="shrink-0 text-primary"
                    size={16}
                    strokeWidth={2.5}
                  />
                ) : null}
              </span>
            </label>
          </div>
        ) : null}
      </div>

      <div className="mt-6 md:mt-8">
        <Button
          className="min-h-14"
          disabled={!canContinue}
          fullWidth
          onClick={() => router.push("/onboarding/age")}
          type="button"
          variant="gradient"
        >
          Continuar
        </Button>
      </div>
    </article>
  );
}

function EquipmentSkeleton() {
  return (
    <div aria-label="Cargando equipamiento" className="space-y-6" role="status">
      {[3, 4, 2].map((itemCount, sectionIndex) => (
        <div key={sectionIndex}>
          <div className="mb-2.5 h-4 w-28 animate-pulse rounded bg-surface-elevated" />
          <div className="grid grid-cols-2 gap-2.5 md:grid-cols-3">
            {Array.from({ length: itemCount }, (_, itemIndex) => (
              <div
                className="h-12 animate-pulse rounded-control-sm border border-border bg-surface"
                key={itemIndex}
              />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
