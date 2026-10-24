"use client";

import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";

type OnboardingWelcomeProps = {
  nombre?: string;
};

export function OnboardingWelcome({ nombre }: OnboardingWelcomeProps) {
  const router = useRouter();
  const normalizedName = nombre?.trim();
  const greeting = normalizedName ? `Hola, ${normalizedName}` : "Hola";

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[520px] -translate-y-3 flex-col justify-center py-2 sm:min-h-[calc(100dvh-4rem)] sm:-translate-y-4 sm:py-4">
      <header>
        <h1 className="font-brand text-[2.5rem] font-extrabold leading-[1.04] tracking-[-0.03em] text-text-primary sm:text-5xl">
          {greeting}
        </h1>
        <p className="mt-3 max-w-[460px] text-base leading-7 text-text-secondary">
          Vamos a adaptar Entrenate a tu forma de entrenar.
        </p>
      </header>

      <ol
        aria-label="Datos que vamos a configurar"
        className="mt-9 border-y border-border sm:mt-10"
      >
        <ConfigurationItem
          description="Qué querés lograr con tu entrenamiento."
          index="01"
          label="Objetivo"
        />
        <ConfigurationItem
          description="Tu punto de partida y nivel actual."
          index="02"
          label="Experiencia"
        />
        <ConfigurationItem
          description="Cómo, cuándo y dónde preferís entrenar."
          index="03"
          label="Preferencias"
        />
      </ol>

      <div className="pt-10 sm:pt-11">
        <Button
          className="font-bold"
          fullWidth
          onClick={() => router.push("/onboarding/objective")}
          size="lg"
          type="button"
          variant="secondary"
        >
          Empezar
        </Button>
      </div>
    </article>
  );
}

function ConfigurationItem({
  description,
  index,
  label,
}: {
  description: string;
  index: string;
  label: string;
}) {
  return (
    <li className="grid min-h-[4.5rem] grid-cols-[auto_1fr] items-center gap-x-4 border-b border-border py-4 last:border-b-0">
      <div aria-hidden="true" className="flex items-center gap-3">
        <span className="h-7 w-0.5 rounded-full bg-thermal-accent" />
        <span className="font-brand text-[0.8125rem] font-bold tracking-[0.12em] text-steel">
          {index}
        </span>
      </div>
      <div className="min-w-0">
        <h2 className="font-brand text-[1.0625rem] font-extrabold leading-6 tracking-[-0.01em] text-text-primary sm:text-lg">
          {label}
        </h2>
        <p className="mt-0.5 text-[0.8125rem] leading-5 text-text-secondary">
          {description}
        </p>
      </div>
    </li>
  );
}
