import Image from "next/image";
import { ArrowUpRight, ClipboardList, Plus } from "lucide-react";

export function HomeContent() {
  return (
    <div className="space-y-10">
      <section aria-labelledby="training-title">
        <h2
          className="mb-4 font-brand text-xl font-bold text-text-primary"
          id="training-title"
        >
          Tu entrenamiento
        </h2>
        <div className="rounded-card border border-primary/25 bg-surface p-6 sm:p-7">
          <div className="mb-5 h-1.5 w-16 rounded-full bg-linear-to-r from-primary to-secondary" />
          <p className="max-w-sm text-base leading-7 text-text-primary/80">
            Desde acá vas a poder iniciar o continuar tus entrenamientos.
          </p>
          <button
            className="mt-7 inline-flex min-h-12 items-center gap-2 rounded-control border border-border-strong bg-surface-elevated px-4 text-sm font-semibold text-text-secondary disabled:cursor-not-allowed"
            disabled
            type="button"
          >
            Ir a mis rutinas
            <ArrowUpRight aria-hidden="true" size={17} />
            <span className="sr-only">Próximamente</span>
          </button>
        </div>
      </section>

      <section aria-labelledby="routines-title">
        <div className="mb-4 flex items-baseline justify-between gap-3">
          <h2
            className="font-brand text-xl font-bold text-text-primary"
            id="routines-title"
          >
            Mis rutinas
          </h2>
          <span className="text-xs font-medium text-text-secondary">Próximamente</span>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <button
            className="flex min-h-27 flex-col items-start justify-between rounded-card border border-border-strong bg-surface p-4 text-left text-sm font-semibold text-text-secondary disabled:cursor-not-allowed"
            disabled
            type="button"
          >
            <ClipboardList aria-hidden="true" size={22} />
            Mis rutinas
          </button>
          <button
            className="flex min-h-27 flex-col items-start justify-between rounded-card border border-border-strong bg-surface p-4 text-left text-sm font-semibold text-text-secondary disabled:cursor-not-allowed"
            disabled
            type="button"
          >
            <Plus aria-hidden="true" size={22} />
            Crear rutina
          </button>
        </div>
      </section>

      <section
        aria-label="GymBro"
        className="flex items-center gap-4 rounded-card border border-border bg-surface p-4 sm:p-5"
      >
        <Image
          alt="GymBro"
          className="size-19 shrink-0 object-contain sm:size-21"
          height={84}
          src="/gymbro/avatar/gymbro-avatar-happy.png"
          width={84}
        />
        <div>
          <h2 className="font-brand text-base font-bold text-text-primary">
            Todo listo.
          </h2>
          <p className="mt-1 text-sm leading-6 text-text-secondary">
            Cuando tengas tu primera rutina, vas a poder empezar a registrar tus entrenamientos.
          </p>
        </div>
      </section>
    </div>
  );
}
