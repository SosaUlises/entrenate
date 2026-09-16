export type TrainingSet = {
  id: string;
  numeroSerie: number;
  peso: number;
  repeticiones: number;
  rir: number | null;
  completada: boolean;
  fechaHoraRegistro: string;
};

export type TrainingSessionExercise = {
  id: string;
  ejercicioId: string;
  nombre: string;
  orden: number;
  seriesObjetivo: number;
  repeticionesMinimasObjetivo: number;
  repeticionesMaximasObjetivo: number;
  rirObjetivoMinimo: number;
  rirObjetivoMaximo: number;
  descansoObjetivoSegundos: number;
  notas: string | null;
  series: TrainingSet[];
};

export type TrainingSession = {
  id: string;
  diaRutinaId: string | null;
  fecha: string;
  horaInicio: string;
  horaFin: string | null;
  estado: number;
  ejercicios: TrainingSessionExercise[];
};

export type TrainingSetInput = {
  peso: number;
  repeticiones: number;
  rir: number | null;
};
