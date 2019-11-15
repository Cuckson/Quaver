using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Quaver.Server.Client;
using Quaver.Server.Client.Handlers;
using Quaver.Server.Client.Structures;
using Quaver.Server.Common.Enums;
using Quaver.Shared.Graphics.Menu.Border;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Graphics.Overlays.Chatting.Channels;
using Quaver.Shared.Graphics.Overlays.Chatting.Messages;
using Quaver.Shared.Graphics.Overlays.Hub;
using Quaver.Shared.Online;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Input;
using Wobble.Logging;
using Wobble.Window;
using ColorHelper = Quaver.Shared.Helpers.ColorHelper;

namespace Quaver.Shared.Graphics.Overlays.Chatting
{
    public class OnlineChat : Sprite, IResizable
    {
        /// <summary>
        /// </summary>
        public Bindable<ChatChannel> ActiveChannel { get; } = new Bindable<ChatChannel>(null);

        /// <summary>
        ///     List of chat channels that are available to join
        /// </summary>
        public static List<ChatChannel> AvailableChatChannels { get; } = new List<ChatChannel>();

        /// <summary>
        ///     The list of chat channels that the user has joined
        /// </summary>
        public static List<ChatChannel> JoinedChatChannels { get; } = new List<ChatChannel>();

        /// <summary>
        /// </summary>
        public ChatChannelList ChannelList { get; private set; }

        /// <summary>
        /// </summary>
        public ChatMessageContainer MessageContainer { get; private set; }

        /// <summary>
        ///     If the chat overlay is opened
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// </summary>
        public static OnlineChat Instance
        {
            get
            {
                var game = (QuaverGame) GameBase.Game;
                return game.OnlineChat;
            }
        }

        /// <summary>
        /// </summary>
        public OnlineChat()
        {
            Size = new ScalableVector2(WindowManager.Width - OnlineHub.WIDTH, 450);
            Tint = ColorHelper.HexToColor("#2F2F2F");

            CreateChatChannelList();
            CreateChatMessageContainer();

            ChannelList.Parent = this;
            DestroyIfParentIsNull = false;

            OnlineManager.Status.ValueChanged += OnConnectionStatusChanged;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            // Handle header dragging
            var rect = new RectangleF(ChannelList.ScreenRectangle.X, ChannelList.ScreenRectangle.Y, Width,
                ChannelList.HeaderBackground.Height);

            if (rect.Contains(MouseManager.CurrentState.Position.ToPoint()) && MouseManager.CurrentState.LeftButton == ButtonState.Pressed)
            {
                var height = MathHelper.Clamp(WindowManager.Height - MouseManager.CurrentState.Y + ChannelList.HeaderBackground.Height / 2f,
                    MessageContainer.TextboxContainer.Height + 200, WindowManager.Height - MenuBorder.HEIGHT);

                ChangeSize(new ScalableVector2(Width, height));
            }

            base.Update(gameTime);
        }

        /// <summary>
        ///     Performs an animation to open the chat
        /// </summary>
        public void Open()
        {
            ClearAnimations();
            MoveToY(0, Easing.OutQuint, 500);
            IsOpen = true;
        }

        /// <summary>
        ///     Performs an animation to close the clear
        /// </summary>
        public void Close()
        {
            ClearAnimations();
            MoveToY((int) Height + 10, Easing.OutQuint, 500);
            IsOpen = false;
        }

        /// <summary>
        /// </summary>
        private void CreateChatChannelList()
            => ChannelList = new ChatChannelList(ActiveChannel, new ScalableVector2(250, Height)) {Parent = this};

        /// <summary>
        /// </summary>
        private void CreateChatMessageContainer()
        {
            MessageContainer = new ChatMessageContainer(ActiveChannel, new ScalableVector2(Width - ChannelList.Width, Height))
            {
                Parent = this,
                X = ChannelList.Width
            };
        }

        /// <summary>
        ///     Handles changing the size of the chat
        /// </summary>
        /// <param name="size"></param>
        public void ChangeSize(ScalableVector2 size)
        {
            Size = size;

            foreach (var child in Children)
            {
                if (child is IResizable c)
                    c.ChangeSize(size);
            }
        }

        /// <summary>
        ///     Subscribes to online events when logging online
        /// </summary>
        private void SubscribeToOnlineEvents()
        {
            OnlineManager.Client.OnAvailableChatChannel += OnAvailableChatchannel;
            OnlineManager.Client.OnFailedToJoinChatChannel += OnFailedToJoinChatChannel;
        }

        /// <summary>
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (OnlineManager.Client == null)
                return;

            OnlineManager.Client.OnAvailableChatChannel -= OnAvailableChatchannel;
            OnlineManager.Client.OnFailedToJoinChatChannel -= OnFailedToJoinChatChannel;
        }

        /// <summary>
        ///     When successfully connecting, subscribe to online chat related events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConnectionStatusChanged(object sender, BindableValueChangedEventArgs<ConnectionStatus> e)
        {
            if (e.Value != ConnectionStatus.Connected)
            {
                UnsubscribeFromEvents();
                return;
            }

            SubscribeToOnlineEvents();

            AvailableChatChannels.Clear();
            Logger.Important("Cleared previously available chat channels", LogType.Runtime);

            foreach (var chan in JoinedChatChannels)
            {
                if (chan.IsPrivate)
                    continue;

                OnlineManager.Client?.JoinChatChannel(chan.Name);
                Logger.Important($"Requested to rejoin chat channel: {chan.Name}", LogType.Runtime);
            }
        }

        /// <summary>
        ///     Called when receiving a new available chat channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAvailableChatchannel(object sender, AvailableChatChannelEventArgs e)
        {
            if (AvailableChatChannels.Contains(e.Channel))
                return;

            AvailableChatChannels.Add(e.Channel);
            Logger.Important($"Received available chat channel: {e.Channel.Name}", LogType.Runtime);
        }

        /// <summary>
        ///     Called when failing to join a chat channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFailedToJoinChatChannel(object sender, FailedToJoinChatChannelEventArgs e)
        {
            var log = $"Failed to join channel: {e.Channel}";

            NotificationManager.Show(NotificationLevel.Error, log);
            Logger.Important(log, LogType.Runtime);
        }

        /// <summary>
        ///     Example chat channels used for testing
        /// </summary>
        /// <returns></returns>
        private static List<ChatChannel> GetTestChannels()
        {
            var channels = new List<ChatChannel>();

            channels.Add(new ChatChannel()
            {
                Name = "#announcements",
                Description = "No Description"
            });

            channels.Add(new ChatChannel()
            {
                Name = "#quaver",
                Description = "No Description"
            });

            channels.Add(new ChatChannel()
            {
                Name = "#offtopic",
                Description = "No Description"
            });

            /*for (var i = 0; i < 10; i++)
            {
                channels.Add(new ChatChannel()
                {
                    Name = $"#example-{i}",
                    Description = $"Example Channel #{i}",
                    IsUnread = true,
                    IsMentioned = true
                });
            }*/

            return channels;
        }
    }
}