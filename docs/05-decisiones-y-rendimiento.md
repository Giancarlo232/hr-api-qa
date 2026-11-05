# 5. Decisiones y pruebas de rendimiento

## 5.1 Cómo se abordó el problema

1. **Entender qué se pide.** El reto no es solo encontrar defectos: es dejar evidencia repetible y trazable. Eso condiciona todo: pruebas automatizadas, datos propios por prueba, trazabilidad requerimiento → caso → defecto y un repositorio con historial.
2. **No había API que probar**, así que se construyó una API de referencia (.NET 10 + EF Core) con las cinco entidades y las reglas del enunciado, e **intencionalmente 10 defectos** marcados en el código. Así el ejercicio demuestra el ciclo completo: diseñar el caso, detectar el fallo y documentarlo.
3. **Pirámide de pruebas.** Unitarias para lógica pura, integración con la API real y base de datos real para las reglas de negocio (donde está el riesgo), Postman como contrato ejecutable por cualquier miembro del equipo y k6 para carga.
4. **Aserciones sobre el comportamiento correcto**, no sobre el actual. Los defectos aparecen como pruebas en rojo etiquetadas (`KnownDefect`); al corregirlos, las mismas pruebas pasan sin modificarse.

## 5.2 Decisiones técnicas

| Decisión | Alternativas | Motivo |
|---|---|---|
| `WebApplicationFactory` + SQLite en archivo temporal | EF InMemory; Testcontainers | InMemory no aplica claves foráneas (ocultaría DEF-004); Testcontainers requiere Docker. SQLite real aplica FK y es portable. |
| Una base y una API por clase de pruebas | Una global; una por prueba | Aislamiento suficiente y arranque rápido; cada prueba crea datos únicos. |
| Datos únicos por prueba (`Guid`, NIT aleatorio) | Limpiar la base entre pruebas | Permite repetir y ejecutar sin orden y sin depender de limpieza. |
| Rasgos `Caso`, `Req`, `Defecto`, `Category` | Solo nombres de prueba | Permite filtrar por requerimiento o defecto y ejecutar la regresión "verde". |
| Postman con variables de colección y scripts | Solo pruebas manuales | Trazable por nombre (`TC-…`) y ejecutable con newman en CI. |
| **k6** como framework de rendimiento | JMeter, NBomber, Locust | Ver 5.3. |

## 5.3 Por qué k6

- Los escenarios son **código JavaScript versionable** y se revisan en un *pull request*, a diferencia de los `.jmx` (XML).
- **Umbrales declarativos** (`thresholds`) que hacen que la ejecución termine en PASA/NO PASA sin interpretar gráficas: exactamente la evidencia que pide el reto.
- Binario único, bajo consumo de recursos y fácil de integrar en CI.
- Etiquetas por endpoint que permiten un umbral por endpoint (en particular para DEF-008).
- Contra: no tiene interfaz gráfica de diseño ni soporta protocolos exóticos; no es necesario aquí.

## 5.4 Escenarios y criterios de aceptación

| | Consulta (`performance/lectura.js`) | Escritura (`performance/escritura.js`) |
|---|---|---|
| Endpoints | GET países, GET empresas paginado, GET empresa por id, GET colaboradores con filtro | POST empresa, POST colaborador, PUT empresa |
| Perfil de carga | 0→25 VUs en 30 s, →50 VUs en 1 min, →0 en 30 s | 0→10 VUs en 30 s, →25 VUs en 1 min, →0 en 30 s |
| Pausa por iteración | 1 s | 1 s |
| Datos iniciales | 20 empresas, 1000 colaboradores (`-e COLABORADORES=…`) | País/departamento/municipio creados en `setup` |
| Umbral de errores | < 1 % | < 2 % |
| Tiempo de respuesta | p95 < 500 ms, p99 < 1 s | p95 < 800 ms, p99 < 1.5 s |
| Umbrales por endpoint | países p95 < 300 ms; empresas y colaboradores p95 < 500 ms | post_empresa, post_colaborador y put_empresa p95 < 800 ms |
| Checks funcionales | estado 200 y estructura | 200/201 (DEF-010) y 201/200 |

**Justificación de los umbrales.** Para consultas interactivas de una pantalla de RR. HH. se considera aceptable p95 < 500 ms; las escrituras admiten más margen por la transacción. La tasa de error es más estricta en lectura porque no tiene motivos de rechazo. Los valores son un acuerdo de partida, a ajustar con el responsable del producto y el entorno real de despliegue.

## 5.5 Cómo evaluar el resultado

Cada ejecución escribe `performance/results/<escenario>.md` con las métricas (solicitudes/s, promedio, p90, p95, p99, máximo, tasa de error) y una tabla de umbrales con **PASA / NO PASA** y un veredicto global. El JSON crudo se guarda junto al reporte (ignorado en git).

## 5.6 Hipótesis confirmadas/descartadas con la ejecución real

| Hipótesis | Cómo se confirmó | Resultado |
|---|---|---|
| DEF-008: el tiempo de `GET /api/colaboradores` crece con el volumen y puede incumplir p95 < 500 ms | `k6 run performance/lectura.js` (volumen acumulado ~3 300–13 000 colaboradores entre corridas) y `k6 run -e COLABORADORES=5000 performance/lectura.js` | **Parcialmente confirmada.** El p95 del endpoint sube de forma medible con el volumen (≈ 32 ms → ≈ 70 ms), consistente con el filtro en memoria sin paginar de DEF-008. A este volumen (decenas de miles de filas) el umbral de 500 ms sigue en PASA; la pendiente observada indica que con datos de producción (cientos de miles de filas) sí lo incumpliría. Ver `performance/results/lectura.md` y `lectura-5000.md`. |
| SQLite (un único escritor) puede producir errores de bloqueo en escritura con 25 VUs | `k6 run performance/escritura.js` (0→10→25 VUs, POST empresa + POST colaborador + PUT empresa por iteración, 2 min) | **Descartada a esta concurrencia.** 0 % de error, p95 ≈ 3.2 ms, p99 ≈ 5.7 ms; no se observaron 5xx ni timeouts atribuibles a bloqueos de SQLite. No se puede descartar a mayor concurrencia o con transacciones más largas; sería necesario repetir contra un motor cliente-servidor para dimensionar producción. Ver `performance/results/escritura.md`. |

Durante esta ejecución se detectó y corrigió un bug en el propio script `performance/lectura.js` (no en la API): la URL de `GET /api/colaboradores?nombre=...` llevaba un espacio sin codificar y el servidor respondía `400` en cada iteración, inflando la tasa de error a ~23 %. Se corrigió con `encodeURIComponent`; no se tocó ningún defecto sembrado.

## 5.7 Limitaciones

- La ejecución se hizo en una sola máquina de desarrollo (macOS, .NET SDK 10.0.403); no hay aislamiento de red ni recursos dedicados, por lo que los tiempos absolutos sirven para comparar entre corridas, no para dimensionar producción.
- La carga contra SQLite local no equivale a la de producción: sirve para comparar versiones, no para dimensionar.
- La base de datos fue acumulando filas entre corridas de k6 (no se reinició entre ejecuciones), lo que explica por qué la corrida "default" ya partía de ~3 000–8 000 colaboradores en los distintos intentos; esto es consistente con el objetivo de observar degradación por volumen, pero significa que el número exacto de filas en cada corrida no está fijado de antemano.
- No se cubre seguridad ni autenticación porque el enunciado no las describe.
