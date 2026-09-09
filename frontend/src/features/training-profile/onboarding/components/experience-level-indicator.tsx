type ExperienceLevelIndicatorProps = {
  activeBars: 0 | 1 | 2 | 3;
};

const barHeights = ["h-3", "h-5", "h-7"] as const;

export function ExperienceLevelIndicator({
  activeBars,
}: ExperienceLevelIndicatorProps) {
  return (
    <span
      aria-hidden="true"
      className="flex h-[34px] w-9 items-end justify-center gap-1"
    >
      {barHeights.map((height, index) => (
        <span
          className={`w-1.5 rounded-sm ${height} ${
            index < activeBars ? "bg-primary" : "bg-text-secondary/25"
          }`}
          key={height}
        />
      ))}
    </span>
  );
}
