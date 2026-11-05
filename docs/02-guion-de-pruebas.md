# 2. Guion de pruebas

Cada caso indica objetivo, precondiciones, datos de entrada, pasos, resultado esperado y resultado obtenido. El identificador `TC-…` coincide con el atributo `[Trait("Caso", …)]` de la prueba xUnit y con el nombre de la petición de Postman.

> **Estado de ejecución.** La columna *Resultado obtenido* es una **proyección por análisis del código** de la API de referencia (el SDK de .NET no estaba disponible donde se redactó el repositorio). Debe confirmarse con la primera ejecución de `dotnet test` y reemplazarse por el resultado real y la evidencia (`.trx`, reporte de newman). `OK` = coincide con lo esperado; `FALLA DEF-xxx` = difiere y origina el defecto indicado.

Convenciones: `{…}` = valor generado por la prueba (nombres y NIT únicos). *Pre-geo* = existe un país, un departamento de ese país y un municipio de ese departamento.

## 2.1 Países

| ID | Objetivo | Precondiciones | Datos de entrada | Pasos | Resultado esperado | Resultado obtenido |
|---|---|---|---|---|---|---|
| TC-PAIS-01 | Crear país válido | Ninguna | `nombre={único}` | POST `/api/paises` | 201, cabecera `Location`, cuerpo con `id>0` y `nombre` | OK |
| TC-PAIS-02 | Rechazar país sin nombre | Ninguna | `nombre` = null, "", "   " | POST `/api/paises` | 400 | OK |
| TC-PAIS-03 | Rechazar país duplicado (sin distinguir mayúsculas) | País "X" existe | `nombre="X"` en mayúsculas | POST `/api/paises` | 409 | OK |
| TC-PAIS-04 | País inexistente | Ninguna | id 999999 | GET `/api/paises/999999` | 404 | OK |
| TC-PAIS-05 | Actualizar país | País existe | `nombre` nuevo | PUT `/api/paises/{id}` | 200, nombre actualizado | OK |
| TC-PAIS-06 | Eliminar país sin dependientes | País sin departamentos | id del país | DELETE `/api/paises/{id}` | 204 | OK |
| TC-PAIS-07 | No eliminar país con departamentos | Pre-geo | id del país | DELETE `/api/paises/{id}` | 409 | **FALLA DEF-004** (500) |

## 2.2 Departamentos

| ID | Objetivo | Precondiciones | Datos de entrada | Pasos | Resultado esperado | Resultado obtenido |
|---|---|---|---|---|---|---|
| TC-DEP-01 | Crear departamento válido | País existe | `nombre={único}`, `paisId` | POST `/api/departamentos` | 201, `paisId` correcto | OK |
| TC-DEP-02 | País inexistente | Ninguna | `paisId=999999` | POST `/api/departamentos` | 400 | OK |
| TC-DEP-03 | Duplicado en el mismo país | Departamento "D" en país P | Mismo nombre y país | POST `/api/departamentos` | 409 | OK |
| TC-DEP-04 | Mismo nombre en países distintos es válido | "D" en país P1 | "D" en país P2 | POST `/api/departamentos` | 201 | OK |
| TC-DEP-05 | No eliminar departamento con municipios | Pre-geo | id del departamento | DELETE `/api/departamentos/{id}` | 409 | OK |

## 2.3 Municipios

| ID | Objetivo | Precondiciones | Datos de entrada | Pasos | Resultado esperado | Resultado obtenido |
|---|---|---|---|---|---|---|
| TC-MUN-01 | Crear municipio válido | Departamento existe | `nombre`, `departamentoId` | POST `/api/municipios` | 201 | OK |
| TC-MUN-02 | Departamento inexistente | Ninguna | `departamentoId=999999` | POST `/api/municipios` | 400 | OK |
| TC-MUN-03 | Duplicado en el mismo departamento | Municipio "M" en depto D | Mismo nombre y depto | POST `/api/municipios` | 409 | OK |
| TC-MUN-04 | Filtrar por departamento | Pre-geo | `departamentoId` | GET `/api/municipios?departamentoId={id}` | 200, exactamente 1 municipio | OK |

## 2.4 Empresas

| ID | Objetivo | Precondiciones | Datos de entrada | Pasos | Resultado esperado | Resultado obtenido |
|---|---|---|---|---|---|---|
| TC-EMP-01 | Crear empresa válida | Pre-geo | Empresa completa, NIT único | POST `/api/empresas` | 201 y cabecera `Location` | **FALLA DEF-010** (200, sin `Location`) |
| TC-EMP-02 | Estructura de la respuesta | Pre-geo | Empresa completa | POST `/api/empresas` | Cuerpo con `id, nit, razonSocial, nombreComercial, telefono, email, paisId, departamentoId, municipioId` | OK |
| TC-EMP-03 | Campos obligatorios | Pre-geo | NIT / razón social / nombre comercial / teléfono vacío (uno por vez) | POST `/api/empresas` | 400 | OK |
| TC-EMP-04 | Correo inválido | Pre-geo | `sin-arroba`, `a@b`, "" | POST `/api/empresas` | 400 | OK |
| TC-EMP-05 | NIT duplicado | Empresa con NIT N existe | Otra empresa con NIT N | POST `/api/empresas` | 409 | **FALLA DEF-001** (200, se crea) |
| TC-EMP-06 | País inexistente | Pre-geo | `paisId=999999` | POST `/api/empresas` | 400 | OK |
| TC-EMP-07 | Municipio de otro departamento | Dos geografías (G1, G2) | País y depto de G1, municipio de G2 | POST `/api/empresas` | 400 | **FALLA DEF-003** (200) |
| TC-EMP-08 | Departamento de otro país | Dos geografías | País de G1, depto y municipio de G2 | POST `/api/empresas` | 400 | **FALLA DEF-003** (200) |
| TC-EMP-09 | Empresa inexistente | Ninguna | id 999999 | GET `/api/empresas/999999` | 404 | OK |
| TC-EMP-10 | Actualizar empresa inexistente | Pre-geo | id 999999, datos válidos | PUT `/api/empresas/999999` | 404 | **FALLA DEF-009** (200, crea registro) |
| TC-EMP-11 | Actualizar empresa existente | Empresa existe | Razón social y teléfono nuevos | PUT `/api/empresas/{id}` | 200, datos actualizados, mismo `id` | OK |
| TC-EMP-12 | No eliminar empresa con colaboradores | Empresa con un colaborador | id de la empresa | DELETE `/api/empresas/{id}` | 409 | OK |
| TC-EMP-13 | Listado paginado y tiempo | Al menos 1 empresa | `page=1&size=10` | GET `/api/empresas?page=1&size=10` | 200, `page, size, total, items[]`, < 500 ms | OK |

## 2.5 Colaboradores

| ID | Objetivo | Precondiciones | Datos de entrada | Pasos | Resultado esperado | Resultado obtenido |
|---|---|---|---|---|---|---|
| TC-COL-01 | Crear colaborador en una empresa | Empresa E1 | Datos válidos, `empresaIds=[E1]` | POST `/api/colaboradores` | 201, estructura completa, `empresaIds=[E1]` | OK |
| TC-COL-02 | Colaborador en varias empresas | Empresas E1, E2 | `empresaIds=[E1,E2]` | POST `/api/colaboradores` | 201, 2 empresas | OK |
| TC-COL-03 | Sin empresa | Ninguna | `empresaIds=[]` | POST `/api/colaboradores` | 400 | OK |
| TC-COL-04 | Empresa inexistente | Ninguna | `empresaIds=[999999]` | POST `/api/colaboradores` | 400 | OK |
| TC-COL-05 | Campos obligatorios | Empresa E1 | Nombre / teléfono / correo vacío (uno por vez) | POST `/api/colaboradores` | 400 | OK |
| TC-COL-06 | Edad sobre el máximo | Empresa E1 | `edad=121` | POST `/api/colaboradores` | 400 | OK |
| TC-COL-07 | Edad no positiva | Empresa E1 | `edad=-1` y `edad=0` | POST `/api/colaboradores` | 400 | **FALLA DEF-002** (201 en ambos) |
| TC-COL-08 | Correo inválido | Empresa E1 | `sin-arroba`, `a@b` | POST `/api/colaboradores` | 400 | **FALLA DEF-005** (201 en ambos) |
| TC-COL-09 | Colaborador inexistente | Ninguna | id 999999 | GET `/api/colaboradores/999999` | 404 | **FALLA DEF-006** (200, cuerpo nulo) |
| TC-COL-10 | Asociar a empresa adicional | Colaborador en E1, empresa E2 | E2 | POST `/api/colaboradores/{id}/empresas/{E2}` | 200, 2 empresas | OK |
| TC-COL-11 | No repetir asociación | Colaborador en E1 | E1 otra vez | POST `/api/colaboradores/{id}/empresas/{E1}` | 409 | **FALLA DEF-007** (200, relación duplicada) |
| TC-COL-12 | Deduplicar empresas en el cuerpo | Empresa E1 | `empresaIds=[E1,E1]` | POST `/api/colaboradores` | 201, 1 empresa | OK |
| TC-COL-13 | Actualizar colaborador | Colaborador en E1, empresa E2 | `edad=41`, `empresaIds=[E2]` | PUT `/api/colaboradores/{id}` | 200, edad 41, empresas = [E2] | OK |
| TC-COL-14 | Actualizar/eliminar inexistente | Empresa E1 | id 999999 | PUT y DELETE | 404 en ambos | OK |
| TC-COL-15 | Eliminar colaborador libera la empresa | Colaborador en E1 | — | DELETE colaborador, luego DELETE empresa | 204 y 204 | OK |
| TC-COL-16 | Buscar por nombre | Colaborador con marca única | `nombre={marca}` | GET `/api/colaboradores?nombre={marca}` | 200, 1 resultado | OK |

## 2.6 Unitarias

| ID | Objetivo | Datos de entrada | Resultado esperado | Resultado obtenido |
|---|---|---|---|---|
| TC-UNI-01 | Correo válido | `persona@correo.com`, `a.b-c@dominio.gt` | `true` | OK |
| TC-UNI-02 | Correo inválido | null, "", "   ", `sin-arroba`, `a@b`, `Nombre <a@b.com>` | `false` | OK |

## 2.7 Rendimiento

| ID | Objetivo | Precondiciones | Escenario | Resultado esperado | Resultado obtenido |
|---|---|---|---|---|---|
| TC-PERF-01 | Estabilidad de **consulta** bajo carga | API arriba; 20 empresas y 1000 colaboradores sembrados | `performance/lectura.js`: 0→25→50 VUs durante 2 min | Error < 1 %, p95 < 500 ms, p99 < 1 s | Pendiente de ejecutar. Hipótesis: `colaboradores` puede superar el umbral por DEF-008 |
| TC-PERF-02 | Estabilidad de **escritura** bajo carga | API arriba | `performance/escritura.js`: 0→10→25 VUs durante 2 min (POST empresa, POST colaborador, PUT empresa) | Error < 2 %, p95 < 800 ms, p99 < 1.5 s | Pendiente de ejecutar. Riesgo: bloqueos de escritura de SQLite |

## 2.8 Contrato en Postman

Las peticiones de `postman/HrApi.postman_collection.json` repiten los casos clave (nombre con el mismo `TC-…`) y añaden en cada una la aserción de **código de estado**, **estructura** (cuando aplica) y **tiempo de respuesta < 500 ms**. Las peticiones con `[DEF-xxx]` en el nombre se espera que fallen.
