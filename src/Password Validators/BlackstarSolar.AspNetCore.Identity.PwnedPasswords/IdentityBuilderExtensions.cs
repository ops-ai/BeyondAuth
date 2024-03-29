﻿namespace BlackstarSolar.AspNetCore.Identity.PwnedPasswords
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using System;

    public static class IdentityBuilderExtensions
    {
        public static IdentityBuilder AddPwnedPasswordsValidator<TUser>(this IdentityBuilder builder,
            Action<PwnedPasswordsValidatorOptions> options = null) where TUser : class
        {
            if (options == null)
            {
                options = o => { };
            }

            builder.Services.Configure(options);
            return builder.AddPasswordValidator<PwnedPasswordsValidator<TUser>>();
        }
    }
}