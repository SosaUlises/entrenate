export const CategoriaEquipamiento = {
  PesasLibres: 1,
  Barras: 2,
  BancosYRacks: 3,
  Poleas: 4,
  Maquinas: 5,
  Calistenia: 6,
  Otros: 7,
} as const;

export type CategoriaEquipamiento =
  (typeof CategoriaEquipamiento)[keyof typeof CategoriaEquipamiento];

export type Equipment = {
  categoria: CategoriaEquipamiento;
  id: string;
  nombre: string;
};
