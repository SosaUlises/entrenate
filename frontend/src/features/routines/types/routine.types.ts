export type RoutineSummary = {
  id: string;
  nombre: string;
  descripcion: string | null;
  activa: boolean;
  cantidadDias: number;
  fechaCreacion: string;
  fechaUltimaModificacion: string;
};

export type RoutineDetail = {
  id: string;
  nombre: string;
  descripcion: string | null;
  activa: boolean;
  fechaCreacion: string;
  fechaUltimaModificacion: string;
  dias: {
    id: string;
    nombre: string;
    descripcion: string | null;
    orden: number;
    ejercicios: {
      ejercicioId: string;
      nombre: string;
      cantidadSeries: number;
      repeticionesMinimas: number;
      repeticionesMaximas: number;
      rirObjetivoMinimo: number;
      rirObjetivoMaximo: number;
      descansoSegundos: number;
      orden: number;
      notas: string | null;
    }[];
  }[];
};

export type RoutineListItem = RoutineSummary & {
  cantidadEjercicios: number;
};

export type CreateRoutineRequest = {
  nombre: string;
  descripcion: string | null;
  dias: {
    nombre: string;
    descripcion: string | null;
    orden: number;
    ejercicios: {
      ejercicioId: string;
      cantidadSeries: number;
      repeticionesMinimas: number;
      repeticionesMaximas: number;
      rirObjetivoMinimo: number;
      rirObjetivoMaximo: number;
      descansoSegundos: number;
      orden: number;
      notas: string | null;
    }[];
  }[];
};

export type UpdateRoutineRequest = CreateRoutineRequest;

export type CreateRoutineResponse = { id: string };
