﻿using System;
using Nitrocoin.Bitcoin.Wallet.JsonConverters;
using NBitcoin;
using Newtonsoft.Json;

namespace Nitrocoin.Bitcoin.Wallet.Models
{
    public class WalletGeneralInfoModel
    {
        [JsonProperty(PropertyName = "walletFilePath")]
        public string WalletFilePath { get; set; }
        
        [JsonProperty(PropertyName = "network")]
        [JsonConverter(typeof(NetworkConverter))]
        public Network Network { get; set; }

        /// <summary>
        /// The time this wallet was created.
        /// </summary>
        [JsonProperty(PropertyName = "creationTime")]
        [JsonConverter(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset CreationTime { get; set; }

        [JsonProperty(PropertyName = "isDecrypted")]
        public bool IsDecrypted { get; set; }

        /// <summary>
        /// The height of the last block that was synced.
        /// </summary>
        [JsonProperty(PropertyName = "lastBlockSyncedHeight")]
        public int? LastBlockSyncedHeight { get; set; }

        /// <summary>
        /// The total number of blocks.
        /// </summary>
        [JsonProperty(PropertyName = "chainTip")]
        public int? ChainTip { get; set; }

        /// <summary>
        /// The total number of nodes that we're connected to.
        /// </summary>
        [JsonProperty(PropertyName = "connectedNodes")]
        public int ConnectedNodes { get; set; }
    }
}
