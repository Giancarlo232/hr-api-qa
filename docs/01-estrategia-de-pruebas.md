# 1. Estrategia de pruebas

## 1.1 Objetivo

Demostrar, con evidencia objetiva y repetible, que la API de gestión de colaboradores y empresas cumple los requisitos funcionales y se comporta de forma estable bajo carga, antes de liberarla a producción. El entregable principal es un **conjunto de pruebas repetible y trazable**; encontrar defectos es una consecuencia, no el fin.

## 1.2 Alcance

| Dentro del alcance | Fuera del alcance |
|---|---|
| CRUD de países, departamentos, municipios, empresas y colaboradores | Interfaz web (solo se prueba la API REST) |
| Reglas de negocio de empresas y colaboradores | Autenticación/autorización (no descrita en el requerimiento) |
| Integridad referencial y manejo de recursos inexistentes | Pruebas de seguridad (OWASP) y de accesibilidad |
| Contrato de la API: códigos de estado, estructura, tiempo de respuesta | Pruebas de compatibilidad entre navegadores |
| Carga sobre endpoints de consulta y de escritura | Pruebas de estrés hasta el punto de quiebre, de resistencia (soak) |

## 1.3 Reglas de negocio bajo prueba

- **Empresa:** geografía completa y coherente (país → departamento → municipio), NIT único, razón social, nombre comercial, teléfono y correo electrónico válido.
- **Colaborador:** nombre completo, edad válida, teléfono, correo válido y al menos una empresa. Puede pertenecer a una o varias empresas, sin repetir la misma.
- **Geografía:** un departamento pertenece a un país; un municipio pertenece a un departamento; no hay duplicados dentro del mismo padre.
- **Integridad:** no se elimina un registro con dependientes (409). Un recurso inexistente responde 404.

## 1.4 Supuestos (el requerimiento no los fija)

| # | Supuesto | Motivo |
|---|---|---|
| S1 | Edad válida: entero entre 1 y 120 | El enunciado solo dice "edad"; se necesita un rango para derivar valores límite. |
| S2 | Un colaborador debe tener al menos una empresa | "Poseen empresa… pueden pertenecer a una o varias". |
| S3 | Referencia a un recurso padre inexistente → 400; recurso solicitado inexistente → 404 | Convención REST habitual. |
| S4 | Alta correcta → 201 con cabecera `Location` | Convención REST habitual. |
| S5 | Umbrales de rendimiento: lectura p95 < 500 ms, escritura p95 < 800 ms | Ver `05-decisiones-y-rendimiento.md`. |

Los supuestos deben validarse con el responsable del producto; si alguno cambia, solo se ajusta la aserción correspondiente.

## 1.5 Niveles y tipos de prueba

| Nivel | Herramienta | Qué cubre | Ubicación |
|---|---|---|---|
| Unitario | xUnit | Funciones puras (validación de correo) | `tests/HrApi.Tests/ValidatorsTests.cs` |
| Integración / API | xUnit + `WebApplicationFactory` + EF Core (SQLite) | Endpoints, reglas de negocio, persistencia real, integridad referencial | `tests/HrApi.Tests/*Tests.cs` |
| Contrato / funcional exploratorio | Postman (+ newman) | Estado, estructura y tiempo de respuesta, ejecutable por cualquiera del equipo | `postman/` |
| Rendimiento | k6 | Carga de consulta y de escritura con umbrales | `performance/` |

## 1.6 Técnicas de diseño de casos

- **Partición de equivalencia:** correos válidos/inválidos, campos obligatorios presentes/ausentes.
- **Análisis de valores límite:** edad −1, 0, 1, 120, 121; tamaño de página.
- **Tablas de decisión:** coherencia geográfica (país, departamento y municipio coherentes o no).
- **Pruebas negativas:** duplicados, referencias inexistentes, eliminaciones con dependientes.
- **Pruebas de transiciones/relaciones:** colaborador con una, varias y repetidas empresas.

## 1.7 Pruebas con error (intencionales)

Con fines de validar el conocimiento sobre pruebas, la API de referencia incluye **10 defectos sembrados** (`DEF-001` … `DEF-010`, marcados con comentarios en el código). Las pruebas que los exponen:

- están etiquetadas con `[Trait("Defecto","DEF-00x")]` y `[Trait("Category","KnownDefect")]`;
- **fallan a propósito** porque afirman el comportamiento *correcto*;
- se documentan en `04-defectos.md` con pasos, resultado esperado/obtenido y evidencia.

Esto permite dos ejecuciones: la de regresión (sin `KnownDefect`, debe estar en verde) y la de defectos (solo `KnownDefect`, debe estar en rojo hasta que se corrijan).

## 1.8 Datos y entorno

- Cada clase de pruebas levanta la API en memoria con una **base SQLite temporal propia**; no hay dependencias entre clases ni orden de ejecución.
- Cada prueba **crea sus propias precondiciones** con nombres y NIT únicos (`Datos.cs`), por lo que se puede repetir y paralelizar entre clases.
- Postman y k6 apuntan a una instancia levantada con `dotnet run`; la URL base es una variable.

## 1.9 Criterios de entrada y salida

**Entrada:** API compila, `/health` responde, base creada.
**Salida (apta para producción):** 100 % de casos de prioridad alta pasados; sin defectos abiertos de severidad Alta; escenarios k6 de lectura y escritura en **PASA**; defectos restantes aceptados por el responsable.

## 1.10 Riesgos

| Riesgo | Mitigación |
|---|---|
| Reglas de negocio ambiguas (edad, códigos HTTP) | Supuestos S1–S5 explícitos y validados |
| SQLite tiene un único escritor: puede limitar la carga de escritura | Se mide en k6; si es un cuello de botella, se prueba contra el motor de producción |
| Datos acumulados degradan el listado de colaboradores | Escenario de lectura con volumen inicial parametrizable (`COLABORADORES`) |

## 1.11 Cómo ejecutar todo

```bash
dotnet test                                     # todo (los KnownDefect fallan a propósito)
dotnet test --filter "Category!=KnownDefect"    # regresión: debe estar en verde
dotnet test --filter "Category=KnownDefect"     # solo los casos que exponen defectos
dotnet test --logger "trx;LogFileName=resultado.trx"   # evidencia de ejecución

dotnet run --project src/HrApi --urls http://localhost:5000
newman run postman/HrApi.postman_collection.json --env-var baseUrl=http://localhost:5000 -r cli,json
k6 run performance/lectura.js
k6 run performance/escritura.js
```
