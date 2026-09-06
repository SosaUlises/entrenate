import Image from "next/image";
import { Button } from "@/components/ui/button";

type OnboardingWelcomeProps = {
  nombre?: string;
};

export function OnboardingWelcome({ nombre }: OnboardingWelcomeProps) {
  const normalizedName = nombre?.trim();
  const greeting = normalizedName ? `Hola, ${normalizedName}` : "Hola";

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[640px] flex-col justify-center sm:min-h-[calc(100dvh-4rem)] lg:grid lg:max-w-[960px] lg:grid-cols-[minmax(0,0.85fr)_minmax(0,1.15fr)] lg:grid-rows-[auto_auto_auto] lg:content-center lg:items-center lg:gap-x-16 xl:gap-x-20">
      <header className="text-center lg:col-start-2 lg:row-start-1 lg:max-w-[460px] lg:text-left">
        <h1 className="font-brand text-3xl leading-tight font-bold tracking-tight text-text-primary sm:text-4xl lg:text-[2.5rem]">
          {greeting}
        </h1>
        <p className="mt-2 text-lg font-semibold text-primary sm:text-xl">
          Soy GymBro
        </p>
      </header>

      <div className="relative mx-auto mt-4 mb-5 h-[clamp(10.5rem,30dvh,17rem)] w-full max-w-[19rem] shrink-0 sm:my-5 lg:col-start-1 lg:row-span-3 lg:row-start-1 lg:my-0 lg:h-[min(52dvh,26.25rem)] lg:max-w-[26.25rem]">
        <Image
          alt="GymBro, asistente de entrenamiento de Entrenate"
          className="object-contain"
          fill
          priority
          sizes="(max-width: 640px) 76vw, (max-width: 1023px) 304px, 420px"
          src="/gymbro/half-body/gymbro-halfbody-power.png"
        />
      </div>

      <div className="mx-auto max-w-[540px] space-y-5 text-center lg:col-start-2 lg:row-start-2 lg:mt-6 lg:max-w-[460px] lg:text-left">
        <p className="text-base leading-6 font-normal text-text-primary">
          Voy a acompañarte en Entrenate para ayudarte a sacar lo mejor de cada
          entrenamiento.
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

      <div className="mx-auto w-full max-w-[440px] pt-6 sm:pt-8 lg:col-start-2 lg:row-start-3 lg:mx-0 lg:max-w-[360px]">
        <Button
          className="min-h-14"
          fullWidth
          type="button"
          variant="gradient"
        >
          Empezar
        </Button>
      </div>
    </article>
  );
}
