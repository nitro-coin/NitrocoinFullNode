﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nitrocoin.Bitcoin.Wallet.JsonConverters;
using NBitcoin;
using NBitcoin.JsonConverters;
using Newtonsoft.Json;
using Nitrocoin.Bitcoin.RPC.Models;
using Nitrocoin.Bitcoin.Wallet;
using Script = NBitcoin.Script;

namespace Nitrocoin.Bitcoin.WatchOnlyWallet
{
    /// <summary>
    /// A wallet
    /// </summary>
    public class WatchOnlyWallet
    {
        /// <summary>
        /// The network this wallet is for.
        /// </summary>
        [JsonProperty(PropertyName = "network")]
        [JsonConverter(typeof(NetworkConverter))]
        public Network Network { get; set; }

        /// <summary>
        /// The type of coin, Bitcoin or Nitrocoin.
        /// </summary>
        [JsonProperty(PropertyName = "coinType")]
        public CoinType CoinType { get; set; }

        /// <summary>
        /// The time this wallet was created.
        /// </summary>
        [JsonProperty(PropertyName = "creationTime")]
        [JsonConverter(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// The height of the last block that was synced.
        /// </summary>
        [JsonProperty(PropertyName = "lastBlockSyncedHeight", NullValueHandling = NullValueHandling.Ignore)]
        public int? LastBlockSyncedHeight { get; set; }

        /// <summary>
        /// The hash of the last block that was synced.
        /// </summary>
        [JsonProperty(PropertyName = "lastBlockSyncedHash", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(UInt256JsonConverter))]
        public uint256 LastBlockSyncedHash { get; set; }

        /// <summary>
        /// The script pub key for this address.
        /// </summary>
        [JsonProperty(PropertyName = "scripts", ItemConverterType = typeof(ScriptJsonConverter))]
        public ICollection<Script> Scripts { get; set; }

        /// <summary>
        /// The list of transactions being watched.
        /// </summary>
        [JsonProperty(PropertyName = "transactions")]
        public ICollection<TransactionVerboseModel> Transactions { get; set; }
    }    
}