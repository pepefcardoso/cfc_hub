# Error Type URIs Registry

This document serves as the central registry for all `ProblemDetails.type` URIs used in the CFCHub API, as mandated by `conventions.md §5`. AI agents implementing API endpoints must add their error types to this file.

| URI | HTTP Status | Error Code | pt-BR Detail |
|-----|-------------|------------|--------------|
| https://cfchub.com.br/errors/validation-error | 400 | VALIDATION_ERROR | Erro de validação nos dados da requisição. |
| https://cfchub.com.br/errors/unauthorized | 401 | UNAUTHORIZED | Autenticação necessária para acessar este recurso. |
| https://cfchub.com.br/errors/forbidden | 403 | FORBIDDEN | Permissões insuficientes para realizar esta ação. |
| https://cfchub.com.br/errors/not-found | 404 | NOT_FOUND | O recurso solicitado não foi encontrado. |
| https://cfchub.com.br/errors/tenant-not-found | 404 | TENANT_NOT_FOUND | Tenant não encontrado. |
| https://cfchub.com.br/errors/conflict | 409 | CONFLICT | Conflito de estado do recurso. |
| https://cfchub.com.br/errors/scheduling-conflict | 409 | SCHEDULING_CONFLICT | O instrutor já possui aula agendada neste horário. |
| https://cfchub.com.br/errors/unprocessable | 422 | UNPROCESSABLE | Regra de negócio violada. |
| https://cfchub.com.br/errors/internal-error | 500 | INTERNAL_ERROR | Erro interno do servidor. |
