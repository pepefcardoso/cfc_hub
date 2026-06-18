import { test as base } from '@playwright/test';
import { v4 as uuidv4 } from 'uuid';

type ApiFixtures = {
  apiHelper: {
    setupTenant: () => Promise<string>;
    seedSlot: (tenantSlug: string) => Promise<any>;
    cleanupTenant: (tenantSlug: string) => Promise<void>;
  };
  tenantContext: {
    slug: string;
  };
};

export const test = base.extend<ApiFixtures>({
  apiHelper: async ({ request }, use) => {
    const backendUrl = process.env.BACKEND_API_URL || 'http://localhost:5000/api/v1';

    await use({
      setupTenant: async () => {
        const slug = `e2e-${uuidv4().substring(0, 8)}`;
        
        // This is a placeholder for the actual tenant registration endpoint
        const res = await request.post(`${backendUrl}/admin/tenants`, {
          data: {
            name: `E2E Tenant ${slug}`,
            slug: slug,
            adminEmail: `admin@${slug}.com`,
            adminPassword: process.env.TEST_ADMIN_PASSWORD || 'password'
          }
        });
        
        // Expecting it to handle tenant provisioning via TenantProvisioningService
        if (!res.ok()) {
           console.log("Setup tenant warning - if endpoint is different, adjust this fixture.");
        }

        return slug;
      },
      seedSlot: async (tenantSlug: string) => {
        // Authenticate as the tenant admin to seed data
        const loginRes = await request.post(`${backendUrl}/auth/login`, {
          data: { email: `admin@${tenantSlug}.com`, password: process.env.TEST_ADMIN_PASSWORD || 'password' }
        });
        const token = loginRes.ok() ? (await loginRes.json()).token : null;
        const headers = token ? { Authorization: `Bearer ${token}` } : {};

        // Provision instructor
        await request.post(`${backendUrl}/staff`, {
          headers,
          data: { name: 'E2E Instructor', role: 'Instructor', email: `inst@${tenantSlug}.com` }
        });

        // Provision vehicle
        await request.post(`${backendUrl}/vehicles`, {
          headers,
          data: { plate: 'ABC-1234', category: 'B', model: 'Onix' }
        });

        // Seed available slot
        const slotRes = await request.post(`${backendUrl}/scheduling/slots`, {
          headers,
          data: {
            startTime: new Date(Date.now() + 86400000).toISOString(), // Tomorrow
            durationMinutes: 50,
            instructorId: 'placeholder', // Ideally from the responses above
            vehicleId: 'placeholder'
          }
        });

        return slotRes.ok() ? await slotRes.json() : null;
      },
      cleanupTenant: async (tenantSlug: string) => {
        // Cleanup resources (soft delete or full drop depending on environment)
        // Usually, in test environments, a full database teardown or schema drop is preferred
        await request.delete(`${backendUrl}/admin/tenants/${tenantSlug}`);
      }
    });
  },
  tenantContext: async ({ apiHelper }, use) => {
    const slug = await apiHelper.setupTenant();
    await use({ slug });
    await apiHelper.cleanupTenant(slug);
  }
});
