using Microsoft.EntityFrameworkCore;
using NET8WebMVC.DB;
using NET8WebMVC.Domain.Repositories;

namespace NET8WebMVC.Infrastructures {
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
