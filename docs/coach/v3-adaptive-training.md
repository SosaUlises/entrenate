# Entrenate Coach V3 — Adaptive Training

## 1. Estado y propósito

Estado: AT-0 normativo cerrado; listo para implementar AT-1.

Adaptive Training adapta una sesión planificada a restricciones puntuales del
día sin modificar la rutina base y sin delegar decisiones de entrenamiento a
un modelo de lenguaje.

```text
rutina activa + día de rutina + contexto del día
        ↓
motor determinístico de adaptación
        ↓
propuesta estructurada no persistida
        ↓
revisión explícita
        ↓
confirmación e inicio con snapshot adaptado
```

Esta especificación complementa `docs/coach.md` y reutiliza políticas de
Routine Planner cuando su semántica coincide. No redefine la identidad global
de Coach ni incluye molestias, lesiones, diagnóstico, rehabilitación o
nutrición.

## 2. Alcance inicial

V3 debe poder representar de forma estructurada:

- menos tiempo disponible para la sesión actual;
- equipamiento temporalmente no disponible;
- uno o más ejercicios que el usuario decide no realizar hoy;
- preferencia por una sesión más liviana;
- simplificación de la sesión conservando su intención principal.

V3 no debe:

- modificar la rutina base;
- guardar como preferencia permanente una restricción puntual;
- prescribir cargas concretas;
- inferir fatiga, readiness, dolor o una condición médica;
- crear superseries ni reducir descansos en V3 inicial;
- seleccionar sustitutos mediante nombres, texto libre o un LLM;
- iniciar una sesión sin revisión y confirmación del usuario.

## 3. Estado actual relevante

### 3.1 Rutina y día

El modelo actual persiste:

- `Rutina`: ownership, nombre, descripción, estado activo y metadata opcional
  de Routine Planner;
- `DiaRutina`: nombre, descripción, orden y `DiaSemana?`;
- `EjercicioRutina`: ejercicio, orden, series, rango de repeticiones, rango de
  RIR, descanso, notas e intención nullable `IsRequiredForAdaptation`.

No persiste actualmente:

- foco tipado del día;
- rol contextual del ejercicio (`PrimaryCompound`, `SecondaryCompound`,
  `Accessory`);
- intención explícita de la sesión.

Routine Planner V2.5 copia la obligatoriedad estructurada de cada slot a
`IsRequiredForAdaptation`. En rutinas legacy o manuales el valor permanece
`null`; no debe compensarse interpretando el nombre del día o del ejercicio.
La política normativa y su fallback se definen en la sección 6.

### 3.2 Inicio de sesión

El flujo actual recibe únicamente `DiaRutinaId`. El backend:

1. valida ownership del día;
2. impide crear una segunda sesión activa;
3. construye `SesionEntrenamiento` desde los `EjercicioRutina` vigentes;
4. copia la prescripción a `EjercicioSesion`;
5. persiste e inicia la sesión en una misma operación.

`EjercicioSesion` ya es el snapshot normativo de lo prescripto en esa
exposición: guarda ejercicio, orden, series objetivo, repeticiones, RIR,
descanso y notas. Progress Coach lee esos campos, no la rutina vigente, para
analizar una sesión completada.

V3 debe intervenir antes de crear la sesión real. Confirmar una adaptación
debe crear `SesionEntrenamiento` y sus `EjercicioSesion` directamente desde el
snapshot adaptado validado. El constructor actual, que sólo acepta
`IEnumerable<EjercicioRutina>`, no alcanza para ese flujo y deberá ampliarse en
la etapa de implementación sin debilitar sus invariantes.

### 3.3 Catálogo, perfil y equipamiento

El catálogo real aporta:

- `ExerciseId` y `Codigo` estable;
- actividad;
- grupo muscular principal y secundarios;
- `PatronMovimiento?`;
- `TipoEjercicio?`;
- equipamientos requeridos con semántica AND.

Sólo los ejercicios activos con metadata de planificación completa son
elegibles como sustitutos automáticos. Un ejercicio sin equipamiento requerido
se considera de peso corporal.

`PerfilEntrenamiento` aporta objetivo, experiencia, duración habitual,
entorno y equipamiento declarado. El entorno no concede equipamiento implícito.
La duración habitual sirve como contexto; el límite del día proviene del input
explícito de la adaptación.

## 4. Fuente de la adaptación

Una propuesta V3 adapta exactamente:

- la rutina activa del usuario autenticado;
- un `DiaRutina` concreto de esa rutina;
- la colección ordenada y vigente de `EjercicioRutina`;
- la metadata vigente de los ejercicios y equipamientos referenciados;
- el perfil de entrenamiento vigente;
- un contexto del día estructurado.

La rutina debe seguir activa al generar y al confirmar. El día debe seguir
perteneciendo a esa rutina. La propuesta no representa un draft de rutina ni
un día alternativo reutilizable.

La generación puede devolver una propuesta idéntica a la sesión original si
el contexto no exige cambios. Ese resultado es válido y debe indicar que no
fueron necesarios ajustes.

## 5. Inputs del contexto del día

Contrato conceptual mínimo:

```text
SessionAdaptationRequest
  RoutineDayId
  AvailableMinutes?
  TemporarilyUnavailableEquipmentIds[]
  ExcludedExerciseIds[]
  EffortPreference

SessionEffortPreference
  Normal
  Lighter
```

Reglas:

- al menos una restricción debe diferir del plan original para que la UI
  ofrezca la adaptación, aunque el motor soporte una propuesta sin cambios;
- `AvailableMinutes`, si se informa, es un entero entre 15 y 240;
- equipamientos y ejercicios se envían como IDs reales, sin texto libre;
- los ejercicios excluidos deben pertenecer al día fuente;
- los equipamientos no disponibles deben existir y estar activos;
- las colecciones se normalizan sin duplicados y con orden estable;
- `Lighter` expresa una preferencia de esfuerzo, no fatiga ni estado médico;
- no existe readiness score en V3 inicial.

El equipamiento no disponible es un delta negativo temporal. No obliga al
usuario a volver a declarar todo el inventario. Un ejercicio original se
considera afectado sólo si requiere un equipamiento marcado como no disponible.
Los sustitutos, en cambio, sólo pueden requerir equipamiento incluido en el
perfil vigente y no marcado como no disponible.

## 6. Proyección y clasificación de la sesión original

Antes de adaptar, Application debe construir una proyección inmutable:

```text
OriginalSessionPlan
  RoutineId
  RoutineUpdatedAtUtc
  RoutineDayId
  DayOrder
  Exercises[]
  EstimatedDurationSeconds

OriginalSessionExercise
  RoutineExerciseId
  ExerciseId
  ExerciseCode
  Name
  Order
  MovementPattern
  ExerciseType
  PrimaryMuscleGroup
  SecondaryMuscleGroups[]
  RequiredEquipmentIds[]
  SlotRole
  IsRequired
  Prescription
```

`SlotRole` e `IsRequired` proceden de esta política normativa, nunca de un LLM,
del nombre del día ni del nombre del ejercicio:

1. todo ejercicio con `TipoEjercicio.Compuesto` es requerido;
2. el primer compuesto de cada patrón, por `Orden` y luego `Codigo` ordinal,
   usa rol `PrimaryCompound`; los demás usan `SecondaryCompound`;
3. todo ejercicio con `TipoEjercicio.Aislamiento` usa rol `Accessory`;
4. un aislamiento con intención persistida explícita usa ese valor para
   `IsRequired`;
5. sin intención persistida, un aislamiento es requerido si eliminarlo deja
   al plan original sin ningún otro ejercicio del mismo `PatronMovimiento` o
   sin ningún otro ejercicio del mismo grupo muscular principal;
6. los demás aislamientos son opcionales;
7. un ejercicio con `TipoEjercicio`, patrón o grupo principal incompleto se
   clasifica como requerido y `UnclassifiedRequired`; puede mantenerse, pero
   no sustituirse automáticamente.

### 6.1 Intención mínima persistida

La regla de unicidad es un fallback conservador, no una reconstrucción exacta
de la intención original. En `Upper`, `Push`, `Pull`, `Lower` o `Legs` puede
promover a requerido un aislamiento que V2 había generado como opcional. El
caso inverso también existe: el mismo patrón de gemelos es opcional en `Lower`
y requerido en `CoreCalves`. `FullBody` requiere core aunque conviva con
compuestos. Las rutinas manuales tampoco contienen una señal que permita
distinguir esos casos.

La metadata persistida mínima es `IsRequiredForAdaptation?` en
`EjercicioRutina`:

- Routine Planner debe copiar el `IsRequired` de su slot al crear la rutina;
- `true` y `false` son autoritativos para aislamientos;
- los compuestos siguen siendo requeridos aunque el dato sea inconsistente;
- `null` activa el fallback conservador anterior para rutinas existentes o
  creadas manualmente;
- no se necesita persistir un ranking, foco del día ni nombre semántico para
  implementar V3.

Esta metadata preserva la diferencia real entre `Upper`, `Lower`, `Push`,
`Pull`, `Legs`, `FullBody` y `CoreCalves` sin acoplar Adaptive Training a los
nombres visibles ni exigir que toda rutina provenga de V2. Las rutinas legacy
y manuales continúan operando con el fallback conservador y menor capacidad de
reducción cuando la intención sea ambigua.

## 7. Estimación de duración

V3 reutiliza la fórmula de V2:

```text
duración estimada =
  5 minutos de preparación general
  + 60 segundos por transición entre ejercicios
  + suma por ejercicio de:
      (45 segundos × series)
      + (descanso × (series - 1))
```

Una sesión con `n` ejercicios tiene `max(0, n - 1)` transiciones. La fórmula es
una estimación de compatibilidad, no una promesa de duración real.

V3 inicial nunca reduce descansos. El descanso forma parte de la prescripción
y comprimirlo cambiaría la intención del ejercicio sin evidencia suficiente.
No hay piso alternativo ni excepción por falta de tiempo: si las demás
transformaciones no alcanzan, el resultado es `NotFeasible`. Tampoco crea
superseries ni aumenta intensidad para compensar tiempo.

## 8. Adaptación por tiempo

La adaptación se ejecuta sólo cuando `AvailableMinutes` es menor que la
duración original estimada. El presupuesto no tiene tolerancia positiva: una
propuesta adaptada debe quedar dentro del tiempo declarado.

Pisos normativos:

```text
policyMinimum(compuesto requerido) = 2
policyMinimum(aislamiento requerido) = 1
policyMinimum(accesorio opcional mientras permanezca) = 1
effectiveMinimum = min(originalSets, policyMinimum)
```

Nunca se aumentan las series originales para alcanzar un piso general. Un
accesorio opcional puede eliminarse por completo; un ejercicio requerido no.

Orden normativo de ajuste, recalculando duración después de cada operación:

1. eliminar accesorios opcionales de menor prioridad, en orden inverso de
   `Orden` y `Codigo`, mientras quede más de un accesorio opcional;
2. reducir el accesorio opcional prioritario que permanezca, de una serie por
   iteración, hasta su `effectiveMinimum`;
3. si todavía no entra, eliminar ese último accesorio siempre que la sesión
   conserve al menos un ejercicio;
4. reducir aislamientos requeridos, de una serie por iteración, hasta su
   `effectiveMinimum`;
5. reducir compuestos requeridos, de una serie por iteración y en orden inverso
   de `Orden` y `Codigo`, hasta su `effectiveMinimum`;
6. si el mínimo requerido sigue excediendo el presupuesto, devolver
   `NotFeasible`.

El motor conserva todos los ejercicios requeridos y su cobertura. No reduce
repeticiones, RIR o descanso por una restricción exclusivamente temporal.

Todo ejercicio eliminado o toda reducción de series debe generar un cambio y
un diagnóstico estructurado. El frontend no reconstruye el orden de decisión.

## 9. Sustitución por equipamiento

Para cada ejercicio afectado por equipamiento temporalmente no disponible, el
motor busca candidatos en el catálogo real.

### 9.1 Elegibilidad dura

Un candidato es válido sólo si:

- está activo;
- tiene `Codigo`, patrón y tipo completos;
- tiene el mismo `PatronMovimiento` que el ejercicio original;
- tiene el mismo `TipoEjercicio`;
- tiene el mismo grupo muscular principal;
- todos sus equipamientos requeridos están activos, declarados en el perfil y
  disponibles hoy;
- no fue excluido por el usuario;
- no está ya presente en la sesión adaptada ni fue seleccionado como sustituto
  de otro slot.

Los mismos requisitos duros se aplican a slots requeridos y opcionales. V3
inicial no relaja patrón, tipo ni grupo principal. Una sustitución aproximada
puede alterar la intención y debe tratarse como no disponible, no como un
candidato de menor score. Un ejercicio original con metadata incompleta no
tiene sustituto automático válido.

### 9.2 Ranking y desempates

Entre candidatos elegibles, ordenar por:

1. mayor cantidad de grupos secundarios compartidos;
2. menor diferencia absoluta en cantidad de equipamientos requeridos;
3. menor uso previo del candidato dentro de la rutina activa;
4. `Codigo` ordinal;
5. `ExerciseId` como último desempate estable.

No se infiere equivalencia por nombre, descripción, imagen o Workout Guide.

### 9.3 Ausencia de sustituto

- si el slot es requerido, la propuesta es `NotFeasible`;
- si es un accesorio opcional, se elimina el ejercicio y la propuesta queda
  como mínimo `FeasibleWithWarnings`;
- si la eliminación rompe cobertura obligatoria, prevalece `NotFeasible`.

La eliminación opcional conserva un cambio estructurado con la causa original.
No se reutiliza un ejercicio ya presente para llenar el slot vacío.

## 10. Ejercicio omitido por el usuario

`ExcludedExerciseIds` expresa una decisión puntual del usuario. No equivale a
equipamiento ausente y no se persiste como rechazo o preferencia permanente.

El motor aplica la misma elegibilidad y ranking de sustitución, pero registra
un motivo diferente:

- `EquipmentUnavailable` para una restricción de equipamiento;
- `UserExcludedExercise` para una omisión explícita.

Si un ejercicio está afectado por ambos motivos, ambos se conservan en el
resultado canónico. La factibilidad sigue dependiendo de la obligatoriedad del
slot y de la cobertura resultante.

## 11. Sesión más liviana

En V3 inicial, `Lighter` significa exactamente:

1. aumentar en uno `MinRir` y `MaxRir` de cada ejercicio, saturando cada extremo
   en 5 y manteniendo `MinRir <= MaxRir`;
2. mantener ejercicios y rango de repeticiones;
3. mantener descansos;
4. mantener las series de todos los ejercicios requeridos;
5. recorrer accesorios opcionales en orden inverso de `Orden` y `Codigo` y
   reducir exactamente una serie de cada uno cuando esté por encima de su
   `effectiveMinimum`;
6. no eliminar ejercicios por `Lighter` solamente;
7. no prescribir peso ni porcentajes de carga.

Si el RIR ya está en 5, ese extremo se conserva. Si ningún accesorio opcional
puede reducirse, no se reduce volumen requerido para simular una sesión más
liviana. Si toda la transformación resulta neutra, la propuesta sigue siendo
válida con un diagnóstico informativo.

La preferencia `Lighter` se aplica antes de la reducción por tiempo. La etapa
de tiempo parte del resultado liviano y puede seguir reduciendo o eliminar
accesorios opcionales conforme a la sección 8. `Lighter` no autoriza a omitir
cobertura requerida ni supone fatiga, lesión o recuperación.

## 12. Orden del motor

El pipeline determinístico es:

1. validar usuario, rutina activa y día;
2. rechazar generación si ya existe una sesión activa;
3. cargar perfil, día, prescripción y catálogo necesario;
4. construir y clasificar el snapshot original;
5. normalizar el contexto del día;
6. sustituir o eliminar ejercicios afectados por equipamiento/exclusión;
7. validar cobertura obligatoria;
8. aplicar `Lighter` si corresponde;
9. aplicar reducción por tiempo;
10. reordenar de forma estable sin huecos;
11. estimar duración original y adaptada;
12. validar contratos de sesión y catálogo;
13. calcular factibilidad, cambios y diagnósticos;
14. canonicalizar y devolver la propuesta.

Aplicar tiempo al final garantiza que las sustituciones y la preferencia de
esfuerzo también entren en el presupuesto real del día.

## 13. Resultado estructurado

Contrato conceptual:

```text
SessionAdaptationProposal
  AdaptationProposalId
  ProposalHash
  PolicyVersion
  Feasibility
  Source
  Context
  OriginalSession
  AdaptedSession
  Changes[]
  Diagnostics[]

SessionAdaptationSource
  RoutineId
  RoutineDayId
  RoutineUpdatedAtUtc

SessionAdaptationContext
  AvailableMinutes?
  TemporarilyUnavailableEquipmentIds[]
  ExcludedExerciseIds[]
  EffortPreference

SessionPlanSnapshot
  EstimatedDurationSeconds
  Exercises[]

SessionExerciseSnapshot
  SourceRoutineExerciseId?
  ExerciseId
  ExerciseCode
  Name
  Order
  SlotRole
  IsRequired
  Prescription

ExercisePrescriptionSnapshot
  Sets
  MinReps
  MaxReps
  MinRir
  MaxRir
  RestSeconds
  Notes?

SessionAdaptationChange
  ChangeType
  Reason
  OriginalExercise?
  AdaptedExercise?

SessionAdaptationDiagnostic
  Code
  Severity
  ExerciseCode?
  Details
```

`ChangeType` diferencia al menos:

- `Maintained`;
- `Replaced`;
- `Removed`;
- `PrescriptionAdjusted`.

Los mantenidos pueden omitirse de `Changes` si `OriginalSession` y
`AdaptedSession` ya permiten compararlos, pero el backend debe exponer una
forma inequívoca de contar y mostrar ejercicios mantenidos.

Los códigos son internos. El contrato de presentación debe incluir mensajes
determinísticos o un mapping de Application para que frontend muestre
`Antes → Después` sin exponer enums ni interpretar reglas.

## 14. Factibilidad y diagnósticos

Estados propios de V3:

- `Feasible`: todos los cambios solicitados se aplicaron sin pérdida relevante;
- `FeasibleWithWarnings`: la sesión es confirmable, pero se eliminó trabajo
  opcional, una preferencia no pudo aplicarse por completo o existe otra
  limitación explícita;
- `NotFeasible`: no puede iniciarse de forma segura bajo ese contexto.

Ejemplos de `NotFeasible`:

- el mínimo obligatorio supera el tiempo disponible;
- un ejercicio obligatorio afectado no tiene sustituto válido;
- las restricciones eliminan una cobertura obligatoria;
- el día o la rutina dejaron de ser válidos;
- el snapshot adaptado viola contratos de prescripción.

Diagnósticos internos mínimos sugeridos:

- `NoChangesRequired`;
- `AccessoryRemovedForTime`;
- `ExerciseSetsReducedForTime`;
- `ExerciseReplacedForEquipment`;
- `ExerciseReplacedForUserExclusion`;
- `OptionalExerciseRemovedWithoutReplacement`;
- `RequiredExerciseHasNoReplacement`;
- `MinimumSessionExceedsAvailableTime`;
- `EssentialCoverageLost`;
- `LighterEffortApplied`;
- `RirIncreaseCapped`.

Los diagnostics no son copy de usuario ni deben enviarse a un LLM sin una
proyección segura.

## 15. Determinismo, identidad e idempotencia

Mismo snapshot fuente, mismo catálogo relevante, mismo perfil, mismo contexto
normalizado y misma versión de política deben producir exactamente la misma
propuesta, representación canónica, hash e identidad.

La representación canónica debe incluir:

- versión de formato y `PolicyVersion`;
- identidad y contenido prescriptivo de la rutina/día fuente;
- perfil relevante: objetivo, experiencia y equipamiento;
- metadata activa de los ejercicios/equipamientos que participan;
- contexto normalizado del día;
- clasificación de slots y fuente de la intención requerida
  (`Persisted` o `ConservativeFallback`);
- snapshot original y adaptado;
- factibilidad, cambios y diagnósticos ordenados.

No debe incluir nombres visibles, timestamps incidentales, `UserId`,
`AdaptationProposalId` ni el propio hash. Los ejercicios se identifican
canónicamente por `Codigo`; los GUID reales permanecen en la propuesta
operativa.

Usar SHA-256 sobre UTF-8 y derivar `AdaptationProposalId` de los primeros 128
bits, siguiendo conceptualmente V2. Las colecciones se ordenan por claves
estables; los valores opcionales y enums tienen representación explícita y no
dependiente de cultura. El formato canónico y `PolicyVersion` tienen versiones
propias de V3; no se reutiliza el canonicalizer de Routine Planner mediante una
abstracción genérica prematura.

La confirmación debe regenerar la propuesta con la fuente vigente y el mismo
contexto estructurado enviado nuevamente por el cliente. Es stale si cambió la
rutina/día, la prescripción, la intención de adaptación, el perfil relevante,
la metadata de catálogo que modifica el resultado o `PolicyVersion`, o si ID o
hash regenerados no coinciden. Un stale exige generar otra propuesta; nunca se
confirma el snapshot recibido desde frontend.

Una restricción única por `(UsuarioId, AdaptationProposalId)` en la auditoría
confirmada permite que doble submit o reintento devuelvan la misma sesión. La
confirmación consulta primero esa identidad: si ya existe con el mismo hash,
devuelve `TrainingSessionId` y `CreatedNow = false`, aun cuando esa sesión esté
activa; con otro hash devuelve conflicto. Sólo después comprueba otra sesión
activa y regenera. La creación y la auditoría se guardan en una transacción y
la restricción única resuelve carreras concurrentes.

## 16. Confirmación, persistencia e inicio

Flujo recomendado:

1. `Generate`: valida y devuelve propuesta; no persiste.
2. `Review`: frontend muestra original, adaptación, cambios y warnings.
3. `Confirm`: cliente envía `AdaptationProposalId`, `ProposalHash` y el mismo
   contexto estructurado.
4. `Regenerate`: backend reconstruye la propuesta desde datos vigentes.
5. `Revalidate`: compara identidad/hash, ownership, sesión activa, catálogo e
   invariantes.
6. `Create session`: crea la sesión y el snapshot adaptado en una transacción.
7. `Return`: devuelve `TrainingSessionId` y `CreatedNow`.

`NotFeasible` nunca es confirmable. Generación y confirmación deben comprobar
que no exista otra sesión activa, salvo el lookup idempotente de una
confirmación ya completada. Una sesión distinta que comenzó entre preview y
confirmación produce conflicto funcional; no se adapta, cancela ni reemplaza.
V3 inicial no modifica silenciosamente una sesión ya iniciada.

La rutina base y sus `EjercicioRutina` no se modifican. `DiaRutinaId` permanece
como referencia de origen en la sesión. La ejecución real usa exclusivamente
los `EjercicioSesion` creados desde `AdaptedSession`.

### 16.1 Auditoría persistida

Para preservar explicación y trazabilidad sin contaminar la rutina, la sesión
adaptada necesita metadata opcional propia. Modelo conceptual recomendado:

```text
SesionAdaptacion (1:1 con SesionEntrenamiento)
  SesionEntrenamientoId
  AdaptationProposalId
  ProposalHash
  PolicyVersion
  Feasibility
  AvailableMinutes?
  EffortPreference
  UnavailableEquipmentIds[]
  ExcludedExerciseIds[]
  OriginalEstimatedDurationSeconds
  AdaptedEstimatedDurationSeconds
  ConfirmedAtUtc
  Changes[]

CambioAdaptacionSesion
  Order
  ChangeType
  Reason
  SlotRole
  IsRequired
  SourceRoutineExerciseId?
  OriginalExerciseId?
  OriginalExerciseCode?
  AdaptedExerciseId?
  AdaptedExerciseCode?
  OriginalPrescription?
  AdaptedPrescription?
```

`EjercicioSesion` es suficiente para conservar la prescripción adaptada
confirmada, pero no conserva qué cambió ni por qué. Por eso la auditoría 1:1 y
su colección de deltas son obligatorias para una sesión adaptada.

No se duplica el snapshot adaptado completo: se reconstruye desde
`EjercicioSesion`. Tampoco se persisten entradas `Maintained`, porque su estado
original y adaptado es el mismo snapshot. Sí se persiste un delta por cada:

- sustitución, con identidad original y adaptada;
- eliminación, con snapshot original completo porque no existe
  `EjercicioSesion` resultante;
- ajuste de prescripción, con prescripción original y adaptada;
- cambio que combine sustitución y prescripción, como una única entrada capaz
  de conservar ambos lados.

Así, el plan original histórico se reconstruye de forma segura combinando los
`EjercicioSesion` sin cambio con los lados originales de los deltas. No se
consulta la rutina vigente para reconstruir historia. Identidades, códigos,
órdenes, roles, prescripciones y motivos son estructurados e inmutables.

Las colecciones del contexto también son snapshots ordenados e inmutables. Su
mapping físico puede ser relacional, owned types o JSON versionado en
Infrastructure; esa elección no altera el contrato de Domain/Application y no
queda como decisión normativa del motor.

## 17. Relación con Progress Coach

Progress Coach debe continuar leyendo exposiciones completadas desde
`EjercicioSesion`. Para una sesión adaptada:

- `ExerciseId` es el ejercicio realmente ejecutable ese día, incluido el
  sustituto si lo hubo;
- series, repeticiones, RIR y descanso son los objetivos adaptados reales;
- una adaptación puntual no altera `EjercicioRutina` ni exposiciones futuras;
- una edición posterior de la rutina no cambia la sesión histórica ni su
  auditoría;
- el análisis de cambio de prescripción compara snapshots reales y puede
  detectar que la prescripción cambió;
- la diferencia contra la rutina base no es incumplimiento: para esa sesión la
  prescripción válida es la adaptación confirmada;
- Progress Coach no decide ni recalcula adaptaciones.

No es necesario que Progress Coach lea `SesionAdaptacion` para mantener su
corrección actual. Podrá usar esa metadata en una versión posterior para
explicar por qué cambió la prescripción, siempre sin reinterpretar el resultado
del motor V3.

## 18. Coach y LLM

Una capa conversacional posterior podrá:

- explicar por qué se eliminó un accesorio;
- explicar por qué se eligió un sustituto;
- explicar cambios de series o RIR;
- explicar duración, factibilidad y warnings;
- responder preguntas sobre una propuesta concreta.

El contexto del LLM debe ser una proyección segura de la propuesta y sus
explicaciones determinísticas. No debe exponer IDs, hashes, enums, diagnostics
ni terminología interna.

El LLM no puede:

- seleccionar o rankear sustitutos;
- agregar, eliminar o modificar ejercicios;
- cambiar series, repeticiones, RIR o descansos;
- confirmar o iniciar la sesión;
- modificar la rutina base;
- inventar datos, ejercicios o motivos;
- tomar decisiones de seguridad.

La conversación no forma parte del alcance de implementación inicial.

## 19. Contratos API conceptuales

Los nombres finales pueden ajustarse a las convenciones del API, pero la
semántica recomendada es:

```text
POST /api/coach/adaptive-training/proposals
Body: SessionAdaptationRequest
Response: SessionAdaptationProposal

POST /api/coach/adaptive-training/proposals/{proposalId}/confirm
Body:
  proposalHash
  context
Response:
  trainingSessionId
  proposalId
  createdNow
```

Una propuesta `NotFeasible` es HTTP success y conserva estructura suficiente
para revisión. Stale, sesión activa o reutilización inconsistente de identidad
son conflictos funcionales. Errores de validación de IDs/contexto no se
convierten en `NotFeasible`.

El endpoint actual de inicio de sesión permanece intacto para sesiones no
adaptadas.

## 20. Invariantes

Una propuesta confirmable debe cumplir:

- pertenece al usuario autenticado, rutina activa y día indicados;
- contiene al menos un ejercicio;
- conserva todos los slots obligatorios y la cobertura esencial original;
- cada ejercicio proviene del catálogo vigente;
- cada sustituto cumple metadata y equipamiento;
- no hay ejercicios duplicados;
- órdenes positivos, únicos y contiguos;
- prescripción dentro de los límites actuales de `EjercicioSesion`;
- ninguna serie queda debajo de su `effectiveMinimum`;
- los descansos y rangos de repeticiones originales permanecen intactos;
- duración adaptada dentro del presupuesto, si fue informado;
- no contiene cargas ni decisiones generativas;
- no modifica la rutina base;
- es reproducible desde su input y versión de política;
- no coexiste con otra sesión activa del usuario.

## 21. Decisiones normativas cerradas

AT-0 cierra las decisiones que condicionan el motor:

1. **Prioridad.** Todo compuesto es requerido. Los aislamientos respetan la
   intención explícita nullable y, cuando falta, usan el fallback conservador
   de cobertura de patrón o grupo principal. Metadata incompleta implica
   requerido no sustituible.
2. **Equivalencia.** Patrón, tipo y grupo principal exactos son requisitos duros
   para todo sustituto. Equipamiento, exclusiones y no duplicación también son
   condiciones de elegibilidad, no factores de score.
3. **Pisos.** Compuesto requerido: dos series. Aislamiento requerido: una.
   Accesorio opcional: una mientras permanezca y cero sólo al eliminarlo. En
   todos los casos se aplica `min(originalSets, policyMinimum)`.
4. **`Lighter`.** RIR `+1` con cap 5, mismos ejercicios, repeticiones y
   descansos, series requeridas intactas y una serie menos por accesorio
   opcional reducible. No es un deload ni prescribe carga.
5. **Descansos.** No se modifican en V3. La falta de tiempo puede terminar en
   `NotFeasible`; no habilita recortes de descanso.
6. **Auditoría.** `EjercicioSesion` conserva el adaptado real.
   `SesionAdaptacion` conserva identidad, política, contexto y duración; sus
   deltas conservan sustituciones, eliminaciones y ajustes. No se duplica el
   snapshot completo ni se reconstruye historia desde la rutina vigente.
7. **Tiempo.** El input válido inicial es 15 a 240 minutos. Un valor menor es
   error de validación, no una propuesta `NotFeasible`.
8. **Fuente.** Sólo puede adaptarse un día de la rutina activa. Una rutina
   inactiva o una sesión ya iniciada no son fuentes válidas.
9. **Identidad.** La propuesta usa canonicalización V3, SHA-256,
   `AdaptationProposalId` reproducible, `PolicyVersion`, regeneración en confirm
   e idempotencia persistida por usuario e identidad de propuesta.

No quedan decisiones normativas pendientes para implementar AT-1. La auditoría
de cobertura del catálogo y la elección del mapping físico de persistencia son
tareas técnicas posteriores; no cambian estas reglas.

## 22. Roadmap técnico recomendado

`docs/coach.md` reserva V3.1 para Discomfort & Restrictions. Para evitar
confusión, los bloques técnicos de Adaptive Training usan prefijo `AT` y no
versiones decimales de producto.

### AT-0 — Contratos y políticas

- **Estado: cerrado por esta especificación.**
- reglas de prioridad, sustitución, pisos, `Lighter` y descansos definidas;
- contrato conceptual, factibilidad, auditoría e identidad definidos;
- sin API ni implementación.

### AT-1 — Adaptación por tiempo

- **Estado: implementado.**
- contratos puros de Application para plan, prescripción y contexto;
- metadata nullable mínima de intención requerida y fallback legacy;
- proyección del día y clasificación determinística de slots;
- reutilización de estimación V2 sin acoplar motores;
- pisos efectivos, reducción determinística y diagnostics;
- tests unitarios de `Upper`, `Lower`, `Push`, `Pull`, `Legs`, `FullBody`,
  `CoreCalves`, rutina manual, metadata incompleta, límites y determinismo;
- sin sustitución, API, persistencia ni frontend todavía.

### AT-2 — Sustitución contextual

- equipamiento no disponible y ejercicio excluido;
- elegibilidad, ranking y desempates;
- auditoría del catálogo real;
- tests de ausencia de sustituto y cobertura.

### AT-3 — Preferencia más liviana

- transformación de RIR y accesorios;
- composición con tiempo y sustituciones;
- tests de topes e idempotencia del motor.

### AT-4 — Propuesta completa

- pipeline combinado;
- snapshots antes/después;
- cambios, factibilidad y explicación determinística;
- canonicalizer, hash y `AdaptationProposalId`.

### AT-5 — API, confirmación e inicio

- generación autenticada stateless;
- regeneración y stale;
- persistencia transaccional de sesión y auditoría;
- idempotencia y carrera de doble submit;
- convivencia con recovery de sesión activa.

### AT-6 — Frontend y E2E

- captura del contexto del día;
- preview `Antes → Después`;
- warnings, `NotFeasible`, stale y confirmación;
- inicio de Training desde `trainingSessionId` real;
- pruebas mobile, accesibilidad y recovery.

### AT-7 — Explicación y conversación

- contexto seguro;
- explicación de decisiones existentes;
- conversación temporal sin mutaciones;
- validación de que el LLM no altera la propuesta.

Discomfort & Restrictions continúa siendo V3.1 del roadmap de producto y no se
incluye en ninguno de estos bloques.

## 23. Criterio de inicio de implementación

AT-1 puede comenzar: las decisiones normativas requeridas están cerradas. Debe
implementarse primero como motor puro y testeable de proyección, clasificación,
duración y adaptación por tiempo, sin API, persistencia, sustituciones ni LLM.

Los tests de AT-1 deben demostrar el fallback conservador para rutinas legacy y
manuales, el uso autoritativo de la intención nullable cuando exista, los pisos
efectivos, la estabilidad del orden y `NotFeasible` cuando el mínimo requerido
no entra en el presupuesto.
