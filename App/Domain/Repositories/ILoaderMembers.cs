namespace App.Domain.Repositories {
    public interface ILoaderMembers {
        public IEnumerable<Domain.Entities.Member> Load();
    }
}
