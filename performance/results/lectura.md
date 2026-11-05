# Resultado k6 – lectura

Fecha de ejecución: 2026-11-05T04:04:35.732Z

**Veredicto global: PASA**

## Métricas

| Métrica | Valor |
|---|---|
| Solicitudes totales | 13691 |
| Solicitudes/s | 113.26 |
| Iteraciones | 3167 |
| Tiempo de respuesta promedio (ms) | 14.78 |
| p90 (ms) | 57.69 |
| p95 (ms) | 70.20 |
| p99 (ms) | 112.85 |
| Máximo (ms) | 421.72 |
| Tasa de error | 0.00 % |

## Umbrales

| Métrica | Umbral | Resultado |
|---|---|---|
| `http_req_failed` | `rate<0.01` | PASA |
| `http_req_duration{endpoint:empresas}` | `p(95)<500` | PASA |
| `http_req_duration{endpoint:paises}` | `p(95)<300` | PASA |
| `http_req_duration{endpoint:colaboradores}` | `p(95)<500` | PASA |
| `http_req_duration` | `p(95)<500` | PASA |
| `http_req_duration` | `p(99)<1000` | PASA |
| `checks` | `rate>0.99` | PASA |
