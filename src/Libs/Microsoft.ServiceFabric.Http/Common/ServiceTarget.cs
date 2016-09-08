// ------------------------------------------------------------
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric
{
    public enum ServiceTarget
    {
        /// <summary>
        /// Selects the primary replica of a stateful service.
        /// </summary>
        Primary,

        /// <summary>
        /// Selects a random secondary replica of a stateful service.
        /// </summary>
        Secondary,

        /// <summary>
        /// Selects a random replica of a stateful service or a random instance of a stateless service.
        /// </summary>
        Any
    }
}
