# 4. Listado de defectos

Defectos sembrados en la API de referencia para esta evaluación. Cada uno indica dónde se origina en el código y cómo reproducirlo.

> **Estado de la evidencia.** Los resultados "obtenidos" salen del análisis del código fuente (cada defecto está marcado con un comentario `DEF-00x`). Al ejecutar las pruebas, adjuntar en `docs/evidencia/` el `.trx` de xUnit y el reporte JSON de newman (comandos en `01-estrategia-de-pruebas.md`, sección 1.11) y capturar las respuestas de los `curl` indicados.

## Resumen

| ID | Título | Severidad | Prioridad | Módulo | Requerimiento | Caso |
|---|---|---|---|---|---|---|
| DEF-001 | Se permite registrar empresas con NIT duplicado | Alta | Alta | Empresas | RF-04 | TC-EMP-05 |
| DEF-002 | Se acepta edad 0 y negativa en colaboradores | Media | Media | Colaboradores | RF-05 | TC-COL-07 |
| DEF-003 | La empresa acepta geografía incoherente | Alta | Alta | Empresas | RF-04 | TC-EMP-07, 08 |
| DEF-004 | Eliminar un país con departamentos devuelve 500 | Alta | Alta | Países | RF-07 | TC-PAIS-07 |
| DEF-005 | No se valida el formato del correo del colaborador | Media | Media | Colaboradores | RF-05 | TC-COL-08 |
| DEF-006 | Consultar un colaborador inexistente devuelve 200 | Media | Media | Colaboradores | RF-08 | TC-COL-09 |
| DEF-007 | Se puede asociar al colaborador dos veces a la misma empresa | Media | Media | Colaboradores | RF-06 | TC-COL-11 |
| DEF-008 | El listado de colaboradores no pagina y filtra en memoria | Media | Alta (rendimiento) | Colaboradores | RNF-01 | TC-PERF-01 |
| DEF-009 | PUT sobre una empresa inexistente la crea | Alta | Alta | Empresas | RF-08 | TC-EMP-10 |
| DEF-010 | Crear empresa responde 200 en lugar de 201 | Baja | Baja | Empresas | RF-09 | TC-EMP-01 |

Severidad: **Alta** = datos incorrectos o error del servidor; **Media** = validación o contrato incumplido sin corrupción inmediata; **Baja** = desviación de convención.

---

## DEF-001 – NIT duplicado en empresas

- **Origen:** `EmpresasEndpoints.cs` (POST no consulta duplicados) y `AppDbContext.cs` (sin índice único sobre `Nit`).
- **Pasos:** crear una empresa con NIT `1234567-8`; crear otra con el mismo NIT.
- **Esperado:** 409 Conflict en la segunda.
- **Obtenido:** 200 OK; ambas quedan guardadas.
- **Reproducción:**
  ```bash
  curl -s -X POST localhost:5000/api/empresas -H 'Content-Type: application/json' \
    -d '{"nit":"1234567-8","razonSocial":"A","nombreComercial":"A","telefono":"1","email":"a@a.com","paisId":1,"departamentoId":1,"municipioId":1}'
  # repetir el mismo comando → debería ser 409
  ```
- **Evidencia:** xUnit `Crear_empresa_con_NIT_duplicado_devuelve_409`; Postman `TC-EMP-05`.
- **Corrección sugerida:** índice único en `Nit` y verificación previa que responda 409.

## DEF-002 – Edad 0 y negativa

- **Origen:** `ColaboradoresEndpoints.Validar` solo rechaza `Edad > 120`.
- **Pasos:** crear un colaborador con `edad=-1` (y luego `0`).
- **Esperado:** 400 Bad Request (supuesto S1: 1–120).
- **Obtenido:** 201 Created en ambos casos.
- **Evidencia:** xUnit `Crear_colaborador_con_edad_no_positiva_devuelve_400` (casos −1 y 0); Postman `TC-COL-07`.
- **Corrección sugerida:** validar `Edad is >= 1 and <= 120`.

## DEF-003 – Geografía incoherente en empresas

- **Origen:** `EmpresasEndpoints.Validar` comprueba que país, departamento y municipio existan, pero no que se pertenezcan entre sí.
- **Pasos:** crear dos geografías G1 y G2; registrar una empresa con país y departamento de G1 y el municipio de G2 (y, en otro caso, país de G1 con departamento y municipio de G2).
- **Esperado:** 400 Bad Request.
- **Obtenido:** 200 OK; la empresa queda con una ubicación imposible.
- **Evidencia:** xUnit `Crear_empresa_con_municipio_de_otro_departamento_devuelve_400` y `…departamento_de_otro_pais…`; Postman `TC-EMP-07` y `TC-EMP-08`.
- **Corrección sugerida:** verificar `Departamento.PaisId == PaisId` y `Municipio.DepartamentoId == DepartamentoId`.

## DEF-004 – Eliminar país con departamentos

- **Origen:** `GeografiaEndpoints.MapPaises` (DELETE no revisa dependientes); la FK `Restrict` lanza una excepción no controlada.
- **Pasos:** crear país → departamento; `DELETE /api/paises/{id}`.
- **Esperado:** 409 Conflict (como ya hace el DELETE de departamentos).
- **Obtenido:** 500 Internal Server Error por violación de clave foránea.
- **Reproducción:** `curl -i -X DELETE localhost:5000/api/paises/1`
- **Evidencia:** xUnit `Eliminar_pais_con_departamentos_devuelve_409`; Postman `TC-PAIS-07`.
- **Corrección sugerida:** `AnyAsync` sobre departamentos antes de borrar.

## DEF-005 – Correo del colaborador sin validar

- **Origen:** `ColaboradoresEndpoints.Validar` solo exige que no esté vacío (la empresa sí usa `Validators.Email`).
- **Pasos:** crear un colaborador con `email="sin-arroba"` y con `a@b`.
- **Esperado:** 400 Bad Request.
- **Obtenido:** 201 Created.
- **Evidencia:** xUnit `Crear_colaborador_con_correo_invalido_devuelve_400` (×2); Postman `TC-COL-08`.
- **Corrección sugerida:** reutilizar `Validators.Email`.

## DEF-006 – Colaborador inexistente devuelve 200

- **Origen:** `ColaboradoresEndpoints`, GET por id devuelve `Results.Ok(null)`.
- **Pasos:** `GET /api/colaboradores/999999`.
- **Esperado:** 404 Not Found.
- **Obtenido:** 200 OK con cuerpo vacío/`null`.
- **Reproducción:** `curl -i localhost:5000/api/colaboradores/999999`
- **Evidencia:** xUnit `Obtener_colaborador_inexistente_devuelve_404`; Postman `TC-COL-09`.
- **Corrección sugerida:** `c is null ? Results.NotFound() : Results.Ok(Map(c))`.

## DEF-007 – Asociación colaborador-empresa duplicada

- **Origen:** `POST /api/colaboradores/{id}/empresas/{empresaId}` no comprueba la relación existente; la tabla no tiene índice único `(ColaboradorId, EmpresaId)`.
- **Pasos:** crear un colaborador en la empresa E1; asociarlo otra vez a E1.
- **Esperado:** 409 Conflict.
- **Obtenido:** 200 OK; `empresaIds` devuelve E1 dos veces.
- **Evidencia:** xUnit `Asociar_colaborador_dos_veces_a_la_misma_empresa_devuelve_409`; Postman `TC-COL-11`.
- **Corrección sugerida:** verificar la relación y crear el índice único.

## DEF-008 – Listado de colaboradores sin paginación y con filtro en memoria

- **Origen:** `GET /api/colaboradores` carga toda la tabla con `Include` y filtra con LINQ to Objects; no hay paginación (a diferencia de empresas).
- **Pasos:** sembrar 1000+ colaboradores; ejecutar `performance/lectura.js` y observar `http_req_duration{endpoint:colaboradores}`.
- **Esperado:** p95 < 500 ms estable al crecer los datos.
- **Obtenido (hipótesis):** el tiempo crece linealmente con el volumen y puede incumplir el umbral. **No es un fallo funcional**; se confirma o descarta con k6.
- **Evidencia:** `performance/results/lectura.md` (se genera al ejecutar).
- **Corrección sugerida:** paginar y filtrar en la base de datos.

## DEF-009 – PUT crea la empresa si no existe

- **Origen:** `EmpresasEndpoints` PUT hace un *upsert* implícito.
- **Pasos:** `PUT /api/empresas/999999` con un cuerpo válido.
- **Esperado:** 404 Not Found sin crear nada.
- **Obtenido:** 200 OK y aparece un registro nuevo (con un id distinto del solicitado).
- **Evidencia:** xUnit `Actualizar_empresa_inexistente_devuelve_404`; Postman `TC-EMP-10`.
- **Corrección sugerida:** responder 404 cuando `FindAsync` devuelva null.

## DEF-010 – POST de empresa responde 200 en lugar de 201

- **Origen:** `EmpresasEndpoints` POST devuelve `Results.Ok` (países, departamentos, municipios y colaboradores sí devuelven 201).
- **Pasos:** crear una empresa válida.
- **Esperado:** 201 Created con cabecera `Location` (supuesto S4).
- **Obtenido:** 200 OK sin `Location`.
- **Evidencia:** xUnit `Crear_empresa_valida_devuelve_201_con_Location`; Postman `TC-EMP-01`.
- **Corrección sugerida:** `Results.Created($"/api/empresas/{e.Id}", e)`.
