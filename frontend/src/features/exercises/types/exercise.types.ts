import type { CategoriaEquipamiento } from "@/features/training-profile/onboarding/types/equipment.types";

export type ExerciseEquipment = {
  id: string;
  nombre: string;
  categoria: CategoriaEquipamiento;
};

export type Exercise = {
  id: string;
  nombre: string;
  descripcion: string | null;
  grupoMuscularPrincipal: string;
  equipamientos: ExerciseEquipment[];
};
