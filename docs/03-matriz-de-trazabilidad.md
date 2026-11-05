# 3. Matriz de trazabilidad

Cadena completa: **requerimiento → caso de prueba → defecto**. Los casos están automatizados en xUnit (clase y método en `tests/HrApi.Tests/`) y repetidos en Postman cuando se indica.

## 3.1 Requerimientos

| ID | Requerimiento (criterio funcional) | Fuente |
|---|---|---|
| RF-01 | Mantenimiento de países (crear, consultar, actualizar, eliminar; sin duplicados) | Solicitud |
| RF-02 | Mantenimiento de departamentos; pertenecen a un país; sin duplicados en el país | Solicitud |
| RF-03 | Mantenimiento de municipios; pertenecen a un departamento; sin duplicados en el departamento | Solicitud |
| RF-04 | Empresas con geografía coherente, NIT único, razón social, nombre comercial, teléfono y correo válido | Reglas de negocio |
| RF-05 | Colaboradores con empresa, nombre completo, edad válida, teléfono y correo válido | Reglas de negocio |
| RF-06 | Un colaborador puede pertenecer a una o varias empresas (sin repetir) | Reglas de negocio |
| RF-07 | Integridad referencial: no se elimina un registro con dependientes | Supuesto S3 |
| RF-08 | Un recurso inexistente responde 404 | Supuesto S3 |
| RF-09 | Contrato: códigos de estado, estructura de respuesta y tiempo de respuesta | El reto |
| RNF-01 | Estabilidad bajo carga en endpoints de consulta | El reto |
| RNF-02 | Estabilidad bajo carga en endpoints de escritura | El reto |

## 3.2 Requerimiento → casos → defectos

| Requerimiento | Casos de prueba | Defectos originados |
|---|---|---|
| RF-01 | TC-PAIS-01, 02, 03, 05, 06 | — |
| RF-02 | TC-DEP-01, 02, 03, 04 | — |
| RF-03 | TC-MUN-01, 02, 03, 04 | — |
| RF-04 | TC-EMP-01, 02, 03, 04, 05, 06, 07, 08, 11, TC-UNI-01, 02 | DEF-001, DEF-003, DEF-010 |
| RF-05 | TC-COL-01, 03, 04, 05, 06, 07, 08, 13, 15 | DEF-002, DEF-005 |
| RF-06 | TC-COL-02, 10, 11, 12 | DEF-007 |
| RF-07 | TC-PAIS-07, TC-DEP-05, TC-EMP-12 | DEF-004 |
| RF-08 | TC-PAIS-04, TC-EMP-09, 10, TC-COL-09, 14 | DEF-006, DEF-009 |
| RF-09 | TC-EMP-02, 13, TC-COL-01, TC-COL-16 y aserciones de Postman | DEF-010 |
| RNF-01 | TC-PERF-01 | DEF-008 (hipótesis) |
| RNF-02 | TC-PERF-02 | — (riesgo: bloqueos de SQLite) |

## 3.3 Defecto → caso → requerimiento (vista inversa)

| Defecto | Caso que lo detecta | Prueba xUnit | Petición Postman | Requerimiento |
|---|---|---|---|---|
| DEF-001 | TC-EMP-05 | `EmpresasTests.Crear_empresa_con_NIT_duplicado_devuelve_409` | TC-EMP-05 | RF-04 |
| DEF-002 | TC-COL-07 | `ColaboradoresTests.Crear_colaborador_con_edad_no_positiva_devuelve_400` (×2) | TC-COL-07 | RF-05 |
| DEF-003 | TC-EMP-07, TC-EMP-08 | `EmpresasTests.Crear_empresa_con_municipio_de_otro_departamento_devuelve_400`, `…departamento_de_otro_pais…` | TC-EMP-07, TC-EMP-08 | RF-04 |
| DEF-004 | TC-PAIS-07 | `GeografiaTests.Eliminar_pais_con_departamentos_devuelve_409` | TC-PAIS-07 | RF-07 |
| DEF-005 | TC-COL-08 | `ColaboradoresTests.Crear_colaborador_con_correo_invalido_devuelve_400` (×2) | TC-COL-08 | RF-05 |
| DEF-006 | TC-COL-09 | `ColaboradoresTests.Obtener_colaborador_inexistente_devuelve_404` | TC-COL-09 | RF-08 |
| DEF-007 | TC-COL-11 | `ColaboradoresTests.Asociar_colaborador_dos_veces_a_la_misma_empresa_devuelve_409` | TC-COL-11 | RF-06 |
| DEF-008 | TC-PERF-01 | `performance/lectura.js` (umbral `endpoint:colaboradores`) | — | RNF-01 |
| DEF-009 | TC-EMP-10 | `EmpresasTests.Actualizar_empresa_inexistente_devuelve_404` | TC-EMP-10 | RF-08 |
| DEF-010 | TC-EMP-01 | `EmpresasTests.Crear_empresa_valida_devuelve_201_con_Location` | TC-EMP-01 | RF-09 |

## 3.4 Cobertura

- 11 de 11 requerimientos tienen al menos un caso.
- 10 defectos documentados, cada uno con un caso que lo detecta y un requerimiento al que afecta.
- Para listar solo las pruebas que apuntan a un defecto: `dotnet test --filter "Defecto=DEF-003"`.
- Para ejecutar los casos de un requerimiento: `dotnet test --filter "Req=RF-04"`.
