import http from 'k6/http';
import { check } from 'k6';

export const options = {
    vus: 10,
    iterations: 100,
};

export default function () {
    const res = http.get('http://localhost:5000/route');
    check(res, { 'status is 200': (r) => r.status === 200 });
}