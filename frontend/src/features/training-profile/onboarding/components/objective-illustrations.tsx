type ObjectiveIllustrationProps = {
  isSelected: boolean;
};

const illustrationClassName = "h-[60px] w-[90px] overflow-visible";

export function MuscleGainIllustration({
  isSelected,
}: ObjectiveIllustrationProps) {
  return (
    <svg
      aria-hidden="true"
      className={illustrationClassName}
      fill="none"
      viewBox="0 0 96 64"
    >
      <g
        opacity={isSelected ? 0.9 : 0.78}
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="0.95"
        transform="translate(21 4) scale(2.25)"
      >
        <path d="M12.409 13.017A5 5 0 0 1 22 15c0 3.866-4 7-9 7-4.077 0-8.153-.82-10.371-2.462-.426-.316-.631-.832-.62-1.362C2.118 12.723 2.627 2 10 2a3 3 0 0 1 3 3 2 2 0 0 1-2 2c-1.105 0-1.64-.444-2-1" />
        <path d="M15 14a5 5 0 0 0-7.584 2" />
        <path d="M9.964 6.825C8.019 7.977 9.5 13 8 15" />
      </g>
    </svg>
  );
}

export function StrengthIllustration({
  isSelected,
}: ObjectiveIllustrationProps) {
  const accentOpacity = isSelected ? 0.72 : 0.36;

  return (
    <svg
      aria-hidden="true"
      className={illustrationClassName}
      fill="none"
      viewBox="0 0 96 64"
    >
      <path
        d="M14 32h68M25 22v20M31 25v14M65 25v14M71 22v20"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="2"
      />
      <path
        d="M17 26v12M21 24v16M75 24v16M79 26v12"
        opacity={accentOpacity}
        stroke="var(--primary)"
        strokeLinecap="round"
        strokeWidth="2.2"
      />
    </svg>
  );
}

export function MuscleAndStrengthIllustration({
  isSelected,
}: ObjectiveIllustrationProps) {
  const accentOpacity = isSelected ? 0.72 : 0.36;

  return (
    <svg
      aria-hidden="true"
      className={illustrationClassName}
      fill="none"
      viewBox="0 0 96 64"
    >
      <g
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.15"
        transform="translate(0 5) scale(1.85)"
      >
        <path d="M12.409 13.017A5 5 0 0 1 22 15c0 3.866-4 7-9 7-4.077 0-8.153-.82-10.371-2.462-.426-.316-.631-.832-.62-1.362C2.118 12.723 2.627 2 10 2a3 3 0 0 1 3 3 2 2 0 0 1-2 2c-1.105 0-1.64-.444-2-1" />
        <path d="M15 14a5 5 0 0 0-7.584 2" />
        <path d="M9.964 6.825C8.019 7.977 9.5 13 8 15" />
      </g>
      <path
        d="M47 35h47"
        opacity={isSelected ? 0.82 : 0.68}
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.8"
      />
      <path
        d="M50 29v12m5-17v22m5-18v14m21-14v14m5-18v22m5-17v12"
        opacity={accentOpacity}
        stroke="var(--primary)"
        strokeLinecap="round"
        strokeWidth="2"
      />
    </svg>
  );
}

export function GeneralConditioningIllustration({
  isSelected,
}: ObjectiveIllustrationProps) {
  const accentOpacity = isSelected ? 0.72 : 0.36;

  return (
    <svg
      aria-hidden="true"
      className={illustrationClassName}
      fill="none"
      viewBox="0 0 96 64"
    >
      <path
        d="M48 54S20 39 20 22c0-8 6-13 13-13 6 0 11 3 15 9 4-6 9-9 15-9 7 0 13 5 13 13 0 17-28 32-28 32Z"
        stroke="currentColor"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
      <path
        d="M13 33h19l5-10 8 21 7-16 5 5h26"
        opacity={accentOpacity}
        stroke="var(--primary)"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2"
      />
    </svg>
  );
}

export type { ObjectiveIllustrationProps };
