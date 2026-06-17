import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 100,
  duration: '10s',
};

export default function () {
  const res = http.get('http://localhost:5162/api/stress-test/inventory-with-lock?productId=1&users=1');
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
}