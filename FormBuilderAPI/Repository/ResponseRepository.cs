using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.Model.SQLModel;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FormBuilderAPI.Repository
{
    public class ResponseRepository : IResponseRepository
    {
        private readonly SQLDbContext _sqlContext;

        public ResponseRepository(SQLDbContext sqlContext)
        {
            _sqlContext = sqlContext;
        }

        // FormResponse operations
        public async Task<int> InsertResponseAsync(FormResponse response)
        {
            _sqlContext.FormResponses.Add(response);
            await _sqlContext.SaveChangesAsync();
            return response.ResponseId;
        }

        public async Task<List<FormResponse>> GetResponsesByFormIdAsync(string formId)
        {
            return await _sqlContext.FormResponses
                .Include(r => r.Answers)
                .Where(r => r.FormId == formId)
                .ToListAsync();
        }

        public async Task<List<FormResponse>> GetResponsesByUserIdAsync(string userId)
        {
            return await _sqlContext.FormResponses
                .Include(r => r.Answers)
                .Where(r => r.SubmittedBy == userId)
                .ToListAsync();
        }

        public async Task<List<FormResponse>> GetResponsesByFormIdAndUserIdAsync(string formId, string userId)
        {
            return await _sqlContext.FormResponses
                .Include(r => r.Answers)
                .Where(r => r.FormId == formId && r.SubmittedBy == userId)
                .ToListAsync();
        }

        // ResponseFile operations
        public async Task InsertResponseFileAsync(ResponseFile file)
        {
            _sqlContext.ResponseFiles.Add(file);
            await _sqlContext.SaveChangesAsync();
        }

        public async Task<List<ResponseFile>> GetFilesByResponseIdAsync(int responseId)
        {
            return await _sqlContext.ResponseFiles
                .Where(f => f.ResponseId == responseId)
                .ToListAsync();
        }

        public async Task<ResponseFile?> GetFileByResponseIdAndFileIdAsync(int responseId, int fileId)
        {
            return await _sqlContext.ResponseFiles
                .Where(f => f.ResponseId == responseId && f.FileId == fileId)
                .FirstOrDefaultAsync();
        }

        // User operations
        public async Task<Dictionary<string, string>> GetUserNamesByIdsAsync(List<string> userIds)
        {
            return await _sqlContext.Users
                .Where(u => userIds.Contains(u.UserId.ToString()))
                .ToDictionaryAsync(u => u.UserId.ToString(), u => u.Username);
        }

        // Bulk operations
        public async Task SaveChangesAsync()
        {
            await _sqlContext.SaveChangesAsync();
        }
    }
}
