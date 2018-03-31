using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiscordBotFunctionLibrary;

namespace RoleManagerBot
{
    public class DiscordHelper
    {
        public static IConfigurationRoot Configuration { get; set; }

        private DiscordSocketClient _client;
        private string BotUsername;
        private List<string> roleNames;

        public DiscordHelper()
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", false, true);

            Configuration = builder.Build();
            roleNames = new List<string>();
            Configuration.GetSection("Roles").Bind(roleNames);

            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Info
            });
            _client.Log += Log;

            _client.MessageReceived += MessageReceived;
            _client.GuildMemberUpdated += GuildMemberUpdated;

            _client.GuildAvailable += GuildAvailable;

            BotUsername = Configuration["BotUsername"];
        }
        public async Task Init()
        {
            string token = Configuration["auth:token"];

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        private async Task GuildMemberUpdated(SocketGuildUser oldInfo, SocketGuildUser newInfo)
        {
            var roles = newInfo.Guild.Roles.ToList();
            await RoleManagementHelper.UpdateRolesWithRetry(newInfo, roles, roleNames);
        }

        private async Task GuildAvailable(SocketGuild guild)
        {
            await RoleManagementHelper.UpdateGuildRoles(guild, roleNames);
        }

        private async Task MessageReceived(SocketMessage message)
        {
            var content = SanitizeContent(message.Content);
            var response = "";
            var mentionedBot = message.MentionedUsers.FirstOrDefault(x => x.Username == BotUsername) != null && message.Author.Username != BotUsername;

            if (mentionedBot)
            {
                var random = new Random();
                var randomInt = random.Next(0, 100);
                if(randomInt <= 50)
                {
                    response = "Cheese!";
                }
                else
                {
                    response = "Petrol!";
                }
                response = $"{message.Author.Mention} {response}";
            }
            if (!string.IsNullOrEmpty(response))
            {
                await message.Channel.SendMessageAsync(response);
            }
        }

        private string SanitizeContent(string message)
        {
            string sanitized = message;
            sanitized = Regex.Replace(sanitized, "<.*?>", string.Empty);
            if (sanitized.Substring(0, 1) == " ")
            {
                sanitized = sanitized.Substring(1, sanitized.Length - 1);
            }
            return sanitized;
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
