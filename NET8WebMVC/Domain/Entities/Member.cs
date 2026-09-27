using System.ComponentModel;

namespace NET8WebMVC.Domain.Entities {
    public class Member {
        public int Id { get; set; }
        [DisplayName("氏名")]
        public string? Name { get; set; }
        [DisplayName("説明")]
        public string? Description { get; set; }
    }
}
