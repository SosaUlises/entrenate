type EnvironmentIllustrationProps = {
  isSelected: boolean;
};

const illustrationClassName = "h-[60px] w-[90px] overflow-visible";

export function HomeWorkoutIllustration({
  isSelected,
}: EnvironmentIllustrationProps) {
  const accentOpacity = isSelected ? 0.95 : 0.5;

  return (
    <svg
      aria-hidden="true"
      className={illustrationClassName}
      fill="none"
      viewBox="0 0 96 64"
    >
      <path
        d="M14 31 48 7l34 24"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2.2"
      />
      <path
        d="M22 26v31h52V26M41 57V41h14v16"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2"
      />
      <rect
        height="9"
        rx="1.5"
        stroke="currentColor"
        strokeWidth="1.8"
        width="9"
        x="28"
        y="35"
      />
      <path
        d="M58 44h16M60 39v10m4-8v6m4-6v6m4-8v10"
        opacity={accentOpacity}
        stroke="var(--primary)"
        strokeLinecap="round"
        strokeWidth="2"
      />
    </svg>
  );
}

export function SmallGymIllustration({
  isSelected,
}: EnvironmentIllustrationProps) {
  const accentOpacity = isSelected ? 0.95 : 0.5;

  return (
    <svg
      aria-hidden="true"
      className={illustrationClassName}
      fill="none"
      viewBox="0 0 96 64"
    >
      <path
        d="M20 53h58M31 50l5-15M65 50l-5-15"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="2.2"
      />
      <rect
        height="9"
        rx="4.5"
        stroke="currentColor"
        strokeWidth="2.2"
        width="39"
        x="28.5"
        y="29"
      />
      <path
        d="M25 16h46M28 10v12M34 12v8M62 12v8M68 10v12"
        opacity={accentOpacity}
        stroke="var(--primary)"
        strokeLinecap="round"
        strokeWidth="2.6"
      />
      <path
        d="M42 38v8h12v-8"
        opacity={isSelected ? 0.85 : 0.44}
        stroke="var(--secondary)"
        strokeLinejoin="round"
        strokeWidth="2"
      />
    </svg>
  );
}

export function CalisthenicsIllustration({
  isSelected,
}: EnvironmentIllustrationProps) {
  const accentOpacity = isSelected ? 0.95 : 0.5;

  return (
    <svg
      aria-hidden="true"
      className={illustrationClassName}
      fill="none"
      viewBox="0 0 96 64"
    >
      <path
        d="M18 57V9h60v48M12 57h12M72 57h12"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2.2"
      />
      <path
        d="M18 17h60"
        opacity={accentOpacity}
        stroke="var(--primary)"
        strokeLinecap="round"
        strokeWidth="2.8"
      />
      <circle
        cx="48"
        cy="28"
        r="5"
        stroke="currentColor"
        strokeWidth="2"
      />
      <path
        d="m44 34-8-13M52 34l8-13M48 33v13m0 0-8 10m8-10 8 10"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2.2"
      />
      <path
        d="M33 21h6M57 21h6"
        opacity={isSelected ? 0.9 : 0.45}
        stroke="var(--secondary)"
        strokeLinecap="round"
        strokeWidth="2.4"
      />
    </svg>
  );
}

export function CommercialGymIllustration({
  isSelected,
}: EnvironmentIllustrationProps) {
  const accentOpacity = isSelected ? 0.95 : 0.5;

  return (
    <svg
      aria-hidden="true"
      className={illustrationClassName}
      fill="none"
      viewBox="0 0 96 64"
    >
      <path
        d="M17 57V9h62v48M12 57h72"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2.2"
      />
      <path
        d="M25 20h46M28 15v10M34 17v6M62 17v6M68 15v10"
        opacity={accentOpacity}
        stroke="var(--primary)"
        strokeLinecap="round"
        strokeWidth="2.5"
      />
      <path
        d="M27 53V32h15v21M31 38h7M31 44h7M54 53V31h15v22M58 36h7"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2"
      />
      <path
        d="M52 48h19M55 44l-3 8M68 44l3 8"
        opacity={isSelected ? 0.9 : 0.45}
        stroke="var(--secondary)"
        strokeLinecap="round"
        strokeWidth="2.2"
      />
    </svg>
  );
}

export type { EnvironmentIllustrationProps };
