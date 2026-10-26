import Link from "next/link";
import {
  Apple,
  ChevronRight,
  ClipboardList,
  RefreshCw,
  TrendingUp,
  type LucideIcon,
} from "lucide-react";

type PlannedCapability = {
  description: string;
  icon: LucideIcon;
  title: string;
};

const plannedCapabilities: PlannedCapability[] = [
  {
    description: "Ajust\u00e1 seg\u00fan tiempo y equipamiento.",
    icon: RefreshCw,
    title: "Adaptaci\u00f3n",
  },
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
      <header className="max-w-lg">
        <h1
          className="font-brand text-3xl font-bold leading-tight text-text-primary"
          id="coach-title"
        >
          Coach
        </h1>
        <p className="mt-2 text-base leading-7 text-text-secondary">
          An&aacute;lisis y orientaci&oacute;n basados en tus entrenamientos reales.
        </p>
      </header>

      <ul className="mt-11 divide-y divide-border/70 border-y border-border/70">
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

        {plannedCapabilities.map((capability) => (
          <li key={capability.title}>
            <div
              aria-disabled="true"
              className="flex min-h-24 items-center gap-3 px-2 py-4"
            >
              <CapabilityIcon disabled icon={capability.icon} />
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
                  <h2 className="font-brand text-lg font-bold leading-6 text-text-secondary">
                    {capability.title}
                  </h2>
                  <span className="text-[0.8125rem] font-semibold leading-5 text-text-muted">
                    Pr&oacute;ximamente
                  </span>
                </div>
                <p className="mt-1 truncate text-sm leading-5 text-text-muted">
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
        className="group flex min-h-28 items-center gap-3 rounded-control-sm px-2 py-5 transition-[background-color,transform] duration-150 hover:bg-surface-interactive/55 active:scale-[0.99] active:bg-surface-active/55 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none motion-reduce:active:scale-100"
        href={href}
      >
        <CapabilityIcon icon={icon} />
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
            <h2 className="font-brand text-xl font-bold leading-7 text-text-primary">
              {title}
            </h2>
            <span className="text-[0.8125rem] font-semibold leading-5 text-info">
              Disponible
            </span>
          </div>
          <p className="mt-1.5 truncate text-sm leading-5 text-text-secondary">
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
      className={`flex size-[3.25rem] shrink-0 items-center justify-center rounded-control border ${disabled ? "border-border bg-surface text-text-muted" : "border-border-strong bg-surface-interactive text-steel"}`}
    >
      <Icon size={24} strokeWidth={1.8} />
    </span>
  );
}
