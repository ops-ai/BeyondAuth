# BeyondAuth User Store

`UserStore` implementation for usage with `UserManager<T>` and `SigninManager<T>`


## Usage
Register the `UserStore` in `startup.cs`
```csharp 
services.AddIdentityCore<IdPUser>()
	.AddUserStore<BeyondAuthUserStore<IdPUser>>()
	.AddDefaultTokenProviders();
```