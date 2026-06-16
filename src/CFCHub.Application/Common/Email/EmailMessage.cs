namespace CFCHub.Application.Common.Email;

public record EmailMessage(string TemplateId, string ToAddress, Dictionary<string, string> TemplateData);
