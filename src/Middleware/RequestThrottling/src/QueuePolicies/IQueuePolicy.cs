// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RequestThrottling.QueuePolicies
{
    /// <summary>
    /// Queueing policies, meant to be used with the <see cref="RequestThrottlingMiddleware"></see>.
    /// </summary>
    public interface IQueuePolicy
    {
        /// <summary>
        /// Called for every incoming request.
        /// When it returns 'true' the request procedes to the server.
        /// When it returns 'false' the request is rejected immediately.
        /// </summary>
        Task<bool> TryEnterAsync();

        /// <summary>
        /// Called after successful requests have been returned from the server.
        /// Does NOT get called for rejected requests.
        /// </summary>
        void OnExit();
    }
}
