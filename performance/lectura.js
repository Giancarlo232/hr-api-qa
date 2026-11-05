// Escenario de CARGA sobre endpoints de CONSULTA.
// Uso:  k6 run performance/lectura.js            (API en http://localhost:5000)
//       k6 run -e BASE_URL=http://host:puerto -e COLABORADORES=2000 performance/lectura.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { resumen } from './lib/resumen.js';

const BASE = __ENV.BASE_URL || 'http://localhost:5000';
const EMPRESAS = parseInt(__ENV.EMPRESAS || '20');
const COLABORADORES = parseInt(__ENV.COLABORADORES || '1000');
const JSON_H = { headers: { 'Content-Type': 'application/json' } };

export const options = {
  scenarios: {
    lectura: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 25 },
        { duration: '1m', target: 50 },
        { duration: '30s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
  // Criterios de aceptación acordados (ver docs/05-decisiones-y-rendimiento.md)
  thresholds: {
    http_req_failed: ['rate<0.01'],                          // < 1 % de errores
    http_req_duration: ['p(95)<500', 'p(99)<1000'],          // p95 < 500 ms, p99 < 1 s
    'http_req_duration{endpoint:paises}': ['p(95)<300'],
    'http_req_duration{endpoint:empresas}': ['p(95)<500'],
    'http_req_duration{endpoint:colaboradores}': ['p(95)<500'],  // sensible a DEF-008
    checks: ['rate>0.99'],
  },
};

const ok = (r) => r.status === 200;

export function setup() {
  const sufijo = Date.now();
  const pais = http.post(`${BASE}/api/paises`, JSON.stringify({ nombre: `PaisK6-${sufijo}` }), JSON_H).json();
  const dep = http.post(`${BASE}/api/departamentos`, JSON.stringify({ nombre: `DepK6-${sufijo}`, paisId: pais.id }), JSON_H).json();
  const mun = http.post(`${BASE}/api/municipios`, JSON.stringify({ nombre: `MunK6-${sufijo}`, departamentoId: dep.id }), JSON_H).json();

  const empresaIds = [];
  for (let i = 0; i < EMPRESAS; i++) {
    const r = http.post(`${BASE}/api/empresas`, JSON.stringify({
      nit: `${sufijo % 100000}${i}-1`, razonSocial: `Empresa K6 ${i}, S.A.`, nombreComercial: `K6 ${i}`,
      telefono: '22334455', email: `k6-${i}@empresa.com`, paisId: pais.id, departamentoId: dep.id, municipioId: mun.id,
    }), JSON_H);
    empresaIds.push(r.json().id);
  }
  for (let i = 0; i < COLABORADORES; i++) {
    http.post(`${BASE}/api/colaboradores`, JSON.stringify({
      nombreCompleto: `Colaborador K6 ${i}`, edad: 20 + (i % 40), telefono: '55667788',
      email: `col${i}@correo.com`, empresaIds: [empresaIds[i % empresaIds.length]],
    }), JSON_H);
  }
  return { empresaId: empresaIds[0] };
}

export default function (datos) {
  const a = http.get(`${BASE}/api/paises`, { tags: { endpoint: 'paises' } });
  check(a, { 'paises 200': ok });

  const b = http.get(`${BASE}/api/empresas?page=1&size=20`, { tags: { endpoint: 'empresas' } });
  check(b, { 'empresas 200': ok, 'empresas con items': (r) => Array.isArray(r.json('items')) });

  const c = http.get(`${BASE}/api/empresas/${datos.empresaId}`, { tags: { endpoint: 'empresa_id' } });
  check(c, { 'empresa por id 200': ok });

  const d = http.get(`${BASE}/api/colaboradores?nombre=Colaborador K6 1`, { tags: { endpoint: 'colaboradores' } });
  check(d, { 'colaboradores 200': ok });

  sleep(1);
}

export function handleSummary(data) {
  return resumen('lectura', data);
}
