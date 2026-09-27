using Microsoft.EntityFrameworkCore;
using App.DB;
using App.Domain.Repositories;

namespace App.Infrastructures {
    public class DBLoaderMembers : ILoaderMembers {
        private readonly SampleDbContext _dbContext;
        public DBLoaderMembers(SampleDbContext context) {
            _dbContext = context;
        }
        public IEnumerable<Domain.Entities.Member> Load() {
            return _dbContext.Members;
        }
    }
}
