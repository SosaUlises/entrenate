import Image from "next/image";
import { cn } from "@/lib/class-names";
import {
  ALL_MUSCLE_FILTER_ID,
  type ExerciseMuscleFilter,
} from "../exercise-muscle-filters";

type MuscleFilterRailProps = {
  className?: string;
  filters: readonly ExerciseMuscleFilter[];
  onSelect: (filterId: string) => void;
  selectedId: string;
};

export function MuscleFilterRail({
  className,
  filters,
  onSelect,
  selectedId,
}: MuscleFilterRailProps) {
  return (
    <div className={cn("relative min-w-0", className)}>
      <div
        aria-label="Filtrar por grupo muscular"
        className="flex w-full touch-pan-x gap-1.5 overflow-x-auto overscroll-x-contain pb-1.5 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
        role="group"
      >
        {filters.map((filter) => {
          const selected = filter.id === selectedId;
          const hasTargetMuscle = filter.id !== ALL_MUSCLE_FILTER_ID;
          const imageFilter = selected && hasTargetMuscle
            ? "hue-rotate(119deg) saturate(0.95) brightness(1.03) contrast(1.22)"
            : selected
              ? "grayscale(1) saturate(0) brightness(1.1) contrast(1.25)"
              : "grayscale(1) saturate(0) brightness(0.75) contrast(1.25)";

          return (
            <button
              aria-label={`Filtrar por ${filter.label}`}
              aria-pressed={selected}
              className={cn(
                "relative flex h-[5.75rem] min-w-[4.75rem] shrink-0 flex-col items-center justify-start rounded-control-sm border border-transparent px-1.5 pt-1 pb-2 text-text-secondary transition-[background-color,border-color,color,transform] duration-150 active:scale-[0.99] focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-focus motion-reduce:transition-none motion-reduce:active:scale-100",
                selected
                  ? "border-thermal-accent/45 bg-surface-active/40 text-text-primary"
                  : "hover:border-border/70 hover:bg-surface-interactive/35 hover:text-text-primary",
              )}
              key={filter.id}
              onClick={() => onSelect(filter.id)}
              type="button"
            >
              <Image
                alt=""
                aria-hidden="true"
                className={cn(
                  "size-14 shrink-0 object-contain transition-[filter,opacity,transform] duration-150 motion-reduce:transition-none",
                  filter.id === ALL_MUSCLE_FILTER_ID && "scale-[1.12]",
                  selected ? "opacity-100" : "opacity-70",
                )}
                height={56}
                sizes="56px"
                src={filter.imageSrc}
                style={{ filter: imageFilter }}
                width={56}
              />
              <span
                className={cn(
                  "mt-1 whitespace-nowrap text-[0.8125rem] leading-4",
                  selected ? "font-bold" : "font-semibold",
                )}
              >
                {filter.label}
              </span>
              <span
                aria-hidden="true"
                className={cn(
                  "absolute bottom-0 h-0.5 w-7 rounded-full bg-thermal-accent transition-opacity duration-150 motion-reduce:transition-none",
                  selected ? "opacity-100" : "opacity-0",
                )}
              />
            </button>
          );
        })}
      </div>
    </div>
  );
}
