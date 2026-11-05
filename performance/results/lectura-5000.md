# Resultado k6 – lectura

Fecha de ejecución: 2026-11-05T04:00:08.001Z

**Veredicto global: PASA**

## Métricas

| Métrica | Valor |
|---|---|
| Solicitudes totales | 17655 |
| Solicitudes/s | 144.92 |
| Iteraciones | 3158 |
| Tiempo de respuesta promedio (ms) | 12.12 |
| p90 (ms) | 51.37 |
| p95 (ms) | 71.25 |
| p99 (ms) | 106.12 |
| Máximo (ms) | 261.57 |
| Tasa de error | 0.00 % |

## Umbrales

| Métrica | Umbral | Resultado |
|---|---|---|
| `checks` | `rate>0.99` | PASA |
| `http_req_duration{endpoint:colaboradores}` | `p(95)<500` | PASA |
| `http_req_duration{endpoint:paises}` | `p(95)<300` | PASA |
| `http_req_duration{endpoint:empresas}` | `p(95)<500` | PASA |
| `http_req_failed` | `rate<0.01` | PASA |
| `http_req_duration` | `p(99)<1000` | PASA |
| `http_req_duration` | `p(95)<500` | PASA |
