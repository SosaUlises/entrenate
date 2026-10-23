# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Entrenate está dirigido a personas que entrenan fuerza e hipertrofia por su cuenta, desde principiantes hasta avanzadas.

Debe ser suficientemente simple para quien recién empieza y suficientemente sólido para personas con años de entrenamiento. Su uso principal ocurre desde el teléfono, incluso durante una sesión real en el gimnasio. La claridad y facilidad de uso también deben contemplar a adultos mayores.

## Product Purpose

Entrenate permite organizar rutinas, ejecutar entrenamientos, registrar series y consultar la actividad, el historial y el progreso dentro de una experiencia continua.

El MVP busca que una persona pueda utilizar Entrenate como herramienta principal para registrar entrenamientos reales y construir un historial confiable. Las capacidades inteligentes o adaptativas pertenecen a etapas futuras y no deben presentarse como disponibles antes de estar implementadas.

## Positioning

El diferencial actual es la continuidad completa del entrenamiento:

`rutina → sesión guiada o libre → historial → actividad → progreso`

Entrenate evita fragmentar este recorrido en herramientas separadas. La experiencia mobile-first está pensada para el uso real dentro del gimnasio, con baja fricción, lectura rápida y recuperación del contexto del usuario.

## Operating Context

- Uso prioritario desde teléfonos móviles durante entrenamientos de fuerza e hipertrofia.
- Interacciones breves entre series, con atención limitada y necesidad de lectura inmediata.
- Creación y administración de rutinas con días, ejercicios, series, repeticiones, RIR, descanso y notas.
- Dos formas de registrar una sesión: modo guiado y carga libre.
- Continuidad de sesiones activas y borradores de rutinas ante navegación, cierre o interrupciones razonables.
- Consulta posterior de historial, actividad y progreso.

## Capabilities and Constraints

- Autenticación, recuperación de contraseña y perfil de entrenamiento.
- Catálogo de ejercicios con búsqueda, filtros, imágenes y demostraciones técnicas cuando existen.
- Creación, edición y consulta de rutinas.
- Sesiones de entrenamiento guiadas o de carga libre, con registro de peso, repeticiones y RIR.
- Descanso local durante el modo guiado.
- Recuperación de sesiones activas sin crear sesiones concurrentes.
- Historial, calendario de actividad y seguimiento del progreso basados en datos reales.
- El backend conserva las reglas centrales del dominio; el frontend no debe inventar contratos ni métricas.
- El MVP no incluye recomendaciones automáticas, IA, nutrición, funciones sociales, pagos, wearables, gimnasios ni gestión de clientes por entrenadores.

## Brand Commitments

- El nombre del producto es **Entrenate**.
- La comunicación utiliza español rioplatense natural, claro y directo.
- La personalidad debe sentirse premium, tecnológica, deportiva, precisa, moderna y muy fácil de usar.
- La identidad debe surgir de la marca, el sistema visual, la tipografía, el color, el movimiento, la interacción, los datos y la claridad.
- Las demostraciones de ejercicios pueden usar una figura humana neutral, sin identidad ni rostro reconocible, exclusivamente para explicar el movimiento.
- Evitar una identidad cyberpunk, gaming, SaaS genérica, basada en glassmorphism excesivo, en superficies anidadas o en recursos decorativos recurrentes.

## Evidence on Hand

- Especificación funcional del MVP: `docs/spec.md`.
- Sistema visual y tokens vigentes: `docs/design-system.md`.
- Arquitectura frontend: `docs/frontend-architecture.md`.
- Implementación real del frontend autenticado: `frontend/src/`.
- Contratos y reglas reales del backend: `backend/src/`.
- Master oficial de marca: `frontend/public/brand/entrenate-wordmark-master.png`.
- Assets actuales de demostraciones de ejercicios: `frontend/public/exercises/v2/`.
- Referencia maestra del modelo técnico neutral: `frontend/public/exercises/v2/_master/neutral-exercise-model-v1-master.png`.
- No hay evidencia aprobada de testimonios, clientes, benchmarks comerciales, precios o capacidades de IA implementadas; no deben fabricarse.

## Product Principles

1. Mantener la continuidad del usuario a través de todo el recorrido de entrenamiento.
2. Hacer que registrar durante una sesión real sea rápido, claro y confiable.
3. Mostrar solo capacidades, datos y estados que existan realmente.
4. Servir a distintos niveles de experiencia sin volver compleja la operación básica.
5. Preparar datos consistentes para evoluciones futuras sin anticipar funcionalidades ni arquitectura innecesaria.

## Accessibility & Inclusion

- Diseño mobile-first con prioridad operativa en pantallas pequeñas.
- Controles táctiles cómodos, foco visible, contraste suficiente y controles semánticos.
- Jerarquía, copy y tamaños legibles para personas con distintos niveles de experiencia y para adultos mayores.
- Las acciones y estados no deben depender exclusivamente del color, de iconos sin texto ni de conocimientos técnicos de entrenamiento.
