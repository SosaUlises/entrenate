# Live Coach V1 — Checklist E2E Manual Final

**Estado: Live Coach V1 — CLOSED.** El usuario reportó que completó el E2E
manual de V1.0–V1.5 y verificó las correcciones detectadas durante la prueba.
Los casos de abajo quedan como guía de regresión y describen el comportamiento
esperado. El reporte de cierre manual es global.

Correcciones verificadas según el reporte: sincronización de migraciones EF Core,
persistencia de la auditoría de adaptaciones aplicadas, visibilidad de ejercicios
omitidos en Guided, invalidación de sugerencias obsoletas y análisis post-sesión
con desvíos agrupados y conteos separados de ejercicios planificados, completados
y omitidos.

---

### A. Abrir Coach + Voz / Texto / TTS

#### A1. Apertura del sheet y estado base
- **Acción / Frase sugerida**: Tocar el botón Live Coach en el encabezado de Training durante una sesión activa.
- **Resultado esperado**: Se abre el modal sheet con el rostro animado en postura idle sobria, sin deformaciones ni halos, mostrando el estado listo para escuchar.
- **Qué valida**: Ciclo de vida del sheet modal, montaje del componente sin estados stale y jerarquía visual base.

#### A2. Consulta por voz con síntesis TTS
- **Acción / Frase sugerida**: Presionar el botón de micrófono y decir con voz activada: *"¿Qué ejercicio me toca?"*.
- **Resultado esperado**: Transcribe el audio con Groq Whisper, muestra la respuesta en texto e inicia la reproducción de voz sincronizada con la animación de habla del rostro.
- **Qué valida**: Captura de micrófono/MediaRecorder, STT autenticado, TTS de ElevenLabs coupled por cache efímero y playback visual.

#### A3. Toggle de voz y fallback a texto
- **Acción / Frase sugerida**: Desactivar el botón de voz en el encabezado, abrir el modo "Escribir" y enviar por texto: *"¿Cómo vengo?"*.
- **Resultado esperado**: Responde solo con texto en pantalla, sin reproducir audio ni solicitar permisos de micrófono.
- **Qué valida**: Modo texto manual, respeto estricto del estado de voz y fallback accesible ante fallas o entornos silenciosos.

---

### B. Queries de Prescripción y Progreso

#### B1. Prescripción de la serie actual
- **Acción / Frase sugerida**: *"¿Qué me toca en esta serie?"* (o *"¿Cuántas repeticiones tengo que hacer?"*).
- **Resultado esperado**: El Coach responde con el rango objetivo de repeticiones, RIR y descanso prescripto de la serie actual autoritativa, aclarando que no hay peso objetivo prescrito.
- **Qué valida**: Consulta determinística de prescripción (`provider = 0`) aislada del ejercicio visible en pantalla.

#### B2. Progreso general de la sesión
- **Acción / Frase sugerida**: *"¿Cuánto me falta para terminar la sesión?"*.
- **Resultado esperado**: Informa la cantidad de ejercicios y series completadas versus las restantes de la sesión.
- **Qué valida**: Desambiguación entre progreso estructural y time constraint de adaptación, sin llamadas al LLM.

---

### C. Registrar Serie → Editar → Confirmar

#### C1. Propuesta de registro por voz
- **Acción / Frase sugerida**: *"Hice 10 reps con 50 kilos y RIR 2"*.
- **Resultado esperado**: El Coach presenta una tarjeta de acción pendiente (`RegisterCurrentSet`) con 50 kg, 10 reps y RIR 2, pidiendo confirmación explícita.
- **Qué valida**: Extracción determinística de datos de serie, creación de pending action server-side y prioridad visual sobre otros estados.

#### C2. Corrección previa a confirmación
- **Acción / Frase sugerida**: *"En realidad fueron 12 reps con 55 kilos"*.
- **Resultado esperado**: Se actualiza la propuesta en pantalla a 55 kg y 12 reps (manteniendo el mismo RIR o ajustando), preservando el mismo `actionId` y pidiendo confirmación.
- **Qué valida**: Edición de propuesta pendiente en memoria efímera sin persistir en BD antes de confirmar.

#### C3. Confirmación explícita y auto-rest
- **Acción / Frase sugerida**: *"Sí"* (o presionar «Registrar serie»).
- **Resultado esperado**: La serie se persiste en Training, se actualiza `updatedSession`, se consume la acción pendiente (single-use) y se inicia automáticamente el timer con el descanso prescripto.
- **Qué valida**: Persistencia autoritativa en base de datos, protección single-use, sincronización de Training y auto-rest coordinado.

---

### D. Timer de Descanso

#### D1. Consulta de timer en curso
- **Acción / Frase sugerida**: *"¿Cuánto falta?"* (o *"¿Cuánto le queda al descanso?"*).
- **Resultado esperado**: Responde los segundos restantes del timer de descanso activo.
- **Qué valida**: Desambiguación entre consulta de timer activo (`IsRestQuery`) y descanso prescripto (`IsCurrentRestQuery`).

#### D2. Detener timer por voz
- **Acción / Frase sugerida**: *"Pará el descanso"*.
- **Resultado esperado**: Responde "Descanso detenido." y el timer en Training se detiene inmediatamente.
- **Qué valida**: Acción de cliente `StopRestTimer` determinística y sincronización de estado compartido con `guidedRest`.

#### D3. Iniciar timer manual
- **Acción / Frase sugerida**: *"Iniciá el descanso"*.
- **Resultado esperado**: Inicia el timer con los segundos prescriptos de la última serie completada.
- **Qué valida**: Resolución de elegibilidad para descanso manual y reutilización de prescripción autoritativa.

---

### E. Navegación Visual → Volver al Current

#### E1. Navegación visual
- **Acción / Frase sugerida**: *"Pasa al siguiente"* (o *"Mostrame el siguiente ejercicio"*).
- **Resultado esperado**: El carrusel de Training se desplaza visualmente al siguiente ejercicio, pero la serie actual pendiente en el backend no cambia.
- **Qué valida**: Desacople estricto entre `visibleExerciseSessionId` y la serie autoritativa pendiente de Training.

#### E2. Volver al ejercicio actual
- **Acción / Frase sugerida**: *"Volvé al actual"* (o *"¿Dónde tengo que seguir?"*).
- **Resultado esperado**: La interfaz de Training vuelve automáticamente al ejercicio con trabajo pendiente.
- **Qué valida**: Intent determinístico `ReturnToCurrentExercise` y navegación visual mediante `NavigateToExercise` hacia el ejercicio pendiente autoritativo.

---

### F. Skip → Reactivate

#### F1. Saltar ejercicio actual
- **Acción / Frase sugerida**: *"Saltá este ejercicio"* → Confirmar diciendo *"Sí"*.
- **Resultado esperado**: Propone `SkipCurrentExercise`; al confirmar, el ejercicio pasa a estado `Omitido`, se excluye del trabajo pendiente y Training avanza al próximo ejercicio disponible.
- **Qué valida**: Transición de estado `Omitido`, preservación de series completadas previas y actualización del resolver autoritativo.

#### F2. Reactivar ejercicio omitido
- **Acción / Frase sugerida**: *"Reactivá [Nombre del ejercicio omitido]"* → Confirmar diciendo *"Sí"*.
- **Resultado esperado**: Propone `ReactivateExercise`; al confirmar, quita el estado omitido y el target autoritativo vuelve al ejercicio reactivado.
- **Qué valida**: Reactivación por nombre, validación de estado en base de datos y sincronización de Training mediante `updatedSession`.

---

### G. BestSet Clarification + Follow-up Contextual V1

#### G1. Solicitud ambigua y clarification
- **Acción / Frase sugerida**: *"¿Cuál fue mi mejor serie?"*.
- **Resultado esperado**: El Coach solicita aclarar el criterio de comparación y muestra botones rápidos de "Peso", "Repeticiones" y "Volumen".
- **Qué valida**: Clarification efímera aislada de pending actions (`ILiveCoachClarificationStore`), sin inventar métricas ni invocar LLM.

#### G2. Selección de criterio
- **Acción / Frase sugerida**: Responder *"Peso"* (o tocar el botón "Peso").
- **Resultado esperado**: Responde determinísticamente cuál fue la mejor serie por peso y guarda la referencia en el contexto conversacional de la sesión.
- **Qué valida**: Consumo de aclaración activa, resolución determinística y almacenamiento de contexto conversacional de sesión V1.0.

#### G3. Follow-up contextual
- **Acción / Frase sugerida**: *"¿Y por repeticiones?"* (o *"¿Y en el anterior?"*).
- **Resultado esperado**: Responde la mejor serie por repeticiones del mismo ejercicio de referencia sin pedir que se especifique nuevamente el ejercicio.
- **Qué valida**: Resolución multi-turn contextual (`LiveCoachSessionFollowUpResolver`), revalidación contra estado autoritativo y `provider = 0`.

---

### H. Proactive Trigger

#### H1. Sugerencia proactiva no invasiva
- **Acción / Frase sugerida**: Dejar transcurrir el descanso 60 segundos por encima de la prescripción (o registrar dos series consecutivas por debajo del rango de reps).
- **Resultado esperado**: Aparece una sugerencia silenciosa en Live Coach, con «Ahora no» y una acción cuando corresponde. La sugerencia respeta la elegibilidad del trabajo pendiente.
- **Qué valida**: Motor proactivo determinístico V1.1, prioridad de señales, no intrusión auditiva y supresión si hay mutación/clarificación en curso.

#### H2. Invalidación de una sugerencia obsoleta
- **Acción**: Aplicar una adaptación que omita ejercicios futuros y completar las últimas series pendientes.
- **Resultado esperado**: Guided muestra «Entrenamiento listo» con el conteo de omitidos y retira «Revisar prescripción». Durante el entrenamiento, los omitidos están disponibles para consulta en Guided. La finalización permanece manual.

---

### I. Time / Fatigue / Performance / Combined Adaptation

#### Transparencia de ajustes (V1.5)

- Revisá por voz y texto el motivo, el recorte y las series pendientes antes de confirmar. Expandí el detalle y compará la prescripción de cada ejercicio conservado. Una omisión anterior debe aparecer como «Ya omitido».
- Para tiempo, compará el límite informado y la estimación antes/después. Para fatiga, verificá que el motivo se atribuya al cansancio informado. Para rendimiento y señales combinadas, contrastá la evidencia citada con el registro real.
- Para prescripción, verificá repeticiones y RIR antes/después y la serie desde la que aplica. Las series registradas conservan sus objetivos históricos.
- Aplicá por voz y con el botón. Compará lo que se aplicó, el total pendiente, la próxima serie y sus objetivos con Training. Si no quedan series, verificá que la finalización siga siendo manual.
- Cancelá o dejá vencer otra propuesta y verificá que no haya cambios ni mensaje de aplicación exitosa.

#### I1. Señal combinada o de tiempo
- **Acción / Frase sugerida**: *"Me quedan 15 minutos y estoy muy cansado"*.
- **Resultado esperado**: El Coach detecta señales combinadas (tiempo + fatiga), genera una propuesta única deduplicada de recorte de volumen y solicita confirmación explícita.
- **Qué valida**: Orquestación determinística de adaptaciones (`LiveAdaptationOrchestrator`), preservación del ejercicio actual y requisito de confirmación.

#### I2. Cancelación segura
- **Acción / Frase sugerida**: *"No, dejalo así"* (o tocar "Mantener sesión").
- **Resultado esperado**: Responde "Listo, mantengo la sesión sin cambios.", descarta la propuesta efímera y no altera el plan de entrenamiento.
- **Qué valida**: Inmutabilidad de la sesión ante descarte y limpieza segura de pending actions.

---

### J. Safe Prescription Adaptation (V1.2)

#### J1. Propuesta de adaptación de prescripción
- **Acción / Frase sugerida**: Presionar el botón de sugerencia proactiva por rendimiento o decir: *"Ajustame las repeticiones"*.
- **Resultado esperado**: El Coach propone reducir el rango de repeticiones en 2 reps (piso 4) o aumentar el RIR en 1 para las series pendientes (tope RIR 5). La propuesta pide confirmación y solo modifica reps o RIR.
- **Qué valida**: Reglas seguras de adaptación de prescripción (V1.2), cálculo determinístico y alcance acotado a series futuras.

#### J2. Confirmación y preservación de snapshots
- **Acción / Frase sugerida**: *"Sí, aplicalo"*.
- **Resultado esperado**: La sesión en Training actualiza la prescripción visible para la serie actual y siguientes, mientras que las series previas completadas conservan su snapshot original.
- **Qué valida**: Mutación segura en `EjercicioSesion`, registro auditado en `AdaptacionesLiveCoach` y sincronización en Training.

---

### K. Stale / Expired / Error Recovery

#### K1. Expiración de propuesta (TTL 45s)
- **Acción / Frase sugerida**: Solicitar el registro de una serie o adaptación y esperar más de 45 segundos antes de responder *"Sí"*.
- **Resultado esperado**: El Coach responde que la propuesta ya no está vigente porque venció y solicita pedirla de nuevo.
- **Qué valida**: Expiración server-side por TTL, protección contra confirmaciones tardías y copy descriptivo.

#### K2. Reintento y fallback de texto
- **Acción / Frase sugerida**: Provocar un corte o error en la petición de voz y tocar el botón "Reintentar" o cambiar a "Escribir".
- **Resultado esperado**: Se preserva el transcript o prompt previo sin perder estado, permitiendo reenviar o continuar por texto.
- **Qué valida**: Resiliencia de UX (V1.4), cleanup de audio en fallos y fallback accesible sin recargar la página.

---

### L. Completar Sesión

#### L1. Finalización de la sesión activa
- **Acción / Frase sugerida**: Registrar o saltar las series restantes y presionar "Finalizar entrenamiento" en Training.
- **Resultado esperado**: La sesión pasa a estado `Completada` en backend, se limpia la memoria conversacional efímera y se navega al detalle de la sesión en History.
- **Qué valida**: Transición de estado de la sesión, cleanup de stores efímeros de Coach y flujo natural hacia el análisis post-sesión.

---

### M. Post-Session Coach / History (V1.3)

#### M1. Resumen post-sesión en History
- **Acción / Frase sugerida**: Abrir la tarjeta de Post-Session Coach en el detalle de la sesión finalizada recién completada.
- **Resultado esperado**: Historial distingue ejercicios planificados, completados y omitidos. El resumen agrupa desvíos entre ejercicios y destaca hasta tres, junto con las adaptaciones aplicadas que tienen auditoría.
- **Qué valida**: Motor post-sesión determinístico (`PostSessionCoachSummaryEngine`), reporte grounded sin texto efímero inventado y omisión de propuestas canceladas.

#### M2. Preguntas interactivas post-sesión
- **Acción / Frase sugerida**: Tocar «¿Cómo me fue hoy?», «¿Qué salió bien?», «¿Dónde rendí peor?», «¿Qué adaptaciones hicimos?» y «¿Qué debería mirar la próxima vez?».
- **Resultado esperado**: Cada pregunta responde a su propósito con datos de las series y adaptaciones registradas.
- **Qué valida**: Consultas determinísticas de solo lectura (`AnswerPostSessionCoachQuery`, `provider = 0`).
