import type { TrainingSet } from "@/features/training/types/training.types";

export type ExerciseProgressBestSet = {
  numeroSerie: number;
  peso: number;
  repeticiones: number;
  rir: number | null;
};

export type ExerciseProgressEntry = {
  sessionId: string;
  fecha: string;
  horaInicio: string;
  series: TrainingSet[];
  volumenTotal: number | null;
  mejorSerie: ExerciseProgressBestSet | null;
  e1RmEstimado: number | null;
};

export type ExerciseProgressSummary = {
  primerE1Rm: number | null;
  ultimoE1Rm: number | null;
  mejorE1Rm: number | null;
  cambioAbsoluto: number | null;
  cambioPorcentual: number | null;
};

export type ExerciseProgress = {
  exerciseId: string;
  nombre: string;
  entrenamientos: ExerciseProgressEntry[];
  resumen: ExerciseProgressSummary;
};

export type ExerciseProgressHubItem = {
  ejercicioId: string;
  nombre: string;
  grupoMuscularPrincipal: string;
  ultimoEntrenamiento: string;
  cantidadSesiones: number;
  mejorSerieUltimaSesion: ExerciseProgressBestSet | null;
  ultimaSerieRegistrada: ExerciseProgressBestSet | null;
  e1RmActual: number | null;
  cambioPorcentual: number | null;
  recentTrend: Array<{
    horaInicio: string;
    value: number;
  }> | null;
};

export type ExerciseProgressHub = {
  ejercicios: ExerciseProgressHubItem[];
};

export type ExerciseProgressTrendPoint = {
  sessionId: string;
  horaInicio: string;
  e1RmEstimado: number | null;
  pesoMaximo: number | null;
  repeticionesMaximas: number;
  volumenSesion: number | null;
  volumenMaximoSerie: number | null;
  seriesCompletadas: number;
  repeticionesTotales: number;
  mejorSerie: ExerciseProgressBestSet | null;
};

export type ExerciseProgressTrend = {
  exerciseId: string;
  nombre: string;
  tieneHistorial: boolean;
  points: ExerciseProgressTrendPoint[];
};
