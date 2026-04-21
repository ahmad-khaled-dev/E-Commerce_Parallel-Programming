import http from 'k6/http';
import { check } from 'k6';

export const options = {
    vus: 20,
    iterations: 20,
};

export default function () {
    const url = 'http://localhost:5162/api/products';

    const res = http.get(url);

    check(res, {
        'status is 200': (r) => r.status === 200,
    });
}