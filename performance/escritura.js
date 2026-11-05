// Escenario de CARGA sobre endpoints de ESCRITURA (POST/PUT).
// Uso:  k6 run performance/escritura.js
//       k6 run -e BASE_URL=http://host:puerto performance/escritura.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { resumen } from './lib/resumen.js';

const BASE = __ENV.BASE_URL || 'http://localhost:5000';
const JSON_H = { headers: { 'Content-Type': 'application/json' } };

export const options = {
  scenarios: {
    escritura: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 10 },
        { duration: '1m', target: 25 },
        { duration: '30s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
  thresholds: {
    http_req_failed: ['rate<0.02'],                           // < 2 % de errores en escritura
    http_req_duration: ['p(95)<800', 'p(99)<1500'],
    'http_req_duration{endpoint:post_empresa}': ['p(95)<800'],
    'http_req_duration{endpoint:post_colaborador}': ['p(95)<800'],
    'http_req_duration{endpoint:put_empresa}': ['p(95)<800'],
    checks: ['rate>0.98'],
  },
};

const creado = (r) => r.status === 200 || r.status === 201; // DEF-010: la API responde 200 en lugar de 201

export function setup() {
  const sufijo = Date.now();
  const pais = http.post(`${BASE}/api/paises`, JSON.stringify({ nombre: `PaisW-${sufijo}` }), JSON_H).json();
  const dep = http.post(`${BASE}/api/departamentos`, JSON.stringify({ nombre: `DepW-${sufijo}`, paisId: pais.id }), JSON_H).json();
  const mun = http.post(`${BASE}/api/municipios`, JSON.stringify({ nombre: `MunW-${sufijo}`, departamentoId: dep.id }), JSON_H).json();
  return { paisId: pais.id, departamentoId: dep.id, municipioId: mun.id };
}

export default function (g) {
  const nit = `${String(__VU).padStart(3, '0')}${String(__ITER).padStart(6, '0')}-${Date.now() % 10}`;
  const empresa = {
    nit, razonSocial: `Empresa Carga ${nit}, S.A.`, nombreComercial: `Carga ${nit}`, telefono: '22334455',
    email: `carga-${nit}@empresa.com`, paisId: g.paisId, departamentoId: g.departamentoId, municipioId: g.municipioId,
  };

  const e = http.post(`${BASE}/api/empresas`, JSON.stringify(empresa), { ...JSON_H, tags: { endpoint: 'post_empresa' } });
  const okE = check(e, { 'POST empresa 200/201': creado });
  if (!okE) { sleep(1); return; }
  const empresaId = e.json('id');

  const c = http.post(`${BASE}/api/colaboradores`, JSON.stringify({
    nombreCompleto: `Colaborador Carga ${nit}`, edad: 25 + (__ITER % 30), telefono: '55667788',
    email: `col-${nit}@correo.com`, empresaIds: [empresaId],
  }), { ...JSON_H, tags: { endpoint: 'post_colaborador' } });
  check(c, { 'POST colaborador 201': (r) => r.status === 201 });

  const u = http.put(`${BASE}/api/empresas/${empresaId}`, JSON.stringify({ ...empresa, nombreComercial: `Editada ${nit}` }),
    { ...JSON_H, tags: { endpoint: 'put_empresa' } });
  check(u, { 'PUT empresa 200': (r) => r.status === 200 });

  sleep(1);
}

export function handleSummary(data) {
  return resumen('escritura', data);
}
