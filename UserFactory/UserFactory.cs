using Microsoft.AspNetCore.Identity;
using ProjectProf.GitlabService;
using System;
using System.Threading.Tasks;

namespace ProjectProf.UserFactory
{
    public class UserFactory<T> where T : IdentityUser, new()
    {
        public delegate Task CreateUser(NGitLab.Models.User gitlabUser);
        public delegate Task UpdateUser(NGitLab.Models.User gitlabUser, T user);

        protected readonly GitlabService.GitlabService _gitlabService;
        protected readonly UserManager<T> _userManager;
        protected CreateUser _functionOfCreatingUser;
        protected UpdateUser _functionOfUpdatingUser;

        public UserFactory(UserManager<T> userManager, string hostUrl, string token)
        {
            _functionOfCreatingUser = this.CreateUserAsync;
            _functionOfUpdatingUser = this.UpdateUserAsync;
            _gitlabService = new GitlabService.GitlabService(hostUrl, token);
            _userManager = userManager;
        }

        public async Task CompareUsersAsync()
        {
            var gitlabUsers = _gitlabService.GetUsers();
            foreach (var gitlabUser in gitlabUsers)
            {
                var user = await this.GetUser(gitlabUser);
                if (gitlabUser == null)
                {
                    await this._functionOfCreatingUser(gitlabUser);
                }
                else
                {
                    await this._functionOfUpdatingUser(gitlabUser, user);
                }
            }
        }

        protected async Task<T> GetUser(NGitLab.Models.User gitlabUser)
        {
            var user = await _userManager.FindByNameAsync(gitlabUser.Username)
                ?? await _userManager.FindByEmailAsync(gitlabUser.Email);
            if (user == null)
            {
                var emails = _gitlabService.GetEmails(gitlabUser.Username);
                foreach (var email in emails)
                {
                    user = await _userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        break;
                    }
                }
            }
            return user;
        }

        protected virtual async Task CreateUserAsync(NGitLab.Models.User gitlabUser)
        {
            var newUser = new T()
            {
                UserName = gitlabUser.Username,
                Email = gitlabUser.Email,
                LockoutEnabled = true,
                LockoutEnd = gitlabUser.State == "blocked" ? DateTimeOffset.MaxValue : DateTimeOffset.MinValue,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(newUser);
        }

        protected virtual async Task UpdateUserAsync(NGitLab.Models.User gitlabUser, T user)
        {
            var flag = false;
            if (gitlabUser.State == "blocked" != user.LockoutEnd > DateTime.Now)
            {
                user.LockoutEnd = user.LockoutEnd > DateTime.Now ? DateTimeOffset.MinValue : DateTimeOffset.MaxValue;
                flag = true;
            }
            else if (gitlabUser.Email != user.Email)
            {
                user.Email = gitlabUser.Email;
                flag = true;
            }
            else if (gitlabUser.Username != user.UserName)
            {
                user.UserName = gitlabUser.Username;
                flag = true;
            }
            if (flag)
            {
                await _userManager.UpdateAsync(user);
            }
        }
    }
}
