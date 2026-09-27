namespace NET8WebMVC.Domain.Repositories {
    public interface ILoaderMembers {
        public IEnumerable<Domain.Entities.Member> Load();
    }
}
