﻿using Discord_UWP.API;
using Discord_UWP.API.Channel;
using Discord_UWP.API.Channel.Models;
using Discord_UWP.API.Gateway;
using Discord_UWP.API.Guild;
using Discord_UWP.API.Login;
using Discord_UWP.API.Login.Models;
using Discord_UWP.API.User;
using Discord_UWP.API.User.Models;
using Discord_UWP.Authentication;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.QueryStringDotNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media.Animation;
using Discord_UWP.CacheModels;
using Discord_UWP.Gateway.DownstreamEvents;
using Microsoft.Toolkit.Uwp;

namespace Discord_UWP
{
    public sealed partial class Main : Page
    {
        private MenuFlyout MakeTextChnMenu(GuildChannel chn)
        {
            MenuFlyout menu = new MenuFlyout();
            menu.MenuFlyoutPresenterStyle = (Style)App.Current.Resources["MenuFlyoutPresenterStyle1"];
            MenuFlyoutItem editchannel = new MenuFlyoutItem()
            {
                Text = "Edit Channel",
                Tag = chn.Raw.Id,
                Icon = new SymbolIcon(Symbol.Edit),
                Margin=new Thickness(-26,0,0,0)
            };
            editchannel.Click += Editchannel;
            menu.Items.Add(editchannel);
            MenuFlyoutSeparator sep1 = new MenuFlyoutSeparator();
            menu.Items.Add(sep1);
            ToggleMenuFlyoutItem mute = new ToggleMenuFlyoutItem()
            {
                Text = "Mute Channel",
                Tag = chn.Raw.Id,
                Icon = new SymbolIcon(Symbol.Mute),
                Margin = new Thickness(-26, 0, 0, 0)
            };
            mute.IsChecked = Storage.MutedChannels.Contains(chn.Raw.Id);
            mute.Click += MuteChannel;
            menu.Items.Add(mute);
            MenuFlyoutItem markasread = new MenuFlyoutItem()
            {
                Text = "Mark as read",
                Tag = chn.Raw.Id,
                Icon = new SymbolIcon(Symbol.View),
                Margin = new Thickness(-26, 0, 0, 0),
                IsEnabled = (TextChannels.Items.FirstOrDefault(x => (x as SimpleChannel).Id == chn.Raw.Id) as SimpleChannel).IsUnread
            };
            menu.Items.Add(markasread);
            markasread.Click += MarkAsReadOnClick;
            MenuFlyoutSeparator sep2 = new MenuFlyoutSeparator();
            menu.Items.Add(sep2);
            MenuFlyoutItem deleteChannel = new MenuFlyoutItem()
            {
                Text = "Delete channel",
                Tag = chn.Raw.Id,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 240, 71, 71)),
                Icon = new SymbolIcon(Symbol.Delete),
                Margin = new Thickness(-26, 0, 0, 0),
                //IsEnabled = !(TextChannels.Items.FirstOrDefault(x => (x as SimpleChannel).Id == chn.Raw.Id) as SimpleChannel).IsUnread
            };

            if (!chn.chnPerms.EffectivePerms.ManageChannels && !chn.chnPerms.EffectivePerms.Administrator)
            {
                deleteChannel.IsEnabled = false;
            }

            menu.Items.Add(deleteChannel);
            deleteChannel.Click += DeleteChannelOnClick;
            return menu;
        }

        private void DeleteChannelOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            Session.DeleteChannel((sender as MenuFlyoutItem).Tag.ToString());
        }

        private async void MarkAsReadOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var channelId = (sender as MenuFlyoutItem).Tag.ToString();
            await Task.Run(async () =>
            {
                await Session.AckMessage(channelId,
                    Storage.Cache.Guilds.FirstOrDefault(x => x.Value.Channels.ContainsKey(channelId))
                        .Value.Channels[channelId]
                        .Raw.LastMessageId);
            });
        }

        private void MuteChannel(object sender, RoutedEventArgs e)
        {
            if (Storage.MutedChannels.Contains((sender as ToggleMenuFlyoutItem).Tag.ToString()))
            {
                Storage.MutedChannels.Remove((sender as ToggleMenuFlyoutItem).Tag.ToString());
                (TextChannels.Items.FirstOrDefault(x => (x as SimpleChannel).Id == (sender as ToggleMenuFlyoutItem).Tag.ToString()) as SimpleChannel)
                    .IsMuted = false;
            } else
            {
                Storage.MutedChannels.Add((sender as ToggleMenuFlyoutItem).Tag.ToString());
                (TextChannels.Items.FirstOrDefault(x => (x as SimpleChannel).Id ==(sender as ToggleMenuFlyoutItem).Tag.ToString()) as SimpleChannel)
                    .IsMuted = true;
            }
            Storage.SaveMutedChannels();
        }

        private void Editchannel(object sender, RoutedEventArgs e)
        {
            SubFrameNavigator(typeof(SubPages.EditChannel), (sender as MenuFlyoutItem).Tag as string);
        }
    }
}