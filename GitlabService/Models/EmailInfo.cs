using System.Runtime.Serialization;

namespace ProjectProf.GitlabService.Models
{
    [DataContract]
    public class EmailInfo
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "email")]
        public string Email { get; set; }
    }
}
