using System;

namespace UserFactory.Models
{
    public class ApplicationUser : IEquatable<ApplicationUser>
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }

        public ApplicationUser(NGitLab.Models.User user)
        {
            Name = user.Name;
            Email = user.Email;
            Username = user.Username;
        }

        public override bool Equals(Object obj)
        {
            try
            {
                return this.Username == (obj as ApplicationUser)?.Username;
            }
            catch
            {
                return false;
            }
        }

        public override int GetHashCode()
            => this.Username == null ? 0 : this.Username.GetHashCode();

        public bool Equals(ApplicationUser other) => this.Username == other.Username;
    }
}
