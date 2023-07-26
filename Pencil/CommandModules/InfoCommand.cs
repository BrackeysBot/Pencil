﻿using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Humanizer;
using Pencil.Services;
using X10D.DSharpPlus;

namespace Pencil.CommandModules;

/// <summary>
///     Represents a class which implements the <c>info</c> command.
/// </summary>
internal sealed class InfoCommand : ApplicationCommandModule
{
    private readonly BotService _botService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfoCommand" /> class.
    /// </summary>
    /// <param name="botService">The bot service.</param>
    public InfoCommand(BotService botService)
    {
        _botService = botService;
    }

    [SlashCommand("info", "Displays information about the bot.")]
    [SlashRequireGuild]
    public async Task InfoAsync(InteractionContext context)
    {
        DiscordClient client = context.Client;
        DiscordMember member = (await client.CurrentUser.GetAsMemberOfAsync(context.Guild).ConfigureAwait(false))!;
        string pencilVersion = _botService.Version;

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(member);
        embed.WithColor(member.Color);
        embed.WithThumbnail(member.AvatarUrl);
        embed.WithTitle($"Pencil v{pencilVersion}");
        embed.AddField("Ping", client.Ping, true);
        embed.AddField("Uptime", (DateTimeOffset.UtcNow - _botService.StartedAt).Humanize(), true);
        embed.AddField("View Source", "[View on GitHub](https://github.com/BrackeysBot/Pencil)", true);

        var builder = new StringBuilder();
        builder.AppendLine($"Pencil: {pencilVersion}");
        builder.AppendLine($"D#+: {client.VersionString}");
        builder.AppendLine($"Gateway: {client.GatewayVersion}");
        builder.AppendLine($"CLR: {Environment.Version.ToString(3)}");
        builder.AppendLine($"Host: {Environment.OSVersion}");

        embed.AddField("Version", Formatter.BlockCode(builder.ToString()));

        await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
    }
}
