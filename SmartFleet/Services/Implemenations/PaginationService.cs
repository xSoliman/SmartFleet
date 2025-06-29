using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Services.Implemenations
{
    public class PaginationService : IPaginationService
    {
        public async Task<List<T>> GetPaginatedAsync<T>(IQueryable<T> query, int pageNumber, int pageSize) where T : class
        {
            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
} 