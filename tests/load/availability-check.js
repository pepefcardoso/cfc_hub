import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 200,
    duration: '60s',
    thresholds: {
        http_req_duration: ['p(99)<200'], // p99 < 200ms
        http_req_failed: ['rate<0.01'], // errors < 1%
    },
};

export default function () {
    // Staging environment URL (mocked)
    const baseUrl = __ENV.BASE_URL || 'https://staging.cfchub.com.br';
    
    // Simulate current date
    const today = new Date().toISOString().split('T')[0];
    const url = `${baseUrl}/api/v1/scheduling/slots/available?date=${today}&category=B`;

    const params = {
        headers: {
            'Content-Type': 'application/json',
            // Typically would need a valid JWT for the tenant
            'Authorization': `Bearer ${__ENV.JWT_TOKEN || 'test-token'}`,
        },
    };

    const res = http.get(url, params);

    check(res, {
        'status is 200': (r) => r.status === 200,
        'response time < 200ms': (r) => r.timings.duration < 200,
    });

    sleep(1);
}
