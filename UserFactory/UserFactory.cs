using Microsoft.AspNetCore.Identity;
using NGitLab;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserFactory.Models;

namespace UserFactory
{
    public class UserFactory<T> where T : IdentityUser, new()
    {
        private readonly GitLabClient _gitlabClient;

        public UserFactory(string hostUrl, string token)
        {
            _gitlabClient = new GitLabClient(hostUrl, token);
        }

        public async Task AddMissingUsers(List<T> currentUsers, UserManager<T> userManager)
        {
            var mails = currentUsers.Select(x => x.Email).ToList();
            var usernames = currentUsers.Select(x => x.UserName).ToList();
            var gitlabUsers = this.GetGitLabUsers();
            foreach (var user in gitlabUsers)
            {
                if (!mails.Contains(user.Email) && !usernames.Contains(user.Username))
                {
                    await CreateUser(user, userManager);
                }
            }
        }

        protected virtual async Task CreateUser(ApplicationUser missUser, UserManager<T> userManager)
        {
            var user = new T()
            {
                UserName = missUser.Username,
                Email = missUser.Email,
            };
            var property = typeof(T).GetProperty("Name");
            if (property != null && property.CanWrite)
            { 
                property.SetValue(user, missUser.Name);
            }
            await userManager.CreateAsync(user);
        }

        protected List<ApplicationUser> GetGitLabUsers()
        {
            var userQuery = new UserQuery() { IsExternal = true };
            var externalUsers = _gitlabClient.Users.Get(userQuery)
                .Select(x => new ApplicationUser(x))
                .ToList();
            var users = _gitlabClient.Users.All
                .Where(user => !user.Name.EndsWith(" Bot") && user.State != "blocked")
                .Select(x => new ApplicationUser(x))
                .ToList();
            users.RemoveAll(externalUsers.Contains);
            return users;
        }
    }
}
