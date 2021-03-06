﻿// Copyright (c) Quarrel. All rights reserved.

using DiscordAPI.Models;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Quarrel.ViewModels.Messages.Gateway;
using Quarrel.ViewModels.Messages.Navigation;
using Quarrel.ViewModels.Models.Bindables.Channels;
using Quarrel.ViewModels.Services.Analytics;
using Quarrel.ViewModels.Services.Discord.Guilds;
using Quarrel.ViewModels.Services.DispatcherHelper;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Quarrel.ViewModels.Services.Discord.Channels
{
    /// <summary>
    /// Manages the all channels the client has access to.
    /// </summary>
    public class ChannelsService : IChannelsService
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly IDispatcherHelper _dispatcherHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelsService"/> class.
        /// </summary>
        /// <param name="analyticsService">The app's analytics service.</param>
        /// <param name="dispatcherHelper">The aoo's dispatcher helper.</param>
        public ChannelsService(
            IAnalyticsService analyticsService,
            IDispatcherHelper dispatcherHelper)
        {
            _analyticsService = analyticsService;
            _dispatcherHelper = dispatcherHelper;

            Messenger.Default.Register<ChannelNavigateMessage>(this, m =>
            {
                _dispatcherHelper.CheckBeginInvokeOnUi(() =>
                {
                    if (CurrentChannel != null)
                    {
                        if (CurrentChannel.Guild.Model.Id != m.Guild.Model.Id)
                        {
                            Messenger.Default.Send(new GuildNavigateMessage(m.Guild));
                        }

                        CurrentChannel.Selected = false;
                    }

                    CurrentChannel = m.Channel;
                    m.Channel.Selected = true;
                });
            });

            Messenger.Default.Register<GatewayMessageAckMessage>(this, m =>
            {
                var channel = GetChannel(m.ChannelId);
                channel?.UpdateLRMID(m.MessageId);
            });
        }

        /// <inheritdoc/>
        public BindableChannel CurrentChannel { get; private set; }

        /// <inheritdoc/>
        public IDictionary<string, BindableChannel> AllChannels { get; } = new ConcurrentDictionary<string, BindableChannel>();

        /// <inheritdoc/>
        public IDictionary<string, ChannelOverride> ChannelSettings { get; } =
            new ConcurrentDictionary<string, ChannelOverride>();

        /// <inheritdoc/>
        public BindableChannel GetChannel(string channelId)
        {
            if (channelId == null)
            {
                return null;
            }

            return AllChannels.TryGetValue(channelId, out BindableChannel channel) ? channel : null;
        }
    }
}
