import http from 'k6/http';
import { check } from 'k6';

export const options = {
    vus: 10,
    iterations: 10,
};

export default function () {
    const url = 'http://localhost:5162/api/inventory/1/decrease';

    const payload = JSON.stringify({
        amount: 1
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    const res = http.post(url, payload, params);

    check(res, {
        'status is 200 or 400 or 409': (r) =>
            r.status === 200 || r.status === 400 || r.status === 409,
    });
}