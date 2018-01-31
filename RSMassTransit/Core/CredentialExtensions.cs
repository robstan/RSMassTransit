﻿using System.Net;
using RSMassTransit.Messages;

namespace RSMassTransit
{
    internal static class CredentialExtensions
    {
        public static NetworkCredential GetNetworkCredential(this ICredential credential)
            => new NetworkCredential(credential.UserName, credential.Password);
    }
}