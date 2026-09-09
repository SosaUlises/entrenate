# Entrenate — Especificación MVP 1

> Plataforma inteligente de entrenamiento adaptativo.

## 1. Propósito del documento

Este documento define el alcance funcional y técnico del **MVP 1 de Entrenate**.

El objetivo es establecer una referencia común para el desarrollo de la primera versión funcional de la plataforma, especificando:

* funcionalidades incluidas;
* funcionalidades fuera de alcance;
* principales reglas de negocio;
* entidades del dominio;
* flujos principales;
* arquitectura inicial;
* tecnologías;
* estructura general del sistema;
* criterios necesarios para considerar finalizado el MVP.

Este documento deberá evolucionar junto con el proyecto cuando se tomen nuevas decisiones funcionales o técnicas.

---

# 2. Descripción del producto

**Entrenate** es una plataforma web orientada a personas que realizan entrenamiento de fuerza e hipertrofia.

Su objetivo es permitir que los usuarios puedan:

1. configurar su perfil deportivo;
2. crear y administrar rutinas;
3. consultar un catálogo de ejercicios;
4. realizar entrenamientos utilizando la aplicación;
5. registrar las series realizadas;
6. consultar entrenamientos anteriores;
7. analizar su progreso.

La visión futura del producto consiste en desarrollar un sistema capaz de analizar el historial individual de cada usuario y adaptar progresivamente su entrenamiento mediante reglas de progresión e inteligencia artificial.

El MVP 1 no implementará todavía estas funcionalidades inteligentes.

Su objetivo será construir la infraestructura funcional necesaria y comenzar a generar datos históricos de entrenamiento.

---

# 3. Objetivo del MVP 1

El MVP 1 deberá permitir completar el siguiente flujo:

**Registro → Configuración del perfil → Creación de rutina → Inicio del entrenamiento → Registro de series → Finalización → Historial → Progreso**

Al finalizar esta etapa, una persona deberá poder utilizar Entrenate como herramienta principal para registrar sus entrenamientos reales desde un teléfono móvil.

---

# 4. Alcance funcional

El MVP 1 estará compuesto por los siguientes módulos:

1. Autenticación.
2. Perfil de entrenamiento.
3. Catálogo de ejercicios.
4. Gestión de rutinas.
5. Ejecución de entrenamientos.
6. Registro de series.
7. Temporizador de descanso.
8. Historial.
9. Seguimiento del progreso.
10. Récords personales.

---

# 5. Autenticación

## 5.1 Registro

El usuario podrá crear una cuenta proporcionando los datos necesarios.

Como mínimo:

* nombre;
* correo electrónico;
* contraseña.

El nombre pertenece al usuario y forma parte del registro inicial. El apellido y los
datos deportivos, como edad, peso, sexo, objetivo y experiencia, no forman parte del
registro. Estos últimos pertenecen al onboarding y al perfil de entrenamiento.

La autenticación será administrada mediante **ASP.NET Core Identity**.

---

## 5.2 Inicio de sesión

El usuario podrá autenticarse mediante:

* correo electrónico;
* contraseña.

Una vez autenticado podrá acceder exclusivamente a sus propios datos.

---

## 5.3 Cierre de sesión

El usuario podrá finalizar su sesión activa.

---

## 5.4 Recuperación de contraseña

El sistema deberá permitir iniciar un proceso de recuperación de contraseña mediante el correo electrónico registrado.

---

# 6. Perfil de entrenamiento

Cada usuario contará con un perfil deportivo.

## Datos iniciales

* nivel de experiencia;
* objetivo principal;
* días disponibles por semana;
* duración aproximada deseada de los entrenamientos;
* disponibilidad general de equipamiento.

## Nivel de experiencia

Valores:

* Principiante
* Intermedio
* Avanzado

## Objetivo

Valores iniciales:

* Hipertrofia
* Fuerza
* Fuerza e Hipertrofia

El perfil podrá modificarse posteriormente.

---

# 7. Catálogo de ejercicios

Entrenate contará con un catálogo central de ejercicios.

Inicialmente se utilizará como fuente el paquete:

`@bryllim/workout-guide`

Sin embargo, Entrenate utilizará identificadores internos propios.

Esto permitirá cambiar el proveedor del catálogo en el futuro sin afectar las relaciones existentes dentro del sistema.

## Información de un ejercicio

Un ejercicio podrá contener:

* identificador;
* nombre;
* descripción;
* grupo muscular principal;
* músculos secundarios;
* equipamiento;
* categoría;
* instrucciones;
* identificador externo;
* ilustraciones;
* estado activo/inactivo.

## Funcionalidades

El usuario podrá:

* listar ejercicios;
* buscar por nombre;
* filtrar por grupo muscular;
* filtrar por equipamiento;
* consultar el detalle de un ejercicio;
* seleccionar ejercicios para incorporarlos a una rutina.

---

# 8. Gestión de rutinas

Un usuario podrá crear múltiples rutinas.

Ejemplos:

* Upper / Lower
* Push / Pull / Legs
* Full Body
* Rutina personalizada

Una rutina tendrá:

* nombre;
* descripción;
* fecha de creación;
* estado activa/inactiva;
* días de entrenamiento.

Inicialmente un usuario podrá tener solamente una rutina marcada como activa.

---

# 9. Días de rutina

Cada rutina estará compuesta por uno o varios días.

Ejemplo:

**Upper / Lower**

1. Upper A
2. Lower A
3. Upper B
4. Lower B

Cada día tendrá:

* nombre;
* descripción opcional;
* orden;
* ejercicios.

---

# 10. Configuración de ejercicios de una rutina

Cuando un ejercicio sea agregado a un día, deberá configurarse su objetivo.

Información:

* ejercicio;
* cantidad de series;
* repeticiones mínimas;
* repeticiones máximas;
* RIR objetivo mínimo;
* RIR objetivo máximo;
* descanso en segundos;
* orden;
* notas opcionales.

Ejemplo:

**Press banca**

* Series: 3
* Repeticiones: 8-10
* RIR: 1-2
* Descanso: 180 segundos

---

# 11. Plantillas iniciales

Entrenate podrá ofrecer estructuras básicas que faciliten la creación de rutinas.

Inicialmente:

* Full Body;
* Upper / Lower;
* Push / Pull / Legs;
* Personalizada.

Las plantillas actuarán como puntos de partida editables.

No representarán todavía metodologías adaptativas.

---

# 12. Sesión de entrenamiento

El usuario podrá seleccionar un día de su rutina activa e iniciar un entrenamiento.

Al iniciar una sesión deberán registrarse:

* usuario;
* día de rutina;
* fecha;
* hora de inicio;
* estado.

Estados posibles:

* EnCurso
* Completada
* Cancelada

La sesión generará una representación independiente de los ejercicios que deben realizarse.

---

# 13. Snapshot de la planificación

Una sesión iniciada deberá conservar la configuración existente en el momento de comenzar el entrenamiento.

Esto significa que posteriores modificaciones de la rutina no deberán alterar entrenamientos históricos.

Por ejemplo:

Si el 30/08 el usuario realizó:

**Press banca — 3 × 8-10**

y posteriormente modifica su rutina a:

**Press banca — 4 × 6-8**

el entrenamiento del 30/08 deberá continuar mostrando la configuración original.

Por esta razón existirá una separación entre:

**EjercicioRutina**

y

**EjercicioSesion**

---

# 14. Ejecución de ejercicios

Durante el entrenamiento, el usuario visualizará cada ejercicio planificado.

Se deberá mostrar:

* nombre;
* ilustración;
* cantidad de series objetivo;
* rango de repeticiones;
* RIR objetivo;
* descanso;
* notas;
* rendimiento de la sesión anterior.

---

# 15. Sesión anterior

Cuando exista información histórica, Entrenate deberá mostrar los resultados obtenidos la última vez que el usuario realizó el mismo ejercicio.

Ejemplo:

### Press banca

**Última sesión**

| Serie |   Peso | Reps | RIR |
| ----- | -----: | ---: | --: |
| 1     | 100 kg |   10 |   2 |
| 2     | 100 kg |    9 |   1 |
| 3     | 100 kg |    8 |   0 |

En el MVP 1 esta información será exclusivamente informativa.

El sistema no recomendará todavía una carga.

---

# 16. Registro de series

El usuario podrá registrar cada serie realizada.

Cada serie contendrá:

* número de serie;
* peso utilizado;
* repeticiones realizadas;
* RIR;
* estado completada/no completada;
* fecha y hora del registro.

Ejemplo:

**Serie 1**

Peso: `100 kg`

Repeticiones: `10`

RIR: `2`

---

# 17. Validaciones de series

Inicialmente deberán contemplarse las siguientes reglas:

* el peso no podrá ser negativo;
* las repeticiones no podrán ser negativas;
* el RIR no podrá ser negativo;
* el número de serie deberá ser válido;
* una serie deberá pertenecer a un ejercicio de la sesión activa.

Se deberá permitir peso `0` para ejercicios realizados exclusivamente con peso corporal cuando corresponda.

Los límites máximos razonables deberán definirse durante la implementación.

---

# 18. Temporizador de descanso

Después de registrar una serie, Entrenate podrá iniciar automáticamente el temporizador configurado para ese ejercicio.

El usuario podrá:

* visualizar el tiempo restante;
* pausar;
* continuar;
* reiniciar;
* finalizar anticipadamente.

El temporizador será principalmente responsabilidad del frontend.

No será necesario persistir cada segundo del temporizador en el backend.

---

# 19. Finalización del entrenamiento

El usuario podrá finalizar una sesión activa.

Al finalizar deberán registrarse:

* hora de finalización;
* estado `Completada`.

La plataforma generará un resumen.

## Información del resumen

* nombre del entrenamiento;
* duración;
* ejercicios realizados;
* series realizadas;
* volumen total;
* posibles récords personales.

---

# 20. Cálculo de volumen

Para ejercicios con carga externa, el volumen básico de una serie se calculará utilizando:

`Volumen = Peso × Repeticiones`

El volumen de un ejercicio será la suma de sus series.

El volumen total de una sesión será la suma del volumen de todos los ejercicios correspondientes.

Esta métrica será utilizada inicialmente con fines descriptivos.

---

# 21. 1RM estimado

Entrenate podrá calcular una estimación del máximo para una repetición.

Inicialmente podrá utilizarse una fórmula estándar como Epley:

`1RM estimado = Peso × (1 + Repeticiones / 30)`

Esta métrica deberá considerarse una estimación y no un máximo real realizado por el usuario.

Podrá utilizarse para:

* historial;
* gráficos;
* récords;
* análisis futuro.

---

# 22. Historial de entrenamientos

El usuario podrá consultar todas sus sesiones completadas.

La vista general deberá mostrar:

* fecha;
* rutina/día;
* duración;
* cantidad de ejercicios;
* cantidad de series.

Al seleccionar una sesión se podrá consultar:

* ejercicios realizados;
* series;
* pesos;
* repeticiones;
* RIR;
* volumen.

---

# 23. Historial por ejercicio

Cada ejercicio contará con una vista de historial individual.

Se podrá visualizar:

* fechas en las que fue realizado;
* series;
* cargas;
* repeticiones;
* RIR;
* volumen;
* 1RM estimado.

Esto permitirá analizar la evolución del usuario específicamente para ese ejercicio.

---

# 24. Seguimiento del progreso

El MVP 1 deberá proporcionar estadísticas descriptivas básicas.

Inicialmente:

* cantidad total de entrenamientos;
* entrenamientos por semana;
* evolución de cargas;
* evolución del 1RM estimado;
* volumen por sesión;
* volumen por ejercicio;
* récords personales.

No se realizarán todavía recomendaciones a partir de estas métricas.

---

# 25. Récords personales

El sistema podrá detectar récords relacionados con:

* mayor peso;
* mayor cantidad de repeticiones;
* mejor 1RM estimado;
* mayor volumen.

La implementación definitiva determinará cuáles deberán persistirse y cuáles podrán calcularse dinámicamente.

---

# 26. Entidades del dominio

Las entidades principales del MVP 1 serán:

## Usuario

Representa una persona registrada.

Extenderá ASP.NET Core Identity.

---

## PerfilEntrenamiento

Representa las características deportivas generales del usuario.

Relación:

`Usuario 1 — 1 PerfilEntrenamiento`

---

## Ejercicio

Representa un ejercicio del catálogo.

---

## Rutina

Representa una planificación perteneciente a un usuario.

Relación:

`Usuario 1 — N Rutina`

---

## DiaRutina

Representa un día dentro de una rutina.

Relación:

`Rutina 1 — N DiaRutina`

---

## EjercicioRutina

Representa la configuración de un ejercicio dentro de un día.

Relaciones:

`DiaRutina 1 — N EjercicioRutina`

`Ejercicio 1 — N EjercicioRutina`

---

## SesionEntrenamiento

Representa un entrenamiento realmente iniciado por el usuario.

Relación:

`Usuario 1 — N SesionEntrenamiento`

---

## EjercicioSesion

Representa un ejercicio realizado dentro de una sesión.

Relaciones:

`SesionEntrenamiento 1 — N EjercicioSesion`

`Ejercicio 1 — N EjercicioSesion`

---

## SerieEntrenamiento

Representa una serie realizada.

Relación:

`EjercicioSesion 1 — N SerieEntrenamiento`

---

## RecordPersonal

Representa un récord conseguido por el usuario.

---

# 27. Modelo conceptual

```text
Usuario
│
├── PerfilEntrenamiento
│
├── Rutinas
│   │
│   └── DiaRutina
│       │
│       └── EjercicioRutina
│           │
│           └── Ejercicio
│
├── SesionesEntrenamiento
│   │
│   └── EjercicioSesion
│       │
│       ├── Ejercicio
│       │
│       └── SerieEntrenamiento
│
└── RecordsPersonales
    │
    └── Ejercicio
```

---

# 28. Principio de separación entre planificación y ejecución

El sistema deberá diferenciar explícitamente:

## Lo que el usuario debería realizar

`Rutina → DiaRutina → EjercicioRutina`

de:

## Lo que el usuario realmente realizó

`SesionEntrenamiento → EjercicioSesion → SerieEntrenamiento`

El historial de entrenamiento será considerado información histórica y no deberá modificarse automáticamente cuando una rutina sea editada posteriormente.

Esta regla será fundamental para el funcionamiento futuro del sistema adaptativo y los modelos de inteligencia artificial.

---

# 29. Arquitectura

Entrenate utilizará inicialmente una arquitectura distribuida en tres componentes principales.

```text
┌──────────────────────────┐
│       FRONTEND           │
│                         │
│ Next.js                  │
│ React                    │
│ TypeScript               │
└────────────┬─────────────┘
             │
             │ HTTPS / JSON
             ▼
┌──────────────────────────┐
│        BACKEND           │
│                         │
│ ASP.NET Core Web API     │
│ C#                       │
│ Entity Framework Core    │
│ ASP.NET Core Identity    │
└────────────┬─────────────┘
             │
             ▼
┌──────────────────────────┐
│      BASE DE DATOS       │
│                         │
│ PostgreSQL / SQL Server  │
└──────────────────────────┘
```

En el MVP 1 todavía no existirá un servicio de inteligencia artificial.

Sin embargo, la arquitectura deberá permitir incorporarlo posteriormente:

```text
Next.js
   │
   ▼
ASP.NET Core API
   │
   ├──────────► Base de datos
   │
   └──────────► Servicio IA Python
                    [MVP 3]
```

---

# 30. Backend

## Tecnología

* C#
* ASP.NET Core Web API
* Entity Framework Core
* ASP.NET Core Identity

El backend será responsable de:

* autenticación;
* autorización;
* usuarios;
* perfiles;
* ejercicios;
* rutinas;
* sesiones;
* series;
* historial;
* estadísticas;
* reglas de negocio.

La lógica principal del dominio deberá permanecer en el backend.

El frontend no deberá convertirse en responsable de las reglas de negocio centrales.

---

# 31. Arquitectura interna del backend

Inicialmente se propone separar el backend en los siguientes proyectos:

```text
Entrenate.Backend
│
├── Entrenate.Domain
├── Entrenate.Application
├── Entrenate.Infrastructure
└── Entrenate.Api
```

## Entrenate.Domain

Contendrá:

* entidades;
* enumeraciones;
* reglas fundamentales del dominio;
* interfaces estrictamente pertenecientes al dominio cuando sean necesarias.

No deberá depender de Infrastructure ni de Api.

---

## Entrenate.Application

Contendrá los casos de uso de la aplicación.

Ejemplos:

* crear rutina;
* modificar rutina;
* iniciar entrenamiento;
* registrar serie;
* finalizar entrenamiento;
* obtener historial.

También podrá contener:

* DTOs;
* validaciones;
* interfaces de servicios;
* comandos/consultas si se adopta CQRS posteriormente.

---

## Entrenate.Infrastructure

Contendrá implementaciones relacionadas con infraestructura.

Por ejemplo:

* Entity Framework Core;
* DbContext;
* configuraciones de entidades;
* repositorios cuando sean necesarios;
* ASP.NET Identity;
* servicios externos;
* persistencia.

---

## Entrenate.Api

Será el punto de entrada HTTP.

Contendrá:

* controllers/endpoints;
* configuración;
* middleware;
* autenticación;
* Dependency Injection;
* documentación de API.

---

# 32. Frontend

## Tecnología

* Next.js
* React
* TypeScript

La aplicación deberá desarrollarse utilizando un enfoque mobile-first.

El frontend será responsable principalmente de:

* interfaz;
* navegación;
* estado visual;
* formularios;
* consumo de API;
* temporizador;
* gráficos;
* experiencia durante el entrenamiento.

---

# 33. Pantallas principales

El MVP 1 deberá contemplar inicialmente:

### Públicas

* Landing Page
* Registro
* Inicio de sesión
* Recuperar contraseña

### Onboarding

* Configuración de perfil

### Aplicación

* Inicio / Dashboard
* Mis rutinas
* Crear rutina
* Editar rutina
* Catálogo de ejercicios
* Detalle de ejercicio
* Iniciar entrenamiento
* Entrenamiento en curso
* Resumen de entrenamiento
* Historial
* Detalle de sesión
* Progreso
* Historial de ejercicio
* Perfil

---

# 34. API inicial

Los endpoints definitivos podrán modificarse durante el desarrollo.

Como referencia inicial:

## Autenticación

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/logout
POST /api/auth/forgot-password
POST /api/auth/reset-password
```

## Perfil

```text
GET  /api/perfil
POST /api/perfil
PUT  /api/perfil
```

## Ejercicios

```text
GET /api/ejercicios
GET /api/ejercicios/{id}
```

Con filtros mediante query parameters.

Ejemplo:

```text
GET /api/ejercicios?musculo=Pecho
```

## Rutinas

```text
GET    /api/rutinas
GET    /api/rutinas/{id}
POST   /api/rutinas
PUT    /api/rutinas/{id}
DELETE /api/rutinas/{id}
```

## Entrenamientos

```text
POST /api/entrenamientos
GET  /api/entrenamientos/{id}

POST /api/entrenamientos/{id}/series

POST /api/entrenamientos/{id}/finalizar
POST /api/entrenamientos/{id}/cancelar
```

## Historial

```text
GET /api/historial
GET /api/historial/ejercicios/{ejercicioId}
```

## Progreso

```text
GET /api/progreso/resumen
GET /api/progreso/ejercicios/{ejercicioId}
```

---

# 35. Base de datos

Se utilizará una base de datos relacional.

Las alternativas iniciales son:

* PostgreSQL;
* SQL Server.

La decisión definitiva deberá tomarse antes de implementar la infraestructura de persistencia.

Entity Framework Core será utilizado como ORM.

Las modificaciones del esquema deberán realizarse mediante migraciones.

---

# 36. Identificadores

Las entidades utilizarán identificadores internos independientes de proveedores externos.

Los ejercicios importados desde Workout Guide conservarán opcionalmente su identificador externo como referencia.

Nunca deberá utilizarse el identificador externo como única clave de negocio interna.

---

# 37. Unidades

Inicialmente Entrenate utilizará:

**Peso: kilogramos (kg)**

El modelo deberá evitar almacenar textos como:

`"100 kg"`

En su lugar deberá almacenarse numéricamente:

`100.00`

y la interfaz será responsable de representar la unidad.

El soporte para libras podrá incorporarse posteriormente.

---

# 38. Fechas y horas

Las fechas almacenadas por el backend deberán manejarse de forma consistente.

Se recomienda almacenar timestamps utilizando UTC y convertirlos a la zona horaria correspondiente en la presentación.

Esto será especialmente importante para:

* inicio de entrenamiento;
* finalización;
* registro de series;
* historial.

---

# 39. Seguridad

Como mínimo:

* contraseñas administradas mediante Identity;
* endpoints privados protegidos;
* autorización por usuario;
* validación de entrada;
* protección de información sensible;
* utilización de HTTPS en producción.

Un usuario nunca deberá poder consultar o modificar rutinas, sesiones o información perteneciente a otro usuario.

---

# 40. Experiencia mobile-first

La pantalla más importante del MVP será **Entrenamiento en curso**.

La experiencia deberá minimizar las interacciones necesarias para registrar una serie.

Principios:

* controles grandes;
* números fácilmente editables;
* navegación simple;
* mostrar inmediatamente la última sesión;
* pocas acciones por serie;
* temporizador visible;
* evitar recargar páginas;
* mantener el estado durante el entrenamiento;
* minimizar escritura manual.

El producto deberá poder utilizarse cómodamente entre series dentro de un gimnasio.

---

# 41. Persistencia durante una sesión

Los datos registrados durante un entrenamiento deberán persistirse progresivamente.

No se deberá esperar hasta finalizar toda la sesión para enviar la información al backend.

Cuando una serie sea registrada correctamente deberá persistirse.

Esto reduce el riesgo de perder un entrenamiento completo por:

* cierre accidental del navegador;
* actualización de la página;
* pérdida temporal de conexión;
* batería;
* error del frontend.

Cuando sea posible, el frontend deberá permitir recuperar una sesión que continúa en estado `EnCurso`.

---

# 42. Manejo básico de errores

La API deberá proporcionar respuestas consistentes ante errores.

Como mínimo deberán diferenciarse:

* errores de validación;
* recurso inexistente;
* usuario no autorizado;
* conflicto de estado;
* error interno.

El frontend deberá mostrar mensajes comprensibles para el usuario y no mensajes técnicos provenientes directamente del servidor.

---

# 43. Datos importantes para futuras etapas

Aunque determinadas variables todavía no sean utilizadas para recomendaciones, deberán almacenarse desde el MVP 1 cuando sean relevantes.

Especialmente:

* fecha;
* ejercicio;
* peso;
* repeticiones;
* RIR;
* orden de serie;
* objetivo de repeticiones;
* RIR objetivo;
* duración aproximada de la sesión.

Estos datos constituirán posteriormente el dataset histórico utilizado por los componentes adaptativos.

---

# 44. Fuera del alcance del MVP 1

No se desarrollarán todavía:

* inteligencia artificial;
* Machine Learning;
* servicio Python;
* recomendación automática de peso;
* adaptación automática de rutinas;
* detección inteligente de estancamiento;
* AI Coach;
* chatbot;
* generación automática de rutinas mediante IA;
* Computer Vision;
* análisis de videos;
* nutrición;
* calorías;
* planes alimenticios;
* integración con wearables;
* funciones sociales;
* rankings;
* entrenadores;
* gimnasios;
* administración de establecimientos;
* suscripciones;
* pagos;
* Plan Pro.

Estas funcionalidades no deberán incorporarse durante el desarrollo del MVP 1 salvo que se actualice explícitamente esta especificación.

---

# 45. Preparación para MVP 2

El MVP 1 deberá generar una base sólida para incorporar posteriormente un **motor adaptativo de entrenamiento**.

El MVP 2 utilizará principalmente:

* historial de sesiones;
* cargas;
* repeticiones;
* RIR;
* objetivos configurados;
* evolución del rendimiento.

Sobre estos datos podrán implementarse funcionalidades como:

* recomendación de próxima carga;
* progresión automática;
* mantenimiento de carga;
* detección de estancamientos;
* ajustes básicos de entrenamiento.

Por esta razón, la calidad y consistencia de los datos registrados durante el MVP 1 será considerada crítica.

---

# 46. Preparación para MVP 3

El MVP 3 incorporará componentes desarrollados en Python relacionados con análisis de datos y Machine Learning.

Conceptualmente:

```text
Entrenate Frontend
       │
       ▼
Entrenate API
       │
       ├──────────────► Base de datos
       │
       ▼
Entrenate AI Service
       │
       ├── Python
       ├── Pandas
       ├── NumPy
       └── Scikit-learn
```

El servicio de IA no deberá acceder directamente desde el frontend.

ASP.NET Core continuará funcionando como backend principal y será responsable de coordinar las funcionalidades inteligentes.

---

# 47. Criterios de aceptación del MVP 1

El MVP 1 podrá considerarse funcionalmente terminado cuando se cumpla el siguiente escenario completo:

### Dado

un usuario nuevo que desea utilizar Entrenate,

### cuando

1. crea una cuenta;
2. inicia sesión;
3. configura su perfil;
4. crea una rutina;
5. crea los días correspondientes;
6. busca ejercicios;
7. incorpora ejercicios a la rutina;
8. configura series, repeticiones, RIR y descansos;
9. selecciona un día;
10. inicia un entrenamiento;
11. visualiza su planificación;
12. registra las series realizadas;
13. utiliza el temporizador;
14. finaliza el entrenamiento;
15. consulta el resumen;
16. vuelve a realizar el mismo ejercicio en otra sesión;
17. visualiza los resultados de la sesión anterior;
18. consulta posteriormente su historial;
19. visualiza la evolución de un ejercicio;

### entonces

la plataforma deberá conservar correctamente toda la información y permitir reconstruir el historial de entrenamiento sin depender del estado actual de la rutina.

---

# 48. Definición de terminado

Una funcionalidad del MVP se considerará terminada cuando:

* cumpla el comportamiento definido;
* tenga validaciones;
* respete autorización;
* persista correctamente la información;
* gestione errores esperables;
* tenga una interfaz utilizable desde móvil;
* no rompa información histórica;
* cuente con pruebas para la lógica crítica cuando corresponda.

---

# 49. Principios del proyecto

Durante el desarrollo del MVP deberán mantenerse los siguientes principios.

### 1. El historial es inmutable conceptualmente

Modificar una rutina futura nunca deberá modificar lo que ocurrió en entrenamientos anteriores.

### 2. Registrar debe ser rápido

La aplicación será utilizada durante el entrenamiento.

La UX deberá minimizar fricción.

### 3. El backend controla las reglas

Las reglas centrales del dominio no deberán existir exclusivamente en React.

### 4. La IA llegará después de los datos

El MVP 1 deberá construir primero una plataforma funcional y generar información histórica confiable.

### 5. Evitar complejidad prematura

No deberán incorporarse patrones, microservicios o abstracciones únicamente por razones académicas.

Toda decisión arquitectónica deberá solucionar una necesidad real del proyecto.

### 6. Preparar sin sobrearquitecturar

El sistema deberá permitir evolucionar hacia MVP 2 y MVP 3, pero sin implementar anticipadamente funcionalidades que todavía no son necesarias.

---

# 50. Stack tecnológico MVP 1

| Componente           | Tecnología              |
| -------------------- | ----------------------- |
| Backend              | C#                      |
| API                  | ASP.NET Core Web API    |
| ORM                  | Entity Framework Core   |
| Autenticación        | ASP.NET Core Identity   |
| Frontend             | Next.js                 |
| UI                   | React                   |
| Lenguaje frontend    | TypeScript              |
| Base de datos        | PostgreSQL o SQL Server |
| Catálogo             | @bryllim/workout-guide  |
| API                  | REST / JSON             |
| Control de versiones | Git                     |
| Repositorio          | GitHub                  |

---

# 51. Estructura inicial del repositorio

Se propone comenzar utilizando un único repositorio.

```text
entrenate/
│
├── backend/
│   │
│   ├── src/
│   │   ├── Entrenate.Domain/
│   │   ├── Entrenate.Application/
│   │   ├── Entrenate.Infrastructure/
│   │   └── Entrenate.Api/
│   │
│   └── tests/
│
├── frontend/
│
├── docs/
│   └── spec.md
│
├── .gitignore
├── README.md
└── LICENSE
```

El servicio Python podrá agregarse cuando comience el MVP 3:

```text
entrenate/
│
├── backend/
├── frontend/
├── ai/
├── docs/
├── README.md
└── ...
```

No será necesario crear el proyecto `ai` durante el MVP 1.

---

# 52. Objetivo técnico inmediato

Una vez creado el repositorio, el orden inicial de implementación será:

1. crear solución .NET;
2. crear proyectos Domain, Application, Infrastructure y Api;
3. configurar referencias entre proyectos;
4. crear aplicación Next.js;
5. seleccionar y configurar base de datos;
6. implementar las entidades iniciales;
7. configurar Entity Framework Core;
8. configurar ASP.NET Core Identity;
9. crear primera migración;
10. implementar autenticación;
11. implementar perfil de entrenamiento;
12. implementar catálogo;
13. implementar rutinas;
14. implementar sesiones;
15. implementar historial;
16. implementar estadísticas básicas.

A partir de este punto el desarrollo deberá realizarse incrementalmente, completando flujos funcionales antes de agregar nuevas funcionalidades.

---

# 53. Visión posterior al MVP 1

El MVP 1 responde principalmente:

**¿Qué entrené?**

**¿Cuánto levanté?**

**¿Cuántas repeticiones hice?**

**¿Cómo fue mi rendimiento anterior?**

**¿Cómo evolucioné?**

El MVP 2 deberá comenzar a responder:

**¿Qué debería hacer en mi próximo entrenamiento?**

Y el MVP 3 buscará responder:

**¿Qué podemos aprender de mi historial para personalizar cada vez mejor mi entrenamiento?**

Esta evolución representa el núcleo de Entrenate:

**Registrar → Analizar → Adaptar → Progresar.**
