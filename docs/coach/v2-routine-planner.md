# Entrenate Coach V2 — Routine Planner

## 1. Estado y propósito

Estado: V2.3 cerrado — estructura semanal, selección, prescripción inicial y
control determinístico de duración implementados.

V2 propone una rutina de entrenamiento a partir del perfil del usuario y del
catálogo real de Entrenate. La planificación es determinística y produce una
propuesta estructurada antes de cualquier escritura.

```text
perfil + catálogo vigente
        ↓
motor determinístico de planificación
        ↓
propuesta estructurada y revisable
        ↓
confirmación explícita del usuario
        ↓
creación mediante las reglas normales de Rutinas
```

Coach puede explicar la propuesta y conversar sobre ella. No puede inventar
ejercicios, decidir libremente la planificación ni persistir cambios por sí
mismo.

Esta especificación complementa `docs/coach.md`; no redefine la identidad ni
las restricciones globales de Coach.

## 2. Alcance inicial

V2 debe:

- generar una rutina nueva desde el perfil de entrenamiento vigente;
- utilizar exclusivamente ejercicios activos del catálogo;
- respetar equipamiento, días y duración disponibles;
- producir prescripción completa por ejercicio;
- permitir revisar la propuesta sin persistirla;
- crear la rutina sólo después de confirmación explícita;
- reutilizar las validaciones y el flujo de creación de rutinas existentes.

V2 no debe:

- generar ejercicios o identificadores inexistentes;
- calcular cargas iniciales ni porcentajes de 1RM;
- modificar una rutina existente sin un flujo específico de revisión y
  confirmación;
- adaptar por lesiones, molestias, fatiga o nutrición;
- usar un LLM para decidir split, ejercicios, volumen o prescripción;
- activar progresión automática. La progresión sigue perteneciendo a Progress
  Coach.

## 3. Capacidades y límites del modelo actual

### 3.1 Perfil disponible

`PerfilEntrenamiento` ya contiene:

- `Objetivo`;
- `NivelExperiencia`;
- `Edad`;
- `Sexo` opcional;
- `PesoKg` opcional;
- `DiasEntrenamientoPorSemana`, actualmente entre 1 y 7;
- `DuracionSesionMinutos`, actualmente entre 15 y 240;
- `EntornoEntrenamiento`;
- cero o más `DiasPreferidos` sin duplicados;
- cero o más `Equipamientos` sin duplicados.

Los días preferidos pueden ser menos que los días semanales solicitados. No son
actualmente una agenda completa obligatoria.

### 3.2 Rutina persistida

La estructura actual permite persistir:

- `Rutina`: usuario, nombre, descripción y estado;
- `DiaRutina`: nombre, descripción y orden;
- `EjercicioRutina`: ejercicio, series, rango de repeticiones, rango de RIR,
  descanso, orden y notas.

Los límites vigentes que una propuesta confirmable debe respetar son:

- 1 a 20 series por ejercicio;
- 1 a 100 repeticiones, con mínimo menor o igual al máximo;
- RIR entre 0 y 5, con mínimo menor o igual al máximo;
- descanso entre 15 y 900 segundos;
- órdenes positivos y únicos dentro de su nivel;
- nombres de rutina y día de hasta 100 caracteres;
- descripciones y notas de hasta 500 caracteres;
- al menos un día por rutina;
- todos los `ExerciseId` deben existir y estar activos.

`DiaRutina` tiene un `DiaSemana?` tipado y nullable. Las rutinas existentes
conservan `null`; las nuevas rutinas generadas por el planner deben persistir el
día propuesto. Los contratos actuales de creación y lectura lo exponen de forma
aditiva.

### 3.3 Catálogo disponible

Cada ejercicio activo puede exponer:

- `Id` real;
- `Codigo` estable y único para el catálogo curado;
- nombre y descripción;
- grupo muscular principal como texto;
- grupos musculares secundarios explícitos;
- `PatronMovimiento` tipado;
- `TipoEjercicio` compuesto o aislamiento;
- equipamientos requeridos;
- `WorkoutGuideId` opcional.

La metadata de planificación es nullable para preservar ejercicios existentes
o incorporados fuera del seed. Un ejercicio sin metadata completa no es
elegible para selección automática. La dificultad técnica, bilateralidad,
prioridad y alternativas equivalentes no se persisten en V2.0 porque ninguna
regla inicial las necesita. V2 no debe inferirlas a partir del nombre, texto
libre ni un LLM.

## 4. Entradas del planner

El caso de uso construirá un input inmutable con un snapshot del perfil y una
proyección del catálogo activo.

### 4.1 Entradas que sí influyen en V2

| Entrada | Uso normativo |
| --- | --- |
| Objetivo | Define énfasis, rangos de repeticiones y descansos. |
| Experiencia | Define volumen inicial, cantidad de ejercicios y complejidad admitida. |
| Días por semana | Selecciona el split y la cantidad exacta de días propuestos. |
| Días preferidos | Se conservan como restricciones de calendario; los faltantes se infieren y se marcan. |
| Duración por sesión | Limita el presupuesto estimado de ejercicios y series. |
| Entorno | Sirve como contexto estructurado y desempate, nunca como equipamiento implícito. |
| Equipamiento | Determina elegibilidad de ejercicios. |
| Catálogo activo | Es la única fuente de `ExerciseId`, equipamiento y metadata de planificación. |

### 4.2 Entradas disponibles que V2 no debe usar inicialmente

- **Edad:** se conserva en el snapshot, pero no cambia volumen, ejercicios ni
  esfuerzo. La edad sola no permite inferir salud, capacidad o restricciones.
- **Sexo:** no modifica la rutina inicial. No hay una regla de producto que
  justifique una prescripción distinta.
- **Peso:** no se usa mientras V2 no prescriba cargas ni tenga una regla formal
  para progresiones de peso corporal.

Estos datos no se envían a un futuro LLM si no participan en una explicación.
Incorporarlos al motor requerirá una regla explícita y testeable, no una
heurística implícita.

## 5. Metadata mínima de planificación del catálogo

Cada ejercicio elegible debe tener metadata persistida, curada y estable:

- `Codigo`: slug único, independiente del GUID y del nombre visible;
- `PatronMovimiento`: enum explícito para empujes, tracciones, tren inferior,
  brazos, hombros, gemelos y core;
- `TipoEjercicio`: `Compuesto` o `Aislamiento`;
- `GrupoMuscularPrincipal`: se reutiliza el campo existente;
- `GruposMuscularesSecundarios`: relación explícita sin repetir el principal;
- equipamientos requeridos con semántica de “todos requeridos”.

`Primary`, `Secondary` y `Accessory` son roles contextuales asignados por el
planner a un cupo de la sesión; no son propiedades intrínsecas del ejercicio y
no se persisten en el catálogo. Tampoco se persiste dificultad en V2.0: agregarla
sin una regla consumidora sería metadata especulativa.

La metadata pertenece al catálogo y no depende de GUID hardcodeados.
`WorkoutGuideId` continúa siendo una integración opcional y no sustituye a
`Codigo`.

Si un ejercicio activo no tiene metadata de planificación, sigue visible en el
catálogo general pero V2 no lo selecciona automáticamente. La propuesta debe
informar esta exclusión como diagnóstico, no completar los datos por inferencia.

## 6. Salida estructurada no persistida

La salida recomendada es conceptualmente:

```text
RoutineProposal
  TrainingProfileId
  ProfileUpdatedAtUtc
  PlannerPolicyVersion
  Name
  Description?
  Goal
  ExperienceLevel
  RequestedDaysPerWeek
  EstimatedWeeklyMinutes
  Feasibility
  ReasonCodes[]
  Warnings[]
  Days[]

RoutineProposalDay
  Order
  ScheduledDay?
  ScheduledDayWasInferred
  Focus
  EstimatedDurationMinutes
  Exercises[]

RoutineProposalExercise
  ExerciseId
  Name
  Order
  MovementPattern
  ExerciseRole
  Sets
  MinReps
  MaxReps
  MinRir
  MaxRir
  RestSeconds
  Notes?
```

`Name` en ejercicios es informativo; `ExerciseId` es la referencia normativa.
El planner debe obtener ambos del mismo snapshot de catálogo.

`Feasibility` debe diferenciar al menos:

- `Feasible`;
- `FeasibleWithWarnings`;
- `NotFeasible`.

Los `ReasonCodes` y warnings permiten explicar, por ejemplo, catálogo
insuficiente, tiempo ajustado, días inferidos o ausencia de una alternativa de
equipamiento. La propuesta no debe aparentar certeza si faltan patrones
esenciales.

`Notes` queda `null` por defecto. Sólo se usa para una indicación estructurada
necesaria ya definida por producto; el planner no inventa instrucciones
técnicas en texto libre.

## 7. Pipeline determinístico

El orden de ejecución es normativo:

1. validar el snapshot del perfil;
2. cargar ejercicios activos y su metadata de planificación;
3. filtrar por equipamiento;
4. construir los días solicitados y asignar el split;
5. calcular cuotas de patrones y volumen;
6. seleccionar ejercicios;
7. prescribir series, repeticiones, RIR y descanso;
8. estimar duración;
9. ajustar dentro de límites sin eliminar cobertura esencial;
10. validar invariantes y devolver propuesta o `NotFeasible`.

No se persiste nada durante este pipeline.

## 8. Elegibilidad por equipamiento

Una selección es elegible únicamente cuando:

- el ejercicio existe y está activo;
- tiene metadata completa para el planner;
- no requiere equipamiento, o todos sus `EquipmentId` requeridos aparecen en el
  perfil;
- todo equipamiento referenciado sigue activo.

Un ejercicio sin relaciones de equipamiento se considera de peso corporal.

El entorno no concede acceso implícito a equipamiento. Por ejemplo,
`GimnasioComercial` no habilita automáticamente todas las máquinas si el perfil
no las declaró. Esta regla evita inventar disponibilidad.

Si no queda ningún ejercicio para un patrón, el motor intenta una alternativa
curada del mismo patrón. Si tampoco existe, devuelve un warning o
`NotFeasible`, según si el patrón es esencial para ese split.

## 9. Distribución semanal inicial

| Días | Split V2 inicial | Orden de focos |
| ---: | --- | --- |
| 1 | Full Body | Full Body A |
| 2 | Full Body A/B | Full Body A, Full Body B |
| 3 | Full Body A/B/C | Full Body A, Full Body B, Full Body C |
| 4 | Upper/Lower | Upper A, Lower A, Upper B, Lower B |
| 5 | Upper/Lower + PPL | Upper, Lower, Push, Pull, Legs |
| 6 | PPL A/B | Push A, Pull A, Legs A, Push B, Pull B, Legs B |
| 7 | PPL A/B + accesorios | PPL A/B y un día corto de Core/Calves |

El séptimo día no agrega otra exposición pesada. Debe usar sólo volumen
accesorio que ya esté dentro del presupuesto semanal. Si el catálogo o la
duración no permiten un día útil sin exceder volumen, el resultado es
`NotFeasible`; el motor no reduce silenciosamente la cantidad solicitada.

### 9.1 Calendario

- Si la cantidad de días preferidos coincide con la cantidad solicitada, se
  usan exactamente esos días.
- Si hay días preferidos parciales, todos se conservan y los faltantes se
  completan buscando maximizar la separación circular entre sesiones.
- Si no hay días preferidos, se distribuyen de forma aproximadamente uniforme
  en la semana.
- Los empates se resuelven de lunes a domingo.
- Cada día inferido se marca con `ScheduledDayWasInferred = true` para permitir
  revisión.

Para que esta política sea reproducible, V2.1 evalúa las combinaciones que
contienen todos los días preferidos y compara sus distancias circulares
ordenadas de menor a mayor. Maximiza primero la menor distancia, luego las
restantes en orden; si dos calendarios siguen empatados, elige la secuencia de
días más temprana de lunes a domingo.

La asignación nunca cambia la cantidad de días solicitada. Días consecutivos
elegidos explícitamente se respetan; el split debe evitar repetir el mismo foco
principal en esos días.

## 10. Cobertura y frecuencia

Los patrones esenciales son:

- tren superior: empuje y tracción;
- tren inferior: dominante de rodilla y bisagra/extensión de cadera;
- core: cobertura semanal;
- gemelos y brazos: accesorios, no sustituyen patrones esenciales.

Reglas iniciales:

- con un día semanal, cada patrón esencial aparece una vez;
- con dos o más días, cada patrón esencial debe tener al menos dos exposiciones
  semanales cuando el split y el tiempo sean factibles;
- un mismo patrón no recibe dos ejercicios primarios en una sesión antes de
  cubrir los demás patrones requeridos;
- grupos pequeños no desplazan un patrón esencial por falta de tiempo;
- el mismo ejercicio puede repetirse en distintos días si no existe una
  alternativa elegible, pero nunca se duplica dentro del mismo día;
- las variantes A/B prefieren ejercicios distintos del mismo patrón, sin hacer
  de la variedad un requisito que vuelva inviable la propuesta.

## 11. Volumen inicial

El volumen se expresa como series efectivas semanales. No incluye calentamiento.
Cada serie aporta `1.0` al grupo muscular principal. Si el ejercicio es
`Compuesto`, aporta `0.5` a cada grupo secundario persistido. Los ejercicios de
`Aislamiento` no aportan volumen secundario en V2.0, aunque en el futuro el
catálogo incorpore secundarios para fines descriptivos.

La contribución secundaria sólo sirve para evitar sobreasignación y comprobar
los rangos semanales. No reemplaza una exposición principal requerida por el
split y no se redondea por ejercicio; se suma como decimal durante toda la
semana.

| Experiencia | Series por patrón principal/semana | Series directas por accesorio/semana | Tope orientativo por sesión |
| --- | ---: | ---: | ---: |
| Sin experiencia | 4–6 | 0–2 | 10 |
| Principiante | 6–8 | 2–4 | 14 |
| Intermedio | 8–12 | 4–6 | 18 |
| Avanzado | 10–14 | 4–8 | 22 |

Aplicación por objetivo:

- `GanarFuerza`: usa la mitad inferior para patrones primarios y limita
  accesorios;
- `GanarMasaMuscular`: usa el centro o mitad superior si entra en tiempo;
- objetivo combinado: usa el centro del rango;
- `AcondicionamientoGeneral`: usa la mitad inferior con cobertura equilibrada.

V2.3 fija esos puntos de forma determinística: fuerza y acondicionamiento usan
el mínimo, el objetivo combinado usa el punto medio entero e hipertrofia usa el
máximo. Para compuestos, el presupuesto se calcula por `PatronMovimiento`; para
aislamientos, por grupo muscular principal. Si la cantidad de exposiciones y el
rango permitido por ejercicio impiden alcanzar el rango semanal, se conserva
la cobertura y se emite `WeeklyVolumeTargetUnreachable`.

La duración siempre puede reducir el volumen hacia el mínimo del rango. El
motor no baja del mínimo sin emitir `FeasibleWithWarnings`; si tampoco cabe una
propuesta utilizable, devuelve `NotFeasible`.

La distribución entre exposiciones debe diferir como máximo en una serie para
un mismo patrón, salvo que el foco del split exija una diferencia explícita.

## 12. Selección de ejercicios

### 12.1 Slots por foco

V2.2 usa la siguiente matriz antes de prescribir volumen. Los slots esenciales
son obligatorios; si no tienen un candidato elegible, la selección es
`NotFeasible`. La ausencia de un accesorio opcional conserva el slot vacío y
produce un warning estructurado.

| Foco | Slots compuestos obligatorios | Core obligatorio | Otros slots |
| --- | --- | --- | --- |
| Full Body | dominante de rodilla; bisagra o extensión de cadera; empuje horizontal o vertical; tracción horizontal o vertical | anti-extensión o flexión | ninguno |
| Upper | empuje horizontal; tracción horizontal; empuje vertical; tracción vertical | no | abducción de hombro; flexión de codo; extensión de codo |
| Lower | dominante de rodilla; bisagra o extensión de cadera | anti-extensión o flexión | extensión de rodilla; flexión de rodilla; flexión plantar |
| Push | empuje horizontal; empuje vertical | no | abducción de hombro; extensión de codo |
| Pull | tracción vertical; tracción horizontal | no | flexión de codo; rotación externa de hombro |
| Legs | dominante de rodilla; bisagra o extensión de cadera | anti-extensión o flexión | extensión de rodilla; flexión de rodilla; flexión plantar |
| Core/Calves | ninguno | anti-extensión o flexión | flexión plantar obligatoria para este foco |

Los slots compuestos se ordenan antes que los accesorios. Un ejercicio no se
repite dentro de una misma sesión. La matriz representa cobertura y selección;
no implica todavía una cantidad de series ni garantiza que todos los accesorios
se mantengan después del ajuste de duración de V2.3.

### 12.2 Elegibilidad y ranking

La compatibilidad de equipamiento usa semántica `AND`: todos los equipamientos
requeridos por el ejercicio deben estar activos en el catálogo y declarados por
el usuario. Una colección vacía representa un ejercicio de peso corporal. El
entorno no agrega equipamiento implícito.

Para cada cupo de patrón, el ranking determinístico es:

1. elegibilidad por actividad, metadata y equipamiento;
2. `TipoEjercicio` requerido por el cupo contextual;
3. menor cantidad de slots ya asignados al patrón y grupo principal;
4. menor uso previo dentro del mismo foco para favorecer variantes A/B/C;
5. penalización por repetir el ejercicio en la sesión inmediatamente anterior;
6. menor uso total semanal del ejercicio;
7. `Codigo` ordinal como desempate estable;
8. `ExerciseId` sólo como desempate final defensivo.

En V2.2 la cantidad de slots es la única medida disponible: todavía no existe
volumen prescripto. V2.3 reemplazará esa aproximación por series semanales donde
corresponda, sin alterar la elegibilidad.

Si una variante debe reutilizar un ejercicio porque no quedan alternativas
elegibles, la selección continúa y emite
`ExerciseReusedAcrossVariants`. La falta normal de candidatos nunca se expresa
mediante una excepción.

No existe ranking por dificultad en V2 inicial. Cuando dos ejercicios siguen
empatados, el código estable produce una selección reproducible sin depender
del GUID, nombre visible o `WorkoutGuideId`.

Orden dentro de una sesión:

1. ejercicios primarios compuestos;
2. ejercicios secundarios compuestos;
3. aislamientos y accesorios;
4. core y gemelos.

No se usa aleatoriedad. El mismo input, catálogo y versión de política deben
producir la misma propuesta.

## 13. Prescripción inicial

### 13.1 Repeticiones y descansos

| Objetivo | Ejercicio primario compuesto | Secundario/accesorio | Descanso primario | Descanso accesorio |
| --- | --- | --- | ---: | ---: |
| Fuerza | 4–6 | 6–10 | 180 s | 90–120 s |
| Hipertrofia | 6–12 | 10–15 | 120 s | 60–90 s |
| Combinado | 5–8 | 8–12 | 150 s | 90 s |
| Acondicionamiento general | 8–12 | 10–15 | 90 s | 60 s |

Cuando el descanso accesorio es un rango, V2.3 usa el extremo superior para un
compuesto secundario y el inferior para un aislamiento: fuerza asigna 120/90 s
e hipertrofia 90/60 s, respectivamente.

### 13.2 RIR

- Sin experiencia: 3–4.
- Principiante: 2–3.
- Intermedio: 2–3.
- Avanzado: 1–3.

V2 no prescribe fallo muscular. Los rangos respetan los límites actuales del
modelo y son puntos de partida; Progress Coach puede recomendar cambios en
exposiciones posteriores.

### 13.3 Series por ejercicio

- Sin experiencia: 2 series.
- Principiante: 2–3 series.
- Intermedio: 3 series.
- Avanzado: 3–4 series.

El motor reparte el presupuesto semanal y luego limita cada ejercicio a estos
rangos. No compensa falta de ejercicios concentrando más de cuatro series en un
ejercicio durante V2 inicial.

La distribución empieza en el mínimo por ejercicio y agrega una serie por
exposición, en orden semanal y de slot, hasta alcanzar el punto objetivo o el
máximo disponible. Para un mismo patrón o grupo, las exposiciones difieren como
máximo en una serie. Los mínimos por ejercicio son duros; el rango semanal es
un objetivo y puede quedar fuera sólo con warning cuando la cobertura y la
cantidad real de exposiciones lo hacen inevitable.

## 14. Estimación y ajuste de duración

V2 necesita una fórmula única, visible y testeable:

```text
duración estimada =
  5 minutos de preparación general
  + 60 segundos por transición entre ejercicios
  + suma por ejercicio de:
      (45 segundos × series)
      + (descanso × (series - 1))
```

Una sesión con `n` ejercicios tiene `max(0, n - 1)` transiciones.

La estimación no afirma medir la duración real. Su propósito es evitar
propuestas evidentemente incompatibles con el perfil.

Una sesión es aceptable si no supera la duración solicitada en más de 10%.
Cuando excede el presupuesto, el ajuste ocurre en este orden:

1. quitar accesorios de menor prioridad;
2. reducir series accesorias hasta su mínimo;
3. reducir series secundarias hasta su mínimo;
4. alternar accesorios entre días equivalentes;
5. reducir patrones principales sólo hasta el mínimo semanal permitido.

En V2.3, quitar un accesorio opcional únicamente de la sesión que excede el
tiempo materializa también la alternancia entre días equivalentes: el accesorio
permanece en los días que sí entran en presupuesto. Luego se reducen, sin bajar
del mínimo por ejercicio, accesorios requeridos, compuestos secundarios y por
último compuestos primarios. Cada eliminación o reducción queda registrada.

No se eliminan silenciosamente patrones esenciales. Si aun así no cabe, se
devuelve `NotFeasible` con la duración mínima estimada y los patrones que no
pudieron cubrirse.

La fórmula deberá calibrarse con datos reales antes de considerarse exacta.

## 15. Invariantes de una propuesta válida

Una propuesta `Feasible` debe cumplir simultáneamente:

- contiene exactamente la cantidad de días solicitada;
- incluye todos los días preferidos del perfil;
- cada día y ejercicio tiene orden positivo y único;
- cada `ExerciseId` proviene del catálogo activo leído para esa ejecución;
- todo ejercicio cumple su equipamiento requerido;
- no hay ejercicios duplicados dentro de un día;
- existe cobertura de todos los patrones esenciales exigidos por el split;
- series, repeticiones, RIR, descansos y longitudes respetan contratos actuales;
- la duración estimada de cada día queda dentro de la tolerancia;
- no contiene una carga sugerida;
- no contiene texto generado por un LLM;
- es idéntica para el mismo input, catálogo y `PlannerPolicyVersion`.

Estas invariantes deben validarse nuevamente al confirmar, porque el perfil o
el catálogo pueden cambiar entre preview y creación.

## 16. Separación de responsabilidades

### 16.1 Motor determinístico

Decide:

- calendario y split;
- focos y cobertura por día;
- elegibilidad y ranking de ejercicios;
- volumen;
- series, repeticiones, RIR y descansos;
- orden de ejercicios;
- duración estimada;
- factibilidad, warnings y reason codes.

### 16.2 Coach determinístico

Puede explicar:

- por qué se eligió el split;
- cómo se usaron objetivo, experiencia, días, tiempo y equipamiento;
- por qué un ejercicio quedó incluido o excluido;
- qué limitación produjo un warning;
- qué se va a persistir si el usuario confirma.

La explicación deriva de la propuesta y sus reason codes.

### 16.3 LLM futuro

Puede:

- interpretar una pregunta sobre la propuesta;
- explicar los datos estructurados en lenguaje natural;
- clasificar una solicitud como aclaración o pedido de ajuste;
- solicitar una aclaración cuando una preferencia no pueda mapearse de forma
  segura.

No puede:

- elegir directamente un ejercicio;
- modificar series, RIR, descansos o split;
- producir IDs;
- declarar equipamiento disponible;
- crear o actualizar una rutina;
- convertir texto libre directamente en una mutación.

Un ajuste interpretado por LLM debe convertirse primero en restricciones
estructuradas, validarse y ejecutar nuevamente el planner.

### 16.4 Usuario

Debe confirmar explícitamente la propuesta final. Cambiar ejercicios,
prescripción o días en la revisión produce una nueva propuesta validada; no es
una autorización implícita para persistir.

## 17. Persistencia y confirmación

El flujo recomendado es:

1. **Generate proposal:** obtiene perfil y catálogo, ejecuta el planner y
   devuelve preview. No escribe en base de datos.
2. **Review:** el usuario revisa días, ejercicios y prescripción. Puede cancelar
   o pedir ajustes estructurados.
3. **Confirm:** el cliente envía la propuesta aceptada y la versión del perfil y
   de la política usadas para generarla.
4. **Revalidate:** el backend vuelve a validar ownership, perfil vigente,
   ejercicios activos, equipamiento, invariantes y contratos de Rutinas.
5. **Create:** sólo entonces se usa el caso de uso normal de creación de rutina
   en una única operación.

V2 inicial no persiste drafts ni conversaciones. Cada propuesta incluye un
`ProposalId` aleatorio y un hash SHA-256 de su representación canónica, que
incluye perfil, catálogo relevante, versión de política y contenido propuesto.
La confirmación no confía en datos descriptivos enviados por el cliente:
regenera y revalida la propuesta antes de comparar el hash.

La rutina creada guarda `PlannerProposalId` y `PlannerProposalHash`. Existe una
unicidad por usuario y `PlannerProposalId`:

- repetir la confirmación con el mismo ID y hash devuelve la misma rutina;
- reutilizar el ID con otro hash devuelve conflicto;
- confirmaciones concurrentes se resuelven por la restricción única, nunca
  creando dos rutinas.

Si el perfil o el catálogo cambió, la confirmación falla con un conflicto y
solicita regenerar; no sustituye ejercicios silenciosamente.

Al confirmar, todas las rutinas activas anteriores del usuario se desactivan y
la nueva queda activa, dentro de la misma transacción. Las rutinas y el historial
no se eliminan. Esta política aplica al flujo V2; los casos de uso actuales no
cambian hasta V2.5.

## 18. Decisiones cerradas y calibraciones futuras

V2.0 cierra como normativas:

1. metadata persistida y curada con código estable;
2. `DiaSemana?` persistido en `DiaRutina`;
3. volumen secundario de `0.5` por serie compuesta;
4. siete días como PPL A/B más día corto de Core/Calves, sin nueva exposición
   pesada;
5. propuesta stateless con ID y hash canónico;
6. confirmación idempotente por usuario y propuesta;
7. desactivación de rutinas anteriores al confirmar una nueva propuesta.

No quedan bloqueantes de modelo para comenzar V2.2. La clasificación curada de
los 37 ejercicios del seed fue auditada funcionalmente. `Face pull` se clasifica
como aislamiento accesorio para evitar que ocupe cupos compuestos o compute
volumen secundario en V2. Son calibraciones futuras, no cambios de contrato:

- validar los rangos de volumen por experiencia y objetivo con uso real;
- calibrar los 45 segundos por serie, transición y tolerancia del 10%;
- decidir qué ajustes manuales ofrecerá la revisión de propuesta;
- agregar dificultad o equivalencias sólo si una regla futura demuestra la
  necesidad.

### Decisiones explícitas de V2 inicial

- sexo, edad y peso no alteran la prescripción;
- no se prescriben cargas iniciales;
- no se infiere equipamiento por entorno;
- no se usan nombres de ejercicios para clasificarlos;
- no se agregan notas generativas;
- no se optimiza con historial de rendimiento en V2 inicial;
- el LLM no forma parte del pipeline de planificación.

## 19. Roadmap técnico recomendado

### V2.0 — Contratos y política (cerrado)

- contratos y flujo de propuesta definidos;
- metadata persistida y seed curado;
- día semanal, idempotencia y activación resueltos;
- política normativa documentada.

### V2.1 — Estructura semanal (cerrado)

- calendario, días inferidos y matriz de splits implementados en Application;
- estructura pura en memoria para 1 a 7 días;
- días preferidos preservados y días derivados marcados explícitamente;
- sin selección de ejercicios ni persistencia.

### V2.2 — Elegibilidad y selección (cerrado)

- contrato puro de catálogo y equipamientos implementado en Application;
- filtrado por metadata completa y compatibilidad `AND` de equipamiento;
- matriz de slots, ranking y variación A/B/C determinísticos;
- factibilidad y diagnósticos estructurados sin persistencia.

### V2.3 — Prescripción y duración (cerrado)

- volumen distribuido por experiencia, objetivo y exposiciones reales;
- repeticiones, RIR y descansos compatibles con `EjercicioRutina`;
- duración estimada con tolerancia del 10% y reducción por prioridad;
- factibilidad y diagnósticos estructurados, sin persistencia.

### V2.4 — Propuesta completa

- componer estructura, selección y prescripción;
- incluir snapshot/versiones y warnings;
- generar explicación determinística inicial;
- ejecutar tests de escenarios representativos de extremo a extremo en
  Application.

### V2.5 — Casos de uso y API

- exponer generación autenticada sin persistencia;
- exponer confirmación explícita e idempotente;
- revalidar perfil, catálogo, ownership y propuesta;
- crear mediante las reglas existentes de Rutinas;
- resolver previamente la persistencia tipada de días semanales.

### V2.6 — Coach explanation/conversation

- construir contexto mínimo a partir de la propuesta;
- explicar reason codes y tradeoffs sin cambiar decisiones;
- permitir preguntas y ajustes convertidos a restricciones estructuradas;
- mantener conversación stateless inicialmente;
- no habilitar mutaciones sin volver a preview y confirmación.

## 20. Criterio de inicio de implementación

V2.2 puede comenzar sobre estos contratos y la metadata auditada, sin un nuevo
modelo. V2.5 debe implementar la transacción de activación e idempotencia ya
definida; no debe inventar otra política.
