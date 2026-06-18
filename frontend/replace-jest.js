const fs = require('fs');
const path = require('path');
const files = [
  'src/app/(dashboard)/alunos/page.test.tsx',
  'src/app/(dashboard)/conformidade/ExpiryDashboard.test.tsx',
  'src/app/(dashboard)/contratos/[contractId]/SignatureCapture.test.tsx',
  'src/app/(dashboard)/financeiro/alunos/[studentId]/PaymentPlanTable.test.tsx',
  'src/components/compliance/CnhStatusCard.test.tsx',
  'src/components/shell/Sidebar.test.tsx'
];
files.forEach(f => {
  let p = path.resolve(f);
  if (fs.existsSync(p)) {
    let content = fs.readFileSync(p, 'utf8');
    content = content.replace(/jest\./g, 'vi.');
    if (!content.includes("import { vi }")) {
      content = "import { vi } from 'vitest';\n" + content;
    }
    fs.writeFileSync(p, content);
  }
});
console.log('Done');
