// Genera un reporte legible (Markdown + JSON crudo) a partir del resumen de k6.
// Cada umbral queda como PASA / NO PASA, que es la evidencia pedida por el reto.

const fmt = (v) => (v === undefined || v === null ? '-' : Number(v).toFixed(2));

export function resumen(nombre, data) {
  const filas = [];
  let todosPasan = true;

  for (const [metrica, m] of Object.entries(data.metrics)) {
    if (!m.thresholds) continue;
    for (const [expresion, r] of Object.entries(m.thresholds)) {
      if (!r.ok) todosPasan = false;
      filas.push(`| \`${metrica}\` | \`${expresion}\` | ${r.ok ? 'PASA' : '**NO PASA**'} |`);
    }
  }

  const d = data.metrics.http_req_duration?.values ?? {};
  const f = data.metrics.http_req_failed?.values ?? {};
  const it = data.metrics.iterations?.values ?? {};
  const reqs = data.metrics.http_reqs?.values ?? {};

  const md = [
    `# Resultado k6 – ${nombre}`,
    '',
    `Fecha de ejecución: ${new Date().toISOString()}`,
    '',
    `**Veredicto global: ${todosPasan ? 'PASA' : 'NO PASA'}**`,
    '',
    '## Métricas',
    '',
    '| Métrica | Valor |',
    '|---|---|',
    `| Solicitudes totales | ${reqs.count ?? '-'} |`,
    `| Solicitudes/s | ${fmt(reqs.rate)} |`,
    `| Iteraciones | ${it.count ?? '-'} |`,
    `| Tiempo de respuesta promedio (ms) | ${fmt(d.avg)} |`,
    `| p90 (ms) | ${fmt(d['p(90)'])} |`,
    `| p95 (ms) | ${fmt(d['p(95)'])} |`,
    `| p99 (ms) | ${fmt(d['p(99)'])} |`,
    `| Máximo (ms) | ${fmt(d.max)} |`,
    `| Tasa de error | ${fmt((f.rate ?? 0) * 100)} % |`,
    '',
    '## Umbrales',
    '',
    '| Métrica | Umbral | Resultado |',
    '|---|---|---|',
    ...filas,
    '',
  ].join('\n');

  return {
    [`performance/results/${nombre}.md`]: md,
    [`performance/results/${nombre}.json`]: JSON.stringify(data, null, 2),
    stdout: `\n${md}\n`,
  };
}
