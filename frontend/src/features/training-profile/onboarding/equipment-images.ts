const equipmentImageByName = new Map<string, string>([
  ["Discos", "/equipment/equipment-discos-v5.png"],
  ["Mancuernas", "/equipment/equipment-dumbbells-v5.png"],
  ["Pesas rusas", "/equipment/equipment-pesas-rusas-v5.png"],
  ["Barra EZ", "/equipment/equipment-barra-ez-v5.png"],
  ["Barra hexagonal", "/equipment/equipment-barra-hexagonal-v5.png"],
  ["Barra olímpica", "/equipment/equipment-barra-olimpica-v5.png"],
  ["Banco ajustable", "/equipment/equipment-banco-ajustable-v5.png"],
  ["Banco plano", "/equipment/equipment-banco-plano-v5.png"],
  ["Jaula de potencia", "/equipment/equipment-jaula-potencia-v5.png"],
  ["Rack", "/equipment/equipment-rack-v5.png"],
  ["Polea alta", "/equipment/equipment-polea-alta-v5.png"],
  ["Polea baja", "/equipment/equipment-polea-baja-v5.png"],
  ["Polea doble", "/equipment/equipment-polea-doble-v5.png"],
  ["Curl femoral", "/equipment/equipment-curl-femoral-v5.png"],
  [
    "Extensión de cuádriceps",
    "/equipment/equipment-extension-cuadriceps-v5.png",
  ],
  ["Hack squat", "/equipment/equipment-hack-squat-v5.png"],
  ["Máquina de gemelos", "/equipment/equipment-maquina-gemelos-v5.png"],
  ["Máquina Smith", "/equipment/equipment-maquina-smith-v5.png"],
  ["Peck deck", "/equipment/equipment-peck-deck-v5.png"],
  ["Prensa de piernas", "/equipment/equipment-prensa-piernas-v5.png"],
  ["Anillas", "/equipment/equipment-anillas-v5.png"],
  ["Barra de dominadas", "/equipment/equipment-barra-dominadas-v5.png"],
  ["Paralelas", "/equipment/equipment-paralelas-v5.png"],
  ["Bandas elásticas", "/equipment/equipment-bandas-elasticas-v5.png"],
]);

export function getEquipmentImagePath(
  equipmentName: string,
): string | undefined {
  return equipmentImageByName.get(equipmentName);
}
