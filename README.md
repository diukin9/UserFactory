# Что такое UserFactory? <br/>
UserFactory - это библиотека, дополняюшая базу данных пользователей, наследуемых от IdentityUser, недостающими пользователями с сервиса GitLab <br/>

### Применение:
```C#
public class YourClass
{
    private readonly UserFactory<YourModel> _userFactory;
    private readonly UserManager<YourModel> _userManager;

    public YourClass(UserManager<YourModel> userManager, string hostUrl, string token)
    {
        this._userFactory = new UserFactory<YourModel>(hostUrl, token)
        this._userManager = userManager;
    }
    
    public async Task YourMethod()
    {
        var usersFromDB = ...;
        await _userFactory.AddMissingUsers(usersFromDB, _userManager);
    }
}
```
***
### Библиотека UserFactory имеет следующие методы:
* Пополнить БД недостающими пользователями с GitLab
```C#
public async Task AddMissingUsers(List<T> usersFromDB, UserManager<T> userManager)
```
* ПЕРЕГРУЖАЕМЫЙ | Создать и добавить нового пользователя в БД
```C#
protected virtual async Task CreateUser(ApplicationUser missedUser, UserManager<T> userManager)
```
* Получить список всех пользователей с GitLab
```C#
protected List<ApplicationUser> GetGitLabUsers()
```
### Примечание: 
* Метод CreateUser перегружаемый (виртуальный). При необходимости вы можете наследоваться от этого класса и реализовать его под свои нужды;
* Сравнение аккаунтов происходит исключительно по username и email.
