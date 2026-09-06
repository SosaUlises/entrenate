import Image from "next/image";
import { Button } from "@/components/ui/button";

type OnboardingWelcomeProps = {
  nombre?: string;
};

export function OnboardingWelcome({ nombre }: OnboardingWelcomeProps) {
  const normalizedName = nombre?.trim();
  const greeting = normalizedName ? `Hola, ${normalizedName} 👋` : "Hola 👋";

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[640px] flex-col sm:min-h-[calc(100dvh-4rem)] lg:grid lg:max-w-[900px] lg:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)] lg:grid-rows-[auto_auto_auto] lg:content-center lg:items-center lg:gap-x-12">
      <header className="text-center lg:col-start-2 lg:row-start-1 lg:text-left">
        <h1 className="font-brand text-3xl font-bold tracking-tight text-text-primary sm:text-4xl">
          {greeting}
        </h1>
        <p className="mt-2 font-brand text-lg font-semibold text-primary sm:text-xl">
          Soy GymBro.
        </p>
      </header>

      <div className="relative mx-auto my-4 h-[clamp(10.5rem,30dvh,17rem)] w-full max-w-[19rem] shrink-0 sm:my-5 lg:col-start-1 lg:row-span-3 lg:row-start-1 lg:my-0 lg:h-[min(46dvh,25rem)] lg:max-w-[24rem]">
        <Image
          alt="GymBro, asistente de entrenamiento de Entrenate"
          className="object-contain"
          fill
          priority
          sizes="(max-width: 640px) 76vw, (max-width: 1023px) 304px, 384px"
          src="/gymbro/half-body/gymbro-halfbody-neutral.png"
        />
      </div>

      <div className="mx-auto max-w-[560px] space-y-4 text-center lg:col-start-2 lg:row-start-2 lg:mt-6 lg:text-left">
        <p className="text-base leading-7 text-text-primary sm:text-lg">
          Voy a acompañarte en Entrenate para ayudarte a sacar lo mejor de cada
          entrenamiento.
        </p>

        <div className="space-y-2">
          <h2 className="font-brand text-xl font-semibold text-text-primary sm:text-2xl">
            Primero quiero conocerte un poco.
          </h2>
          <p className="text-sm leading-6 text-text-secondary sm:text-base">
            Son unas preguntas rápidas sobre vos y tu forma de entrenar. No te
            va a llevar más de 2 minutos.
          </p>
        </div>
      </div>

      <div className="mx-auto mt-auto w-full max-w-[440px] pt-6 sm:pt-8 md:mt-0 lg:col-start-2 lg:row-start-3 lg:max-w-none">
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
