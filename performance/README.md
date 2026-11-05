# Pruebas de rendimiento (k6)

Framework elegido: **k6** (justificación en `docs/05-decisiones-y-rendimiento.md`).

| Script | Escenario | Umbrales |
|---|---|---|
| `lectura.js` | Carga de consulta, hasta 50 VUs | error < 1 %, p95 < 500 ms, p99 < 1 s |
| `escritura.js` | Carga de escritura, hasta 25 VUs | error < 2 %, p95 < 800 ms, p99 < 1.5 s |

## Ejecutar

```bash
# 1) levantar la API
dotnet run --project src/HrApi --urls http://localhost:5000

# 2) desde la raíz del repositorio (las rutas del reporte son relativas a ella)
k6 run performance/lectura.js
k6 run performance/escritura.js

# variantes
k6 run -e BASE_URL=http://otro-host:5000 -e COLABORADORES=5000 performance/lectura.js
```

Al terminar se generan `performance/results/lectura.md` y `performance/results/escritura.md` con las métricas y el resultado **PASA / NO PASA** de cada umbral. k6 también termina con código distinto de cero si algún umbral falla, útil en CI.

> Los reportes de `performance/results/` todavía no existen: se generan en la primera ejecución.
