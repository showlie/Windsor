﻿// Copyright 2004-2008 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Facilities.WcfIntegration
{
	using System;
	using System.Reflection;
	using Castle.MicroKernel;
	using System.ServiceModel;
	using System.ServiceModel.Description;
	using System.ServiceModel.Channels;

	public abstract class AbstractChannelBuilder<M> : AbstractChannelBuilder, IClientChannelBuilder<M>
			where M : IWcfClientModel
	{
		private M clientModel;

		public AbstractChannelBuilder(IKernel kernel)
			: base(kernel)
		{
		}

		/// <summary>
		/// Get a delegate capable of creating channels.
		/// </summary>
		/// <param name="clientModel">The client model.</param>
		/// <returns>The <see cref="ChannelCreator"/></returns>
		public ChannelCreator GetChannelCreator(M clientModel)
		{
			this.clientModel = clientModel;
			return GetEndpointChannelCreator(clientModel.Endpoint);
		}

		/// <summary>
		/// Get a delegate capable of creating channels.
		/// </summary>
		/// <param name="model">The component model.</param>
		/// <param name="clientModel">The client model.</param>
		/// <param name="contract">The contract override.</param>
		/// <returns>The <see cref="ChannelCreator"/></returns>
		public ChannelCreator GetChannelCreator(M clientModel, Type contract)
		{
			this.clientModel = clientModel;
			return GetEndpointChannelCreator(clientModel.Endpoint, contract);
		}

		#region AbstractChannelBuilder Members

		protected override ChannelCreator GetChannelCreator(Type contract, ServiceEndpoint endpoint)
		{
			return GetChannelCreator(clientModel, contract, endpoint);
		}

		protected override ChannelCreator GetChannelCreator(Type contract, string configurationName)
		{
			return GetChannelCreator(clientModel, contract, configurationName);
		}

		protected override ChannelCreator GetChannelCreator(Type contract, Binding binding, string address)
		{
			return GetChannelCreator(clientModel, contract, binding, address);
		}

		protected override ChannelCreator GetChannelCreator(Type contract, Binding binding, EndpointAddress address)
		{
			return GetChannelCreator(contract, binding, address);
		}

		#endregion

		#region GetChannelCreator Members

		protected virtual ChannelCreator GetChannelCreator(M clientModel, Type contract,
														   ServiceEndpoint endpoint)
		{
			return CreateChannelCreator(contract, clientModel, endpoint);
		}

		protected virtual ChannelCreator GetChannelCreator(M clientModel, Type contract,
														   string configurationName)
		{
			return CreateChannelCreator(contract, clientModel, configurationName);
		}

		protected virtual ChannelCreator GetChannelCreator(M clientModel, Type contract,
														   Binding binding, string address)
		{
			return CreateChannelCreator(contract, clientModel, binding, address);
		}

		protected virtual ChannelCreator GetChannelCreator(M clientModel, Type contract,
														   Binding binding, EndpointAddress address)
		{
			return CreateChannelCreator(contract, clientModel, binding, address);
		}

		protected virtual ChannelCreator CreateChannelCreator(Type contract, M clientModel,
			                                                  params object[] channelFactoryArgs)
		{
			Type type = typeof(ChannelFactory<>).MakeGenericType(new Type[] { contract });

			ChannelFactory channelFactory = (ChannelFactory)
				Activator.CreateInstance(type, channelFactoryArgs);
			channelFactory.Opening += delegate { OnOpening(channelFactory, clientModel); };

			MethodInfo methodInfo = type.GetMethod("CreateChannel", new Type[0]);
			return (ChannelCreator)Delegate.CreateDelegate(
				typeof(ChannelCreator), channelFactory, methodInfo);
		}

		protected virtual void OnOpening(ChannelFactory channelFactory, M clientModel)
		{
			ServiceEndpointBehaviors behaviors =
				new ServiceEndpointBehaviors(channelFactory.Endpoint, Kernel)
				.Install(new WcfEndpointBehaviors(WcfBehaviorScope.Clients));

			if (clientModel != null)
			{
				foreach (IWcfBehavior behavior in clientModel.Behaviors)
				{
					behaviors.Install(behavior);
				}
			}
		}

		#endregion
	}
}