﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Nitrocoin.Bitcoin.Utilities;

namespace Nitrocoin.Bitcoin.RPC
{
    public interface IRPCAuthorization
    {
        List<IPAddress> AllowIp { get; }
        List<string> Authorized { get; }

        bool IsAuthorized(string user);
        bool IsAuthorized(IPAddress ip);
    }

    public class RPCAuthorization : IRPCAuthorization
    {
        private readonly List<string> authorized;
        private readonly List<IPAddress> allowIp;

        public RPCAuthorization()
        {
            this.allowIp = new List<IPAddress>();
            this.authorized = new List<string>();
        }

        public List<string> Authorized
        {
            get
            {
                return this.authorized;
            }
        }

        public List<IPAddress> AllowIp
        {
            get
            {
                return this.allowIp;
            }
        }

        public bool IsAuthorized(string user)
        {
            Guard.NotEmpty(user, nameof(user));

            return Authorized.Any(a => a.Equals(user, StringComparison.OrdinalIgnoreCase));
        }
        public bool IsAuthorized(IPAddress ip)
        {
            Guard.NotNull(ip, nameof(ip));

            if (AllowIp.Count == 0)
                return true;
            return AllowIp.Any(i => i.AddressFamily == ip.AddressFamily && i.Equals(ip));
        }
    }
}
