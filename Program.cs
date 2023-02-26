using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace TelegramBot {
    class TelegramTTTGame {

    }

    class TelegramTTTUser {
        public static List<TelegramTTTUser> users = new();

        public ITelegramBotClient bot;
        public TelegramTTTGame game;

        public long userId;
        public string fullname;

        public bool isFree;

        public TelegramTTTUser() {}

        public TelegramTTTUser(ITelegramBotClient bot, long userId, string fullname) {
            this.bot = bot;
            this.game = null;

            this.userId = userId;
            this.fullname = fullname;

            this.isFree = false;

            TelegramTTTUser.users.Add(this);
        }

        public void Search() {
            this.isFree = true;

            // --- 1

            var filteredUsers = (
                from user in TelegramTTTUser.users
                where (user.isFree && !(this.Equals(user)))
                select user
            ).ToList();

            // --- 

            if (filteredUsers.Count() > 0)
            {   
                // --- 2
                
                var enemy = filteredUsers[0];
                Task.Run(async () => { await this.HandlerFoundGame(enemy.userId, enemy.fullname); });
                Task.Run(async () => { await filteredUsers[0].HandlerFoundGame(this.userId, this.fullname); });

                // ---

                // CreateGame()
            }
        }

        async public Task HandlerFoundGame(long enemyUserId, string enemyFullname) {
            this.isFree = false;

            await bot.SendTextMessageAsync(this.userId, $"your enemy is {enemyFullname}[userId={enemyUserId}]");
        }
    }

    class Program {
        static string TOKEN = "5622134573:AAHbULm4SQRIuDSTi4XTTfpU6OTQz8CelIw";

        static void Main(string[] args) {
            TelegramBotClient client = new TelegramBotClient(TOKEN);

            client.StartReceiving(Update, Error);

            Console.ReadLine();
        }

        async static Task Update(ITelegramBotClient bot, Update update, CancellationToken token) {
            var callback = update.CallbackQuery;
            Message message;

            if (callback != null) {
                message = callback.Message;
            } else {
                message = update.Message;
            }
            var userId = message.Chat.Id;
            var fullname = message.From.FirstName + " " + message.From.LastName;

            if (message != null) {
                if (message.Text != null) {
                    if (message.Text == "/search") {
                        if ((
                            from user in TelegramTTTUser.users 
                            where user.isFree == true && user.userId == userId
                            select user
                        ).Count() == 0)
                        {
                            await bot.SendTextMessageAsync(userId, "search a game...");

                            CreateUser(bot, userId, fullname);
                        } else {
                            await bot.SendTextMessageAsync(userId, "you are already searching a game");
                        }
                    }
                }
            }
            if (callback != null) {
                string data = callback.Data;

                char fieldIndex = data[0];
                char side = data[1];

                if (side == '0') {
                    
                }
            }

            return;
        }

        static void CreateUser(ITelegramBotClient bot, long userId, string fullname) {
            // TelegramTTTUser.users.ForEach((TelegramTTTUser e) => {
            //     System.Console.Write($" User(userId={e.userId}, isFree={e.isFree})");
            // });
            // System.Console.WriteLine();

            var user = new TelegramTTTUser(bot, userId, fullname);
            user.Search();
        }

        static Task Error(ITelegramBotClient bot, Exception exception, CancellationToken token) {
            throw new NotImplementedException();
        }
    }
}
