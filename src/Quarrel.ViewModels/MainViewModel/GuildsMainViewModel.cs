﻿// Copyright (c) Quarrel. All rights reserved.

using GalaSoft.MvvmLight.Command;
using JetBrains.Annotations;
using Quarrel.ViewModels.Helpers;
using Quarrel.ViewModels.Messages.Gateway;
using Quarrel.ViewModels.Messages.Gateway.Guild;
using Quarrel.ViewModels.Messages.Navigation;
using Quarrel.ViewModels.Models.Bindables.Channels;
using Quarrel.ViewModels.Models.Bindables.Guilds;
using Quarrel.ViewModels.Models.Bindables.Users;
using Quarrel.ViewModels.Models.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Quarrel.ViewModels
{
    /// <summary>
    /// The ViewModel for all data throughout the app.
    /// </summary>
    public partial class MainViewModel
    {
        private RelayCommand<IGuildListItem> _navigateGuild;
        private RelayCommand _navigateAddServerPage;
        private BindableGuild _currentGuild;
        private BindableGuildMember _currentGuildMember;

        /// <summary>
        /// Gets a command that sends Messenger Request to change Guild.
        /// </summary>
        public RelayCommand<IGuildListItem> GuildListItemClicked => _navigateGuild = _navigateGuild ?? new RelayCommand<IGuildListItem>(
            (guildListItem) =>
            {
                if (guildListItem is BindableGuild bGuild)
                {
                    CurrentGuild.Selected = false;
                    bGuild.Selected = true;
                    Task.Run(() =>
                    {
                        MessengerInstance.Send(new GuildNavigateMessage(bGuild));
                    });
                }
                else if (guildListItem is BindableGuildFolder bindableGuildFolder)
                {
                    bool collapsed = !bindableGuildFolder.IsCollapsed;
                    bindableGuildFolder.IsCollapsed = collapsed;
                    foreach (var guildId in bindableGuildFolder.Model.GuildIds)
                    {
                        _guildsService.AllGuilds.TryGetValue(guildId, out var guild);
                        if (guild != null)
                        {
                            guild.IsCollapsed = collapsed;
                        }
                    }
                }
            });

        /// <summary>
        /// Gets a command that opens the add server page.
        /// </summary>
        public RelayCommand NavigateAddServerPage => _navigateAddServerPage = new RelayCommand(() =>
        {
            _subFrameNavigationService.NavigateTo("AddServerPage");
        });

        /// <summary>
        /// Gets or sets the currently selected guild.
        /// </summary>
        public BindableGuild CurrentGuild
        {
            get => _currentGuild;
            set => Set(ref _currentGuild, value);
        }

        /// <summary>
        /// Gets the current user's BindableGuildMember in the current guild.
        /// </summary>
        public BindableGuildMember CurrentGuildMember
        {
            get => _currentGuildMember;
            private set => Set(ref _currentGuildMember, value);
        }

        /// <summary>
        /// Gets all Guilds the current member is in.
        /// </summary>
        [NotNull]
        public ObservableRangeCollection<IGuildListItem> BindableGuilds { get; private set; } =
            new ObservableRangeCollection<IGuildListItem>();

        private void RegisterGuildsMessages()
        {
            MessengerInstance.Register<GuildNavigateMessage>(this, m =>
            {
                if (CurrentGuild != m.Guild)
                {
                    BindableChannel channel =
                        m.Guild.Channels.FirstOrDefault(x => x.IsTextChannel && x.Permissions.ReadMessages);
                    BindableGuildMember currentGuildMember;

                    if (!m.Guild.IsDM)
                    {
                        currentGuildMember = _guildsService.GetGuildMember(_currentUserService.CurrentUser.Model.Id, m.Guild.Model.Id);
                    }
                    else
                    {
                        currentGuildMember = new BindableGuildMember(
                            new DiscordAPI.Models.GuildMember()
                            {
                                User = _currentUserService.CurrentUser.Model,
                            },
                            "DM",
                            _currentUserService.CurrentUser.Presence);
                    }

                    _dispatcherHelper.CheckBeginInvokeOnUi(() =>
                    {
                        CurrentChannel = channel;
                        CurrentGuild = m.Guild;
                        BindableMessages.Clear();

                        if (m.Guild.IsDM)
                        {
                            CurrentBindableMembers.Clear();
                        }

                        CurrentGuildMember = currentGuildMember;
                    });

                    if (channel != null)
                    {
                        MessengerInstance.Send(new ChannelNavigateMessage(channel));
                    }

                    _analyticsService.Log(
                        m.Guild.IsDM ?
                        Constants.Analytics.Events.OpenDMs :
                        Constants.Analytics.Events.OpenGuild,
                        ("guild-id", m.Guild.Model.Id));
                }
            });

            // Handles string message used for App Events
            MessengerInstance.Register<string>(this, m =>
            {
                if (m == "GuildsReady")
                {
                    _dispatcherHelper.CheckBeginInvokeOnUi(() =>
                    {
                        // Show guilds
                        BindableGuilds.Clear();
                        BindableGuilds.Add(_guildsService.AllGuilds["DM"]);

                        if (_guildsService.AllGuildFolders.Count > 0)
                        {
                            foreach (var folder in _guildsService.AllGuildFolders)
                            {
                                if (folder.Model.Id != null)
                                {
                                    BindableGuilds.Add(folder);
                                }

                                foreach (var guildId in folder.Model.GuildIds)
                                {
                                    _guildsService.AllGuilds.TryGetValue(guildId, out BindableGuild bindableGuild);
                                    if (bindableGuild != null)
                                    {
                                        BindableGuilds.Add(bindableGuild);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var guild in _guildsService.AllGuilds.Values)
                            {
                                if (guild.Model.Id != "DM")
                                {
                                    BindableGuilds.Add(guild);
                                }
                            }
                        }
                    });
                }
            });

            MessengerInstance.Register<GatewayGuildCreatedMessage>(this, m =>
            {
                BindableGuild guild = new BindableGuild(m.Guild);
                _guildsService.AllGuilds.Add(m.Guild.Id, guild);
                _dispatcherHelper.CheckBeginInvokeOnUi(() => { BindableGuilds.Insert(1, guild); });
            });

            MessengerInstance.Register<GatewayGuildDeletedMessage>(this, m =>
            {
                BindableGuild guild;
                if (_guildsService.AllGuilds.ContainsKey(m.Guild.GuildId))
                {
                    guild = _guildsService.AllGuilds[m.Guild.GuildId];
                    _guildsService.AllGuilds.Remove(m.Guild.GuildId);
                    _dispatcherHelper.CheckBeginInvokeOnUi(() => { BindableGuilds.Remove(guild); });
                }
            });

            MessengerInstance.Register<GatewayUserSettingsUpdatedMessage>(this, m =>
            {
                _dispatcherHelper.CheckBeginInvokeOnUi(() =>
                {
                    if (m.Settings.GuildFolders != null && m.Settings.GuildOrder != null)
                    {
                        // TODO: Handle guild reorder.
                    }
                });
            });
        }
    }
}
