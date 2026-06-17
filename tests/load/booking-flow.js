import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

export const serverErrors = new Rate('server_errors');

export const options = {
    vus: 50,
    duration: '60s',
    thresholds: {
        http_req_duration: ['p(99)<500'], // p99 < 500ms
        server_errors: ['rate<0.01'], // 5xx errors < 1%
    },
};

export default function () {
    const baseUrl = __ENV.BASE_URL || 'https://staging.cfchub.com.br';

    const url = `${baseUrl}/api/v1/scheduling/slots`;

    const payload = JSON.stringify({
        instructorId: '00000000-0000-0000-0000-000000000001',
        vehicleId: '00000000-0000-0000-0000-000000000002',
        trackId: '00000000-0000-0000-0000-000000000003',
        studentId: '00000000-0000-0000-0000-000000000004',
        date: new Date().toISOString().split('T')[0],
        startTime: '08:00:00',
        endTime: '08:50:00',
        category: 'B'
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${__ENV.JWT_TOKEN || 'test-token'}`,
        },
    };

    const res = http.post(url, payload, params);

    // 201 Created or 409 Conflict are both acceptable outcomes in this scenario. 
    // 5xx means an actual server error (which is not expected).
    check(res, {
        'status is 201 or 409': (r) => r.status === 201 || r.status === 409,
        'no 5xx errors': (r) => r.status < 500,
    });
    
    serverErrors.add(res.status >= 500);

    sleep(1);
}
