﻿#region

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Xenon.Services;

#endregion

namespace Xenon.Core
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckBotPermissionAttribute : PreconditionAttribute
    {
        public CheckBotPermissionAttribute(GuildPermission permission)
        {
            GuildPermission = permission;
            ChannelPermission = null;
        }

        public CheckBotPermissionAttribute(ChannelPermission permission)
        {
            ChannelPermission = permission;
            GuildPermission = null;
        }

        public GuildPermission? GuildPermission { get; }
        public ChannelPermission? ChannelPermission { get; }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            IGuildUser guildUser = null;
            if (context.Guild != null)
                guildUser = await context.Guild.GetCurrentUserAsync().ConfigureAwait(false);

            if (GuildPermission.HasValue)
            {
                if (guildUser == null)
                    return PreconditionResult.FromError(
                        $"This command can only be used in a {"server".InlineCode()} channel");
                if (!guildUser.GuildPermissions.Has(GuildPermission.Value))
                    return PreconditionResult.FromError(
                        $"I need the permission {GuildPermission.Value.Humanize()} to do this");
            }

            if (!ChannelPermission.HasValue) return PreconditionResult.FromSuccess();
            ChannelPermissions perms;
            if (context.Channel is IGuildChannel guildChannel)
                perms = guildUser.GetPermissions(guildChannel);
            else
                perms = ChannelPermissions.All(context.Channel);

            return !perms.Has(ChannelPermission.Value)
                ? PreconditionResult.FromError(
                    $"I need the channel permission {ChannelPermission.Value.Humanize()} to do this")
                : PreconditionResult.FromSuccess();
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckPermissionAttribute : PreconditionAttribute
    {
        public CheckPermissionAttribute(GuildPermission permission)
        {
            GuildPermission = permission;
            ChannelPermission = null;
        }

        public CheckPermissionAttribute(ChannelPermission permission)
        {
            ChannelPermission = permission;
            GuildPermission = null;
        }

        public GuildPermission? GuildPermission { get; }
        public ChannelPermission? ChannelPermission { get; }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            var guildUser = context.User as IGuildUser;

            if (GuildPermission.HasValue)
            {
                if (guildUser == null)
                    return Task.FromResult(
                        PreconditionResult.FromError(
                            $"This command is only aviable in {"server".InlineCode()} channels"));
                if (!guildUser.GuildPermissions.Has(GuildPermission.Value))
                    return Task.FromResult(
                        PreconditionResult.FromError(
                            $"You need the permission {GuildPermission.Value.Humanize()} to do this"));
            }

            if (ChannelPermission.HasValue)
            {
                ChannelPermissions perms;
                if (context.Channel is IGuildChannel guildChannel)
                    perms = guildUser.GetPermissions(guildChannel);
                else
                    perms = ChannelPermissions.All(context.Channel);

                if (!perms.Has(ChannelPermission.Value))
                    return Task.FromResult(PreconditionResult.FromError(
                        $"You need the channel permission {ChannelPermission.Value.Humanize()} to do this"));
            }

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckServerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            return Task.FromResult(context.Guild != null
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError(
                    $"This command is only aviable in {"server".InlineCode()} channels"));
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckNsfwAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (context.Channel is ITextChannel text && text.IsNsfw)
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(
                PreconditionResult.FromError($"This command is only aviable in {"nsfw".InlineCode()} channels"));
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckBotOwnerAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            switch (context.Client.TokenType)
            {
                case TokenType.Bot:
                    var application = await context.Client.GetApplicationInfoAsync();
                    if (context.User.Id != application.Owner.Id)
                        return PreconditionResult.FromError("You are not the owner of this bot!");
                    return PreconditionResult.FromSuccess();
                default:
                    return PreconditionResult.FromError($"I just occured an internal error! :(");
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckServerOwnerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            return Task.FromResult(context.Guild == null
                ? PreconditionResult.FromError($"This command is only aviable in {"server".InlineCode()} channels")
                : ((IGuildUser) context.User).Guild.OwnerId == context.User.Id
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError(
                        $"You need to be the {"owner".InlineCode()} of this server to use this command"));
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class CheckBotHierarchyAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter,
            object value, IServiceProvider services)
        {
            var currentUser = (context as SocketCommandContext)?.Guild.CurrentUser;
            if (value is SocketGuildUser user && currentUser != null)
                return Task.FromResult(user.Hierarchy <= currentUser.Hierarchy
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError("I have not enough permissions to do this"));

            throw new NotImplementedException(nameof(value));
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class CheckUserHierarchyAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter,
            object value, IServiceProvider services)
        {
            var user = (context as SocketCommandContext)?.Guild.CurrentUser;
            if (value is SocketGuildUser target && user != null)
                return Task.FromResult(target.Hierarchy <= user.Hierarchy
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError("You have not enough permissions to do this"));

            throw new NotImplementedException(nameof(value));
        }
    }
}