# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Entrenate está dirigido a personas que entrenan fuerza e hipertrofia por su cuenta, desde principiantes hasta avanzadas.

Debe ser suficientemente simple para quien recién empieza y suficientemente sólido para personas con años de entrenamiento. Su uso principal ocurre desde el teléfono, incluso durante una sesión real en el gimnasio. La claridad y facilidad de uso también deben contemplar a adultos mayores.

## Product Purpose

Entrenate permite organizar rutinas, ejecutar entrenamientos, registrar series y consultar la actividad, el historial y el progreso dentro de una experiencia continua.

El MVP busca que una persona pueda utilizar Entrenate como herramienta principal para registrar entrenamientos reales y construir un historial confiable.

Sobre esa base de datos reales, Entrenate evolucionará hacia una capa inteligente denominada **Entrenate Coach**, capaz de analizar el contexto, progreso y planificación del usuario para ofrecer asistencia personalizada.

Las capacidades de Entrenate Coach se incorporan incrementalmente y no deben presentarse como disponibles antes de estar implementadas.

Su especificación y roadmap se encuentran en `docs/coach.md`.

## Positioning

El diferencial actual es la continuidad completa del entrenamiento:

`rutina → sesión guiada o libre → historial → actividad → progreso`

Entrenate evita fragmentar este recorrido en herramientas separadas. La experiencia mobile-first está pensada para el uso real dentro del gimnasio, con baja fricción, lectura rápida y recuperación del contexto del usuario.

La evolución del producto busca extender esta continuidad con inteligencia contextual. Entrenate Coach no se plantea como un chatbot genérico, sino como una capa capaz de utilizar los datos reales del entrenamiento para analizar, explicar, recomendar y, progresivamente, asistir en la planificación y adaptación.

## Operating Context

- Uso prioritario desde teléfonos móviles durante entrenamientos de fuerza e hipertrofia.
- Interacciones breves entre series, con atención limitada y necesidad de lectura inmediata.
- Creación y administración de rutinas con días, ejercicios, series, repeticiones, RIR, descanso y notas.
- Dos formas de registrar una sesión: modo guiado y carga libre.
- Continuidad de sesiones activas y borradores de rutinas ante navegación, cierre o interrupciones razonables.
- Consulta posterior de historial, actividad y progreso.
- Las futuras capacidades inteligentes deben utilizar este contexto existente en lugar de obligar al usuario a volver a describir manualmente su situación en cada interacción.

## Capabilities and Constraints

### Current product capabilities

- Autenticación, recuperación de contraseña y perfil de entrenamiento.
- Catálogo de ejercicios con búsqueda, filtros, imágenes y demostraciones técnicas cuando existen.
- Creación, edición y consulta de rutinas.
- Sesiones de entrenamiento guiadas o de carga libre, con registro de peso, repeticiones y RIR.
- Descanso local durante el modo guiado.
- Recuperación de sesiones activas sin crear sesiones concurrentes.
- Historial, calendario de actividad y seguimiento del progreso basados en datos reales.
- El backend conserva las reglas centrales del dominio; el frontend no debe inventar contratos ni métricas.

### Future intelligent capabilities

Entrenate Coach incorporará progresivamente capacidades como:

- análisis contextual del progreso;
- recomendaciones de entrenamiento;
- asistencia para crear y modificar rutinas;
- adaptación del entrenamiento ante cambios de tiempo, equipamiento, preferencias o contexto;
- adaptación conservadora ante molestias o restricciones, sin realizar diagnóstico médico;
- orientación nutricional general vinculada al entrenamiento;
- insights proactivos cuando los datos del usuario justifiquen una intervención útil.

Estas capacidades están definidas por versiones en `docs/coach.md`.

Documentarlas no implica que estén disponibles actualmente.

### Current MVP exclusions

El MVP actual no incluye todavía:

- Entrenate Coach;
- recomendaciones automáticas;
- motor adaptativo;
- generación inteligente de rutinas;
- adaptación por molestias;
- nutrición inteligente;
- funciones sociales;
- pagos;
- wearables;
- gimnasios;
- gestión de clientes por entrenadores.

Las capacidades del Coach deben habilitarse únicamente a medida que cada versión sea implementada y validada.

## Entrenate Coach

**Entrenate Coach** es la futura capa inteligente del producto.

No es una mascota, personaje animado ni avatar. Su identidad debe integrarse con la dirección seria, premium y orientada al rendimiento de Entrenate.

Su objetivo es utilizar progresivamente información como:

- perfil del usuario;
- objetivo;
- nivel de experiencia;
- equipamiento;
- rutinas;
- planificación;
- sesiones realizadas;
- series;
- RIR;
- historial;
- progreso;

para ofrecer asistencia contextual.

El principio arquitectónico y de producto es:

`datos reales → reglas/motores → decisión estructurada → interacción y explicación`

Un modelo de lenguaje puede participar en interpretación y conversación, pero no debe convertirse por sí solo en la fuente de verdad de las decisiones de entrenamiento ni evitar las reglas normales del producto.

La definición detallada, límites, seguridad y roadmap de Entrenate Coach se encuentran en:

`docs/coach.md`

## Brand Commitments

- El nombre del producto es **Entrenate**.
- La comunicación utiliza español rioplatense natural, claro y directo.
- La personalidad debe sentirse premium, tecnológica, deportiva, precisa, moderna y muy fácil de usar.
- La identidad debe surgir de la marca, el sistema visual, la tipografía, el color, el movimiento, la interacción, los datos y la claridad.
- Entrenate Coach debe formar parte de esta misma identidad y no introducir una mascota, personaje recurrente ni estética separada del producto.
- Las demostraciones de ejercicios pueden usar una figura humana neutral, sin identidad ni rostro reconocible, exclusivamente para explicar el movimiento.
- Evitar una identidad cyberpunk, gaming, SaaS genérica, basada en glassmorphism excesivo, en superficies anidadas o en recursos decorativos recurrentes.

## Evidence on Hand

- Especificación funcional del MVP: `docs/spec.md`.
- Sistema visual y tokens vigentes: `docs/design-system.md`.
- Arquitectura frontend: `docs/frontend-architecture.md`.
- Especificación y roadmap de Entrenate Coach: `docs/coach.md`.
- Estado actual de implementación: `docs/PROJECT-STATUS.md`.
- Implementación real del frontend autenticado: `frontend/src/`.
- Contratos y reglas reales del backend: `backend/src/`.
- Master oficial de marca: `frontend/public/brand/entrenate-wordmark-master.png`.
- Assets actuales de demostraciones de ejercicios: `frontend/public/exercises/v2/`.
- Referencia maestra del modelo técnico neutral: `frontend/public/exercises/v2/_master/neutral-exercise-model-v1-master.png`.
- No hay evidencia aprobada de testimonios, clientes, benchmarks comerciales, precios o capacidades inteligentes todavía no implementadas; no deben fabricarse.

## Product Principles

1. Mantener la continuidad del usuario a través de todo el recorrido de entrenamiento.
2. Hacer que registrar durante una sesión real sea rápido, claro y confiable.
3. Mostrar solo capacidades, datos y estados que existan realmente.
4. Servir a distintos niveles de experiencia sin volver compleja la operación básica.
5. Preparar datos consistentes para evoluciones futuras sin anticipar funcionalidades ni arquitectura innecesaria.
6. Construir las capacidades inteligentes sobre datos reales y reglas explícitas, no sobre respuestas libres de un modelo.
7. Introducir Entrenate Coach de manera incremental, manteniendo claramente separado lo implementado de lo planificado.
8. Mantener al usuario en control de los cambios importantes propuestos por sistemas inteligentes.

## Accessibility & Inclusion

- Diseño mobile-first con prioridad operativa en pantallas pequeñas.
- Controles táctiles cómodos, foco visible, contraste suficiente y controles semánticos.
- Jerarquía, copy y tamaños legibles para personas con distintos niveles de experiencia y para adultos mayores.
- Las acciones y estados no deben depender exclusivamente del color, de iconos sin texto ni de conocimientos técnicos de entrenamiento.
- Las recomendaciones inteligentes deben explicar suficientemente su intención y no depender exclusivamente de indicadores visuales ambiguos.

## Coach product placement

Entrenate Coach is a premium product area within Entrenate.

- Coach has its own entry point in the primary navigation.
- The Coach Hub is the root surface for all Coach capabilities.
- Current capabilities:
  - Progress Coach
- Planned capabilities:
  - Routine Planner
  - Adaptive Training
  - Nutrition Coach
- Planned capabilities must not expose functional flows before implementation.
- Unavailable capabilities may appear only as clearly disabled / coming soon entries.
- Coach uses the Acero Térmico V5 design system and does not introduce a separate visual identity.