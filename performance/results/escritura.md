# Resultado k6 – escritura

Fecha de ejecución: 2026-11-05T04:02:16.441Z

**Veredicto global: PASA**

## Métricas

| Métrica | Valor |
|---|---|
| Solicitudes totales | 4629 |
| Solicitudes/s | 38.35 |
| Iteraciones | 1542 |
| Tiempo de respuesta promedio (ms) | 2.98 |
| p90 (ms) | 2.89 |
| p95 (ms) | 3.21 |
| p99 (ms) | 5.70 |
| Máximo (ms) | 160.02 |
| Tasa de error | 0.00 % |

## Umbrales

| Métrica | Umbral | Resultado |
|---|---|---|
| `http_req_duration` | `p(99)<1500` | PASA |
| `http_req_duration` | `p(95)<800` | PASA |
| `http_req_duration{endpoint:put_empresa}` | `p(95)<800` | PASA |
| `http_req_duration{endpoint:post_empresa}` | `p(95)<800` | PASA |
| `http_req_failed` | `rate<0.02` | PASA |
| `http_req_duration{endpoint:post_colaborador}` | `p(95)<800` | PASA |
| `checks` | `rate>0.98` | PASA |
