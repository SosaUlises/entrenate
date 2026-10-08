import {
  CategoriaEquipamiento,
  DiaSemana,
  EntornoEntrenamiento,
  NivelExperiencia,
  ObjetivoEntrenamiento,
  Sexo,
  type CategoriaEquipamiento as CategoriaEquipamientoValue,
  type DiaSemana as DiaSemanaValue,
  type EntornoEntrenamiento as EntornoEntrenamientoValue,
  type NivelExperiencia as NivelExperienciaValue,
  type ObjetivoEntrenamiento as ObjetivoEntrenamientoValue,
  type Sexo as SexoValue,
} from "./training-profile.types";

export const objectiveLabels: Record<ObjetivoEntrenamientoValue, string> = {
  [ObjetivoEntrenamiento.GanarMasaMuscular]: "Ganar masa muscular",
  [ObjetivoEntrenamiento.GanarFuerza]: "Ganar fuerza",
  [ObjetivoEntrenamiento.GanarmasaMuscularYFuerza]: "Masa muscular y fuerza",
  [ObjetivoEntrenamiento.AcondicionamientoGeneral]: "Acondicionamiento general",
};

export const experienceLabels: Record<NivelExperienciaValue, string> = {
  [NivelExperiencia.SinExperiencia]: "Sin experiencia",
  [NivelExperiencia.Principiante]: "Principiante",
  [NivelExperiencia.Intermedio]: "Intermedio",
  [NivelExperiencia.Avanzado]: "Avanzado",
};

export const environmentLabels: Record<EntornoEntrenamientoValue, string> = {
  [EntornoEntrenamiento.Casa]: "Casa",
  [EntornoEntrenamiento.GimnasioPequeno]: "Gimnasio pequeño",
  [EntornoEntrenamiento.Calistenia]: "Calistenia",
  [EntornoEntrenamiento.GimnasioComercial]: "Gimnasio comercial",
};

export const sexLabels: Record<SexoValue, string> = {
  [Sexo.Masculino]: "Masculino",
  [Sexo.Femenino]: "Femenino",
  [Sexo.PrefieroNoInformarlo]: "Prefiero no informarlo",
};

export const dayOptions: ReadonlyArray<{ label: string; value: DiaSemanaValue }> = [
  { label: "Lun", value: DiaSemana.Lunes },
  { label: "Mar", value: DiaSemana.Martes },
  { label: "Mié", value: DiaSemana.Miercoles },
  { label: "Jue", value: DiaSemana.Jueves },
  { label: "Vie", value: DiaSemana.Viernes },
  { label: "Sáb", value: DiaSemana.Sabado },
  { label: "Dom", value: DiaSemana.Domingo },
];

export const equipmentCategoryDefinitions: ReadonlyArray<{
  label: string;
  value: CategoriaEquipamientoValue;
}> = [
  { label: "Pesas libres", value: CategoriaEquipamiento.PesasLibres },
  { label: "Barras", value: CategoriaEquipamiento.Barras },
  { label: "Bancos y racks", value: CategoriaEquipamiento.BancosYRacks },
  { label: "Poleas", value: CategoriaEquipamiento.Poleas },
  { label: "Máquinas", value: CategoriaEquipamiento.Maquinas },
  { label: "Calistenia", value: CategoriaEquipamiento.Calistenia },
  { label: "Otros", value: CategoriaEquipamiento.Otros },
];

export const objectiveOptions = Object.entries(objectiveLabels).map(
  ([value, label]) => ({ label, value: Number(value) as ObjetivoEntrenamientoValue }),
);

export const experienceOptions = Object.entries(experienceLabels).map(
  ([value, label]) => ({ label, value: Number(value) as NivelExperienciaValue }),
);

export const environmentOptions = Object.entries(environmentLabels).map(
  ([value, label]) => ({ label, value: Number(value) as EntornoEntrenamientoValue }),
);

export const sexOptions = Object.entries(sexLabels).map(([value, label]) => ({
  label,
  value: Number(value) as SexoValue,
}));

export function formatPreferredDays(days: DiaSemanaValue[]): string {
  if (days.length === 0) return "Sin días fijos";

  const labels = new Map(dayOptions.map((day) => [day.value, day.label]));
  return [...days]
    .sort((firstDay, secondDay) => firstDay - secondDay)
    .map((day) => labels.get(day) ?? String(day))
    .join(" · ");
}

export function formatWeight(weight: number | null): string {
  return weight === null
    ? "No informado"
    : `${new Intl.NumberFormat("es-AR", { maximumFractionDigits: 2 }).format(weight)} kg`;
}
