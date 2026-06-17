import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 100,
    duration: '30s',
    thresholds: {
        http_req_duration: ['p(99)<100'], // p99 < 100ms
        http_req_failed: ['rate<0.01'],
    },
};

export default function () {
    const baseUrl = __ENV.BASE_URL || 'https://staging.cfchub.com.br';
    
    const url = `${baseUrl}/api/v1/students?limit=20`;

    const params = {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${__ENV.JWT_TOKEN || 'test-token'}`,
        },
    };

    const res = http.get(url, params);

    check(res, {
        'status is 200': (r) => r.status === 200,
        'response time < 100ms': (r) => r.timings.duration < 100,
    });

    sleep(1);
}
