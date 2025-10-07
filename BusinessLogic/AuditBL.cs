using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FormBuilderAPI.BusinessLogicLayer
{
    public class AuditBL
    {
        private readonly ILogger<AuditBL> _logger;

        public AuditBL(ILogger<AuditBL> logger)
        {
            _logger = logger;
        }

        public async Task LogActionAsync(string userId, string action, string details = "")
        {
            await Task.Run(() =>
            {
                _logger.LogInformation($"[AUDIT] User: {userId}, Action: {action}, Details: {details}, Timestamp: {DateTime.UtcNow}");
            });
        }
    }
}
