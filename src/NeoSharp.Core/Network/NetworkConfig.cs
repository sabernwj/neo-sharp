﻿using Microsoft.Extensions.Configuration;

namespace NeoSharp.Core.Network
{
    public class NetworkConfig 
    {
        public string ServerIp { get; set; }

        public int ServerStartPort { get; set; }

        public NetworkConfig(IConfiguration configuration = null)
        {
            configuration?.GetSection(nameof(NetworkConfig))?.Bind(this);
        }
    }
}