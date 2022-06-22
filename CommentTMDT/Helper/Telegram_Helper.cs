using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace CommentTMDT.Helper
{
    class Telegram_Helper
    {
        private readonly TelegramBotClient BotClientKafka;

        public Telegram_Helper(string keyBot)
        {
            BotClientKafka = new TelegramBotClient(keyBot);
        }

        public async Task SendMessageToChannel(string message, long groupOrChannelId, bool isHtmlParseMode = false, bool isWebPreview = false)
        {
            try
            {
                var parseMode = ParseMode.Html;
                await BotClientKafka.SendTextMessageAsync(groupOrChannelId, message, parseMode, disableWebPagePreview: !isWebPreview).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }

        public async ValueTask<bool> SendFileToGroup(string pathFile, long idGroup)
        {
            try
            {
                using (FileStream sendFileStream = System.IO.File.Open(pathFile, FileMode.Open))
                {
                    await BotClientKafka.SendDocumentAsync(idGroup,
                        new Telegram.Bot.Types.InputFiles.InputOnlineFile(sendFileStream,
                        $"{DateTime.Now.Date.ToString("dd-MM-yyyy")}.txt"));
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
