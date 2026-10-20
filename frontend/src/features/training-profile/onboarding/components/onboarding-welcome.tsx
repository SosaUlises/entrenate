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
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[640px] flex-col justify-center sm:min-h-[calc(100dvh-4rem)]">
      <header className="text-center">
        <h1 className="font-brand text-3xl leading-tight font-bold tracking-tight text-text-primary sm:text-4xl lg:text-[2.5rem]">
          {greeting}
        </h1>
        <p className="mt-2 text-lg font-semibold text-primary sm:text-xl">
          Bienvenido a Entrenate
        </p>
      </header>

      <div className="mx-auto mt-8 max-w-[540px] space-y-5 text-center">
        <p className="text-base leading-6 font-normal text-text-primary">
          Vamos a preparar tu experiencia para que Entrenate se adapte a tu forma
          de entrenar.
        </p>

        <div className="space-y-3">
          <h2 className="font-brand text-xl font-bold text-text-primary lg:text-2xl">
            Primero quiero conocerte un poco.
          </h2>
          <p className="text-sm leading-6 font-normal text-text-secondary lg:text-[0.9375rem]">
            Son unas preguntas rápidas sobre vos y tu forma de entrenar. No te
            va a llevar más de 2 minutos.
          </p>
        </div>
      </div>

      <div className="mx-auto w-full max-w-[440px] pt-6 sm:pt-8">
        <Button
          className="min-h-14"
          fullWidth
          onClick={() => router.push("/onboarding/objective")}
          type="button"
          variant="primary"
        >
          Empezar
        </Button>
      </div>
    </article>
  );
}
