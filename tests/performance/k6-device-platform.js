import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    inventory_filters: {
      executor: 'ramping-vus',
      startVUs: 5,
      stages: [
        { duration: '30s', target: 20 },
        { duration: '60s', target: 50 },
        { duration: '30s', target: 0 }
      ]
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<2000']
  }
};

const API_BASE_URL = __ENV.API_BASE_URL || 'https://localhost:7279';
const ACCESS_TOKEN = __ENV.ACCESS_TOKEN || '';

export default function () {
  const headers = ACCESS_TOKEN
    ? { Authorization: `Bearer ${ACCESS_TOKEN}` }
    : {};

  const environment = __ITER % 2 === 0 ? 'demo' : 'qa';

  const listResponse = http.get(`${API_BASE_URL}/api/devices`, { headers });
  check(listResponse, {
    'device list status is 200': (response) => response.status === 200
  });

  const dashboardResponse = http.get(`${API_BASE_URL}/api/devices/dashboard/${environment}`, { headers });
  check(dashboardResponse, {
    'dashboard status is 200': (response) => response.status === 200
  });

  sleep(1);
}
