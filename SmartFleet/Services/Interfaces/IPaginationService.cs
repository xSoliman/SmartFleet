using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartFleet.Services.Interfaces
{
    public interface IPaginationService
    {
        Task<List<T>> GetPaginatedAsync<T>(IQueryable<T> query, int pageNumber, int pageSize) where T : class;
    }
} 