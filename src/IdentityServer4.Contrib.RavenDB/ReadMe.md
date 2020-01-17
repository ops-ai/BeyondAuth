# IdentityServer 4 RavenDB stores

### Enable Document Expiration
```
await store.Maintenance.SendAsync(new ConfigureExpirationOperation(new ExpirationConfiguration
{
             Disabled = false,
             DeleteFrequencyInSec = 60
}));
```


### Register Claims and Principals custom IdentityServer serializers
```
documentStore.Conventions.CustomizeJsonSerializer += (JsonSerializer serializer) =>
{
    serializer.Converters.Add(new ClaimConverter());
    serializer.Converters.Add(new ClaimsPrincipalConverter());
};
```