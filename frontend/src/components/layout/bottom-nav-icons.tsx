type BottomNavIconProps = {
  active: boolean;
  className?: string;
};

const detailClassName = (active: boolean) =>
  active ? "text-thermal-accent" : "text-steel/70";

export function HomeNavIcon({ active, className }: BottomNavIconProps) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 24 24"
    >
      <path
        d="M3.75 10.35 12 3.9l8.25 6.45v8.15a1.6 1.6 0 0 1-1.6 1.6H5.35a1.6 1.6 0 0 1-1.6-1.6v-8.15Z"
        stroke="currentColor"
        strokeLinejoin="bevel"
        strokeWidth="1.95"
      />
      <path
        d="M8 20.1v-5.65h8v5.65"
        stroke="currentColor"
        strokeWidth="1.95"
      />
      <path
        className={detailClassName(active)}
        d="M8.2 10.9h7.6"
        stroke="currentColor"
        strokeWidth="1.95"
      />
    </svg>
  );
}

export function RoutinesNavIcon({ active, className }: BottomNavIconProps) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 24 24"
    >
      <path
        d="M5 4.25h14a1.5 1.5 0 0 1 1.5 1.5v12.5a1.5 1.5 0 0 1-1.5 1.5H5a1.5 1.5 0 0 1-1.5-1.5V5.75A1.5 1.5 0 0 1 5 4.25Z"
        stroke="currentColor"
        strokeLinejoin="bevel"
        strokeWidth="1.95"
      />
      <path d="M8.25 8h8.5M8.25 12h8.5M8.25 16h5.5" stroke="currentColor" strokeWidth="1.95" />
      <path
        className={detailClassName(active)}
        d="M5.8 8h.1M5.8 12h.1M5.8 16h.1"
        stroke="currentColor"
        strokeLinecap="square"
        strokeWidth="2.6"
      />
    </svg>
  );
}

export function CoachNavIcon({ active, className }: BottomNavIconProps) {
  return (
    <svg
      aria-hidden="true"
      className={`${active ? "opacity-100" : "opacity-[0.72]"} ${className ?? ""}`}
      focusable="false"
      viewBox="0 0 300 220"
    >
      <defs>
        <clipPath id="coach-a-only" clipPathUnits="userSpaceOnUse">
          <path d="M167 8 300 220H0Z" />
        </clipPath>
      </defs>
      <image
        clipPath="url(#coach-a-only)"
        height="724"
        href="/brand/entrenate-wordmark-master.png"
        width="2172"
        x="-1335"
        y="-235"
      />
    </svg>
  );
}

export function ProgressNavIcon({ active, className }: BottomNavIconProps) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 24 24"
    >
      <path d="M4 19.5V5" stroke="currentColor" strokeWidth="1.95" />
      <path d="M4 19.5h16.25" stroke="currentColor" strokeWidth="1.95" />
      <path
        d="m6.8 16.1 3.25-3.45 3.05 1.7 4.55-5.7"
        stroke="currentColor"
        strokeLinejoin="bevel"
        strokeWidth="2"
      />
      <path
        className={detailClassName(active)}
        d="M17.65 8.65h-3.1M17.65 8.65v3.1"
        stroke="currentColor"
        strokeWidth="1.95"
      />
    </svg>
  );
}

export function ExercisesNavIcon({ active, className }: BottomNavIconProps) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 24 24"
    >
      <path d="M7.25 12h9.5" stroke="currentColor" strokeWidth="2" />
      <path
        d="M5.1 8.55h2.15v6.9H5.1a1 1 0 0 1-1-1v-4.9a1 1 0 0 1 1-1ZM18.9 8.55h-2.15v6.9h2.15a1 1 0 0 0 1-1v-4.9a1 1 0 0 0-1-1Z"
        stroke="currentColor"
        strokeLinejoin="bevel"
        strokeWidth="1.95"
      />
      <path
        className={detailClassName(active)}
        d="M2.35 10.25v3.5M21.65 10.25v3.5"
        stroke="currentColor"
        strokeWidth="1.95"
      />
    </svg>
  );
}
