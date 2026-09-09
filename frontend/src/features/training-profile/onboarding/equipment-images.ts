const equipmentImageByName = new Map<string, string>([
  ["Discos", "/equipment/equipment-discos.png"],
  ["Mancuernas", "/equipment/equipment-dumbbells.png"],
  ["Pesas rusas", "/equipment/equipment-pesas-rusas.png"],
  ["Barra EZ", "/equipment/equipment-barra-ez.png"],
  ["Barra hexagonal", "/equipment/equipment-barra-hexagonal.png"],
  ["Barra olímpica", "/equipment/equipment-barra-olimpica.png"],
  ["Banco ajustable", "/equipment/equipment-banco-ajustable.png"],
  ["Banco plano", "/equipment/equipment-banco-plano.png"],
  ["Jaula de potencia", "/equipment/equipment-jaula-potencia.png"],
  ["Rack", "/equipment/equipment-rack.png"],
  ["Polea alta", "/equipment/equipment-polea-alta.png"],
  ["Polea baja", "/equipment/equipment-polea-baja.png"],
  ["Polea doble", "/equipment/equipment-polea-doble.png"],
  ["Curl femoral", "/equipment/equipment-curl-femoral.png"],
  [
    "Extensión de cuádriceps",
    "/equipment/equipment-extension-cuadriceps.png",
  ],
  ["Hack squat", "/equipment/equipment-hack-squat.png"],
  ["Máquina de gemelos", "/equipment/equipment-maquina-gemelos.png"],
  ["Máquina Smith", "/equipment/equipment-maquina-smith.png"],
  ["Peck deck", "/equipment/equipment-peck-deck.png"],
  ["Prensa de piernas", "/equipment/equipment-prensa-piernas.png"],
  ["Anillas", "/equipment/equipment-anillas.png"],
  ["Barra de dominadas", "/equipment/equipment-barra-dominadas.png"],
  ["Paralelas", "/equipment/equipment-paralelas.png"],
  ["Bandas elásticas", "/equipment/equipment-bandas-elasticas.png"],
]);

export function getEquipmentImagePath(
  equipmentName: string,
): string | undefined {
  return equipmentImageByName.get(equipmentName);
}
