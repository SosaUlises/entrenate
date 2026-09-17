import Image from "next/image";
import Link from "next/link";
import { ArrowUpRight } from "lucide-react";

export function HomeContent() {
  return (
    <div className="space-y-6">
      <section
        aria-labelledby="training-title"
        className="rounded-card bg-surface px-6 py-7 sm:px-7"
      >
        <h2
          className="font-brand text-2xl font-bold leading-tight text-text-primary"
          id="training-title"
        >
          Tu entrenamiento
        </h2>
        <p className="mt-3 max-w-sm text-sm leading-6 text-text-secondary">
          Elegí un día de tu rutina y empezá a entrenar.
        </p>
        <Link
          className="mt-6 inline-flex min-h-11 items-center gap-2 rounded-control bg-primary-strong px-4 text-sm font-semibold text-text-primary transition-colors hover:bg-primary focus-visible:outline-primary"
          href="/routines"
        >
          Ver mis rutinas
          <ArrowUpRight aria-hidden="true" size={17} />
        </Link>
      </section>

      <section
        aria-label="GymBro"
        className="flex items-center gap-4 border-t border-border/60 pt-4"
      >
        <Image
          alt="GymBro"
          className="h-25 w-20 shrink-0 object-contain"
          height={100}
          src="/gymbro/half-body/gymbro-halfbody-power.png"
          width={80}
        />
        <div className="min-w-0">
          <h2 className="font-brand text-base font-bold text-text-primary">
            Todo listo.
          </h2>
          <p className="mt-1 text-sm leading-5 text-text-secondary">
            Cuando tengas tu primera rutina, vas a poder empezar a registrar tus entrenamientos.
          </p>
        </div>
      </section>
    </div>
  );
}
