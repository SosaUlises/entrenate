export type ExerciseMuscleFilter = {
  id: string;
  label: string;
  imageSrc: string;
  backendValues: readonly string[] | null;
};

const NEUTRAL_MUSCLE_MAP = "/muscle-maps/muscle-map-todos-neutral.png";

export const ALL_MUSCLE_FILTER_ID = "all";

const primaryMuscleFilters: readonly ExerciseMuscleFilter[] = [
  {
    id: ALL_MUSCLE_FILTER_ID,
    label: "Todos",
    imageSrc: NEUTRAL_MUSCLE_MAP,
    backendValues: null,
  },
  {
    id: "chest",
    label: "Pecho",
    imageSrc: "/muscle-maps/muscle-map-pecho-frontal.png",
    backendValues: ["Pecho"],
  },
  {
    id: "back",
    label: "Espalda",
    imageSrc: "/muscle-maps/muscle-map-espalda-posterior.png",
    backendValues: ["Espalda"],
  },
  {
    id: "shoulders",
    label: "Hombros",
    imageSrc: "/muscle-maps/muscle-map-hombros-dual.png",
    backendValues: ["Hombros"],
  },
  {
    id: "arms",
    label: "Brazos",
    imageSrc: "/muscle-maps/muscle-map-brazos-dual.png",
    backendValues: ["Bíceps", "Tríceps"],
  },
  {
    id: "legs",
    label: "Piernas",
    imageSrc: "/muscle-maps/muscle-map-piernas-dual.png",
    backendValues: ["Cuádriceps", "Isquiotibiales", "Glúteos", "Gemelos"],
  },
  {
    id: "core",
    label: "Core",
    imageSrc: "/muscle-maps/muscle-map-core-frontal.png",
    backendValues: ["Core"],
  },
];

const knownBackendValues = new Set(
  primaryMuscleFilters.flatMap((filter) => filter.backendValues ?? []),
);

export function getVisibleMuscleFilters(
  backendGroups: readonly string[],
): ExerciseMuscleFilter[] {
  const availableGroups = new Set(backendGroups);
  const visiblePrimaryFilters = primaryMuscleFilters.filter(
    (filter) =>
      filter.id === ALL_MUSCLE_FILTER_ID ||
      filter.backendValues?.some((value) => availableGroups.has(value)),
  );
  const unknownFilters = Array.from(availableGroups)
    .filter((value) => value.length > 0 && !knownBackendValues.has(value))
    .map((value) => ({
      id: `unknown:${value}`,
      label: value,
      imageSrc: NEUTRAL_MUSCLE_MAP,
      backendValues: [value],
    }));

  return [...visiblePrimaryFilters, ...unknownFilters];
}

export function matchesMuscleFilter(
  backendGroup: string,
  filter: ExerciseMuscleFilter,
): boolean {
  return filter.backendValues === null || filter.backendValues.includes(backendGroup);
}
