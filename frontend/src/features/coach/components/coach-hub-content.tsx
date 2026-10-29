import Link from "next/link";
import {
  Apple,
  ChevronRight,
  ClipboardList,
  RefreshCw,
  TrendingUp,
  type LucideIcon,
} from "lucide-react";
import { MainPageHeader } from "@/components/layout/main-page-header";

type PlannedCapability = {
  description: string;
  icon: LucideIcon;
  title: string;
};

const plannedCapabilities: PlannedCapability[] = [
  {
    description: "Orientaci\u00f3n ligada a tu entrenamiento.",
    icon: Apple,
    title: "Nutrici\u00f3n",
  },
];

export function CoachHubContent() {
  return (
    <section
      aria-labelledby="coach-title"
      className="mx-auto w-full min-w-0 max-w-xl"
    >
      <MainPageHeader
        description="Análisis y orientación basados en tus entrenamientos reales."
        id="coach-title"
        title="Coach"
      />

      <ul className="divide-y divide-border/70 border-y border-border/70">
        <AvailableCapability
          description="Analiz&aacute; tu evoluci&oacute;n con contexto."
          href="/coach/progress"
          icon={TrendingUp}
          title="Progreso"
        />
        <AvailableCapability
          description="Gener&aacute; una propuesta desde tu perfil actual."
          href="/coach/routines"
          icon={ClipboardList}
          title="Rutinas"
        />
        <AvailableCapability
          description="Eleg&iacute; un d&iacute;a y ajustalo a las condiciones de hoy."
          href="/routines"
          icon={RefreshCw}
          title="Adaptaci&oacute;n"
        />

        {plannedCapabilities.map((capability) => (
          <li key={capability.title}>
            <div
              aria-disabled="true"
              className="flex min-h-24 items-center gap-3 px-1 py-4"
            >
              <CapabilityIcon disabled icon={capability.icon} />
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
                  <h2 className="font-brand text-[1.125rem] font-bold leading-6 text-text-secondary">
                    {capability.title}
                  </h2>
                  <span className="text-[0.8125rem] font-semibold leading-5 text-text-muted">
                    Pr&oacute;ximamente
                  </span>
                </div>
                <p className="mt-1 truncate text-[0.8125rem] leading-5 text-text-muted">
                  {capability.description}
                </p>
              </div>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}

function AvailableCapability({
  description,
  href,
  icon,
  title,
}: PlannedCapability & { href: string }) {
  return (
    <li>
      <Link
        className="group flex min-h-24 items-center gap-3 px-1 py-4 transition-[background-color,transform] duration-150 hover:bg-surface-interactive/45 active:scale-[0.99] active:bg-surface-active/50 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none motion-reduce:active:scale-100"
        href={href}
      >
        <CapabilityIcon icon={icon} />
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
            <h2 className="font-brand text-[1.125rem] font-bold leading-6 text-text-primary">
              {title}
            </h2>
            <span className="text-[0.8125rem] font-semibold leading-5 text-text-secondary">
              Disponible
            </span>
          </div>
          <p className="mt-1 truncate text-[0.8125rem] leading-5 text-text-secondary">
            {description}
          </p>
        </div>
        <span
          aria-hidden="true"
          className="flex size-11 shrink-0 items-center justify-center text-text-muted transition-colors duration-150 group-hover:text-text-primary group-active:text-thermal-accent motion-reduce:transition-none"
        >
          <ChevronRight size={20} strokeWidth={1.8} />
        </span>
      </Link>
    </li>
  );
}

function CapabilityIcon({
  disabled = false,
  icon: Icon,
}: {
  disabled?: boolean;
  icon: LucideIcon;
}) {
  return (
    <span
      aria-hidden="true"
      className={`relative flex size-11 shrink-0 items-center justify-center ${disabled ? "text-text-muted" : "text-steel after:absolute after:bottom-0.5 after:h-0.5 after:w-3 after:rounded-full after:bg-thermal-accent"}`}
    >
      <Icon size={25} strokeWidth={1.9} />
    </span>
  );
}
