﻿using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Raven.Identity
{
    /// <summary>
    /// Extends <see cref="IdentityBuilder"/> so that RavenDB services can be registered through it.
    /// </summary>
    public static class IdentityBuilderExtensions
    {
        /// <summary>
        /// Registers a RavenDB as the user store.
        /// </summary>
        /// <typeparam name="TUser">The type of the user.</typeparam>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IdentityBuilder AddRavenDbIdentityStores<TUser>(this IdentityBuilder builder) where TUser : IdentityUser
        {
            return builder.AddRavenDbIdentityStores<TUser, IdentityRole>();
        }

        /// <summary>
        /// Registers a RavenDB as the user store.
        /// </summary>
        /// <typeparam name="TUser">The type of the user.</typeparam>
        /// <typeparam name="TRole">The type of the role.</typeparam>
        /// <param name="builder">The builder.</param>
        /// <returns>The builder.</returns>
        public static IdentityBuilder AddRavenDbIdentityStores<TUser, TRole>(this IdentityBuilder builder)
            where TUser : IdentityUser
            where TRole : IdentityRole, new()
        {
            builder.Services.AddScoped<IUserStore<TUser>, UserStore<TUser, TRole>>();
            builder.Services.AddScoped<IRoleStore<TRole>, RoleStore<TRole>>();

            return builder;
        }
    }
}
