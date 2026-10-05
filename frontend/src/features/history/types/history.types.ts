export type TrainingSessionStatus = 1 | 2 | 3;

export type TrainingSessionHistorySummary = {
  id: string;
  diaRutinaId: string | null;
  fecha: string;
  horaInicio: string;
  horaFin: string | null;
  estado: TrainingSessionStatus;
  cantidadEjercicios: number;
  cantidadSeriesCompletadas: number;
};

export const TRAINING_SESSION_STATUS = {
  inProgress: 1,
  completed: 2,
  cancelled: 3,
} as const satisfies Record<string, TrainingSessionStatus>;
