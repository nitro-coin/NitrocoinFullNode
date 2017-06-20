using System;
using Microsoft.Extensions.Logging;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using Nitrocoin.Bitcoin.Logging;

namespace Nitrocoin.Bitcoin.Connection
{
	public class RelayBehavior : NodeBehavior
	{
		public RelayBehavior()
		{
		}


		public override object Clone()
		{
			return new RelayBehavior();
		}

		protected override void AttachCore()
		{
			this.AttachedNode.StateChanged += AttachedNode_StateChanged;
			this.AttachedNode.MessageReceived += AttachedNode_MessageReceived;
		}

		private void AttachedNode_MessageReceived(Node node, IncomingMessage message)
		{
		}

		private void AttachedNode_StateChanged(Node node, NodeState oldState)
		{
		}

		protected override void DetachCore()
		{
			this.AttachedNode.StateChanged -= AttachedNode_StateChanged;
			this.AttachedNode.MessageReceived -= AttachedNode_MessageReceived;
		}
	}
}