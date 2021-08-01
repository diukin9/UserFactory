using NGitLab;
using NGitLab.Impl;
using NGitLab.Models;
using ProjectProf.GitlabService.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ProjectProf.GitlabService
{
    public class GitlabService
    {
        private readonly IGitLabClient _client;
        private readonly API _api;

        public GitlabService(string hostUrl, string token)
        {
            _client = new GitLabClient(hostUrl, token);
            _api = _client.GetType()
                .GetField("_api", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(_client) as API;
        }

        public List<User> GetUsers()
        {
            var userQuery = new UserQuery() { IsExternal = true };
            var externalUserMailboxes = _client.Users.Get(userQuery)
                .Select(x => x.Email)
                .ToList();
            var users = _client.Users.All
                .Where(x => !x.Name.EndsWith(" Bot") && !externalUserMailboxes.Contains(x.Email))
                .ToList();
            return users;
        }

        public List<string> GetEmails(string username)
        {
            var user = _client.Users.All
                .SingleOrDefault(x => x.Username == username);
            try
            {
                var emails = _api.Get()
                    .GetAll<EmailInfo>($"/users/{user.Id}/emails")
                    .Select(x => x.Email)
                    .ToList();
                emails.Add(user.Email);
                return emails;
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
