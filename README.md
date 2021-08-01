# Что такое UserFactory?
<b>UserFactory</b> - это библиотека, дополняющая и обновляющая базу данных пользователей, наследуемых от IdentityUser, недостающими пользователями с сервиса GitLab. Важными особенностями фабрики пользователей являются:
1. Фабрика принимает любые модели пользователей, наследуемые от IdentityUser. Это гарантирует ее универсальность.
2. Методы обновления и добавления пользователей в базу данных являются переопределяемыми. При необходимости вы сами сможете выстроить свою логику добавления и обновления пользователей.

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

## Пример использования фабрики пользователей в ASP.NET Core:
```C#
public class ExampleController : Controller
{
   private readonly UserFactory<ApplicationUser> _userFactory;
   private readonly UserManager<ApplicationUser> _userManager;

   public ExampleController(UserManager<ApplicationUser> userManager, IOptions<GitlabSettings> gitlabSettings)
   {
      _userManager = userManager;
      var url = gitlabSettings.Value.HostUrl;
      var token = gitlabSettings.Value.Token;
      _userFactory = new AdvancedUserFactory(mapper, _userManager, url, token);
   }

   public async Task<IActionResult> UpdateGitlabUsers()
   {
      await _userFactory.CompareUsersAsync();
      return RedirectToAction(nameof(Index));
   }
}
```

## Пример переопределения методов создания и обновления пользователей в UserFactory:
```C#
// Наследуемся от UserFactory, указывая модель пользователя, с которой работает наш сервис
public class AdvancedUserFactory : UserFactory.UserFactory<ApplicationUser>
{
   private readonly IMapper _mapper;

   public AdvancedUserFactory(IMapper mapper, UserManager<ApplicationUser> userManager, string hostUrl, string token) 
       : base(userManager, hostUrl, token)
   {
       // Переопределим делегаты с базовой реализации на новую и переопределенную
       _functionOfCreatingUser = this.CreateUserAsync;
       _functionOfUpdatingUser = this.UpdateUserAsync;
       
       _mapper = mapper;
   }
   
   // Переопределение функции создания пользователей
   protected override async Task CreateUserAsync(User user)
   {
      var newUser = _mapper.Map<ApplicationUser>(user);
      await _userManager.CreateAsync(newUser);
   }

   // Переопределение функции обновления пользователей
   protected override async Task UpdateUserAsync(User gitlabUser, ApplicationUser user)
   {
      await base.UpdateUserAsync(gitlabUser, user);
      var flag = false;
      if (gitlabUser.AvatarURL != user.ImagePath)
      {
         user.ImagePath = gitlabUser.AvatarURL;
         flag = true;
      }
      else if (gitlabUser.Name != user.Name)
      {
         user.Name = gitlabUser.Name;
         flag = true;
      }
      if (flag)
      {
         await _userManager.UpdateAsync(user);
      }
   }
}
```

## Дополнение: как добавить сквозную авторизацию GitLab на примере ASP.NET Core:
1. Подключите фабрику пользователей по вышеуказанной инструкции, чтобы обновить базу данных
2. Установите библиотеку AspNet.Security.OAuth.GitLab // Install-Package AspNet.Security.OAuth.GitLab -Version 5.0.9
3. В GitLab добавьте новое приложение. Для этого:
    * Перейдите в GitLab
    * Кликните на свою аватарку в правом верхнем углу
    * Выберите вкладку "Edit Profile"
    * На левой панельке выберите "Applications"
    * Заполните графу "Name" (имя можно придумать любое)
    * В графу "Redirect URI" вставьте ссылку в следующем формате:
     https://localhost:ПОРТ/signin-gitlab <br>
     <b>Например</b>: https://localhost:5001/signin-gitlab
    * Отметьте следующие scope: read_user, openid
    * Сохраните приложение. GitLab сгенерирует Application ID (тоже самое, что и clientId) и Secret (тоже самое, что и clientSecret). Эти два поля нам понадобятся в скором времени.
4. В файле appsettings.jsom добавьте следующее:
```json
"Gitlab": {
    "HostUrl": "ссылка на ваш хост GitLab",
    "Token": "ваш токен",
    "ClientId": "clientId сгенерированный в пункте выше",
    "ClientSecret": "clientSecret сгенерированный в пункте вышел"
},
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
  ...
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
7. Осталось только добавить кнопку/ссылку/форму и т.п. во View-представлении, ссылающуюся на метод GitlabAuthenticate, например:
```C#
  <a asp-controller="ControllerName" asp-action="GitlabAuthenticate" asp-route-returnUrl="@Context.Request.Path">
```
8. Готово! Вы внедрили сквозную авторизацию GitLab в свой веб-сервис!
