# HR API – Evaluación técnica de calidad y pruebas

Estrategia de pruebas, automatización y evidencia para una API REST de gestión de **países, departamentos, municipios, empresas y colaboradores** (.NET 10, xUnit, Entity Framework, Postman y k6).

El reto pide demostrar, con evidencia objetiva y repetible, que la API cumple los requisitos y es estable bajo carga. Como no se entregó una API, este repositorio incluye una **API de referencia con 10 defectos sembrados** para que las pruebas tengan algo real que detectar y documentar.

## Contenido

| Ruta | Qué contiene |
|---|---|
| `src/HrApi` | API de referencia (minimal API + EF Core + SQLite). Defectos marcados como `DEF-00x` en el código |
| `tests/HrApi.Tests` | Pruebas xUnit unitarias y de integración (`WebApplicationFactory`) |
| `postman/` | Colección por entidad y operación (GET/POST/PUT/DELETE) y su archivo de entorno, con aserciones de estado, estructura y tiempo |
| `performance/` | Escenarios k6 de carga (consulta y escritura) con umbrales |
| `docs/01-estrategia-de-pruebas.md` | Alcance, niveles, técnicas, supuestos, criterios y riesgos |
| `docs/02-guion-de-pruebas.md` | Casos con objetivo, precondiciones, datos, pasos, esperado y obtenido |
| `docs/03-matriz-de-trazabilidad.md` | Requerimiento → caso de prueba → defecto |
| `docs/04-defectos.md` | Listado de defectos con pasos, esperado/obtenido y evidencia |
| `docs/05-decisiones-y-rendimiento.md` | Decisiones, elección de k6, umbrales y límites |

## Requisitos

.NET 10 SDK, Git, [k6](https://k6.io) y, opcionalmente, Node.js con `newman` para la colección de Postman.

## Ejecutar

```bash
dotnet test                                    # todas las pruebas (12 casos fallan a propósito: defectos)
dotnet test --filter "Category!=KnownDefect"   # regresión, debe pasar completa
dotnet test --filter "Category=KnownDefect"    # solo los que exponen defectos
dotnet test --filter "Defecto=DEF-003"         # un defecto concreto
dotnet test --filter "Req=RF-04"               # los casos de un requerimiento

dotnet run --project src/HrApi --urls http://localhost:5000
newman run postman/HrApi.postman_collection.json -e postman/HrApi.postman_environment.json
k6 run performance/lectura.js
k6 run performance/escritura.js
```

## Estado

- Código, pruebas, colección y scripts escritos y revisados, **pero todavía no ejecutados**: el entorno donde se redactaron no tenía el SDK de .NET.
- La columna *Resultado obtenido* del guion y los defectos son proyecciones por análisis del código; hay que confirmarlos con la primera ejecución y adjuntar la evidencia (`.trx`, reporte de newman, `performance/results/*.md`).
- Resultados esperados de `dotnet test`: ≈ 64 casos, 52 en verde y 12 en rojo (los de `KnownDefect`).

## Convención de commits

Un commit por entregable (API, pruebas, Postman, rendimiento, documentación) para que el historial cuente el trabajo.
