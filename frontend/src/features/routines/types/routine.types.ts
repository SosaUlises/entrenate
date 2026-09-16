export type RoutineSummary = {
  id: string;
  nombre: string;
  descripcion: string | null;
  activa: boolean;
  cantidadDias: number;
  fechaCreacion: string;
  fechaUltimaModificacion: string;
};

// GET /api/routines/{id} supplies the nested exercises needed for this count.
export type RoutineDetail = {
  dias: { ejercicios: { ejercicioId: string }[] }[];
};

export type RoutineListItem = RoutineSummary & {
  cantidadEjercicios: number;
};
