﻿namespace Raven.Identity
{
    /// <summary>
    /// The type of ID to create for users.
    /// </summary>
    public enum UserIdType
    {
        /// <summary>
        /// Default. We'll use whatever you supplied while creating the user. If no ID is specified, we'll fall back to Raven's default ID creation, which can be configured via Raven's Global Identifier Generation Conventions: https://ravendb.net/docs/article-page/4.2/csharp/client-api/configuration/identifier-generation/global
        /// </summary>
        None,
        /// <summary>
        /// The ID will utilize the user's email, e.g. "AppUsers/johndoe@mail.com"
        /// </summary>
        Email,
        /// <summary>
        /// The ID will utilize the user's username, e.g. "AppUsers/johndoe"
        /// </summary>
        UserName,
        /// <summary>
        /// The ID will be generated by the server dynamically, e.g. "AppUsers/00000000001-A"
        /// </summary>
        ServerGenerated,
        /// <summary>
        /// The ID will be generated by the current node in the cluster dynamically using the collection/number-tag convention, e.g. "AppUsers/1-A".
        /// </summary>
        NumberTag,
        /// <summary>
        /// The ID will be generated by the cluster to use consecutive IDs, e.g. "AppUsers/1". Note that performance is impacted due to having to consult with the cluster to find the next consecutive number.
        /// </summary>
        Consecutive
    }
}
