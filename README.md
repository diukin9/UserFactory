# Что такое UserFactory?
<b>UserFactory</b> - это библиотека, дополняющая и обновляющая базу данных пользователей, наследуемых от IdentityUser, недостающими пользователями с сервиса GitLab. Важными особенностями фабрики пользователей являются:
1. Фабрика принимает любые модели пользователей, наследуемых от IdentityUser. Это гарантирует ее универсальность.
2. Методы обновления и добавления пользователей в базу данных являются переопределяемыми. При необходимости вы сами сможете построить свою логику добавления и обновления пользователей.

## Как подключить фабрику пользователей:
1. Добавьте библиотеку UserFactory в свой проект (она автоматически подхватит библиотеку GitlabService)
2. Проинициализируйте объект фабрики пользователей, передав в конструктор менеджера пользователей, ссылку на хост GitLab и токен: 
```C#
var userFactory = new UserFactory(userManager, url, token);
```
3. Для добавления и обновления пользователей воспользуйтесь асинхронным методом CompareUsersAsync():
```C#
await userFactory.CompareUsersAsync();
```
4. Дождитесь завершения работы метода и проверяйте базу данных. Все готово!

## Дополнение: как добавить сквозную авторизацию через GitLab на примере ASP.NET Core:
1. Подключите фабрику пользователей по вышеуказанной инструкции, чтобы обновить базу данных
2. Установите библиотеку AspNet.Security.OAuth.GitLab // Install-Package AspNet.Security.OAuth.GitLab -Version 5.0.9
3. В гитлабе добавьте новое приложение. Для этого:
  1. Перейдите в гитлаб
  2. Кликните на свою аватарку в правом верхнем углу
  3. Выберите вкладку "Edit Profile"
  4. На левой панели выберите "Applications"
  5. Заполните графу "Name" (имя можно придумать любое)
  6. В графу "Redirect URI" вставьте ссылку в следующем формате:
     https://localhost:ПОРТ/signin-gitlab
     Например: https://localhost:5001/signin-gitlab
  7. Отметьте следующие scope:
     * read_user
     * openid
  8. Сохраните приложение. Гитлаб сгенерирует Application ID (тоже самое, что и clientId) и Secret (тоже самое, что и clientSecret). Эти два поля нам понадобятся в скором времени.
4. В файле appsettings.jsom добавьте следующее:
```json
{
  ...,
  "Gitlab": {
    "HostUrl": "ссылка на ваш хост гитлаба",
    "Token": "ваш токен",
    "ClientId": "clientId сгенерированный в пункте выше",
    "ClientSecret": "clientSecret сгенерированный в пункте вышел"
  },
  ...
}
```
5. В методе Startup.cs добавьте следующее:
```C#
public void ConfigureServices(IServiceCollection services)
{
  ...
  var gitlabSettings = new GitlabSettings();
    Configuration.GetSection("Gitlab").Bind(gitlabSettings);
  services.AddAuthentication(options => { })
    .AddGitLab(options => {
        options.ClientId = gitlabSettings.ClientId;
        options.ClientSecret = gitlabSettings.ClientSecret;
        options.AuthorizationEndpoint = $"{gitlabSettings.HostUrl}oauth/authorize";
        options.TokenEndpoint = $"{gitlabSettings.HostUrl}oauth/token";
        options.UserInformationEndpoint = $"{gitlabSettings.HostUrl}api/v4/user";
        options.CallbackPath = "/signin-gitlab";
    });
}
```
6. В контроллер добавьте следующее (при необходимости переделайте логику методов под себя):
```C#
  [HttpGet]
  [HttpPost]
  public IActionResult GitlabAuthenticate(string returnUrl)
  {
      var provider = "GitLab";
      var root = $"/account/{nameof(GitlabAuthorization)}?returnUrl={returnUrl ?? string.Empty}";
      var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, root);
      return Challenge(properties, provider);
  }

  [HttpGet]
  [HttpPost]
  public async Task<IActionResult> GitlabAuthorization(string remoteError, string returnUrl)
  {
      var info = await _signInManager.GetExternalLoginInfoAsync();
      if (info == null || remoteError != null)
      {
          return View(nameof(Login));
      }
      var loginProvider = info.LoginProvider;
      var providerKey = info.ProviderKey;
      var claims = info.Principal.Claims;
      var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
      var user = await _userManager.FindByLoginAsync(loginProvider, providerKey);
      if (user == null)
      {
          user = await _userManager.FindByNameAsync(name);
      }
      if (user != null)
      {
          if ((user.LockoutEnd ?? DateTimeOffset.MaxValue) > DateTime.Now)
          {
              ModelState.AddModelError(string.Empty, "Ваш аккаунт заблокирован");
          }
          var identityResult = await _userManager.AddLoginAsync(user, info);
          var signInResult = await _signInManager.ExternalLoginSignInAsync(loginProvider, providerKey, false);
          if (!signInResult.Succeeded || identityResult.Errors.Any(x => x.Code != "LoginAlreadyAssociated"))
          {
              ModelState.AddModelError(string.Empty, "Возникла ошибка на стороне GitLab");
              return View(nameof(Login));
          }
          return Redirect($"~{returnUrl ?? "/"}");
      }
      ModelState.AddModelError(string.Empty, "Вы не являетесь внутренним сотрудником компании");
      return View(nameof(Login));
  }
```
7. Осталось только добавить кнопку/ссылку/форму во View, ссылающуюся на метод GitlabAuthenticate, например:
```C#
  <a asp-controller="ControllerName" asp-action="GitlabAuthenticate" asp-route-returnUrl="@Context.Request.Path">
```
8. Готово! Вы внедрили сквозную авторизацию через GitLab в свой веб-сервис!
