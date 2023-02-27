using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace TelegramBot {
    class TelegramTTTGame {
        public static List<TelegramTTTGame> games = new();
        public ITelegramBotClient bot;
        public List<byte> map = new();
        public bool moveNow = false;

        public TelegramTTTGame(ITelegramBotClient bot, TelegramTTTUser user1, TelegramTTTUser user2) {
            this.FillNull();
            this.bot = bot;
        }

        public bool Move(TelegramTTTUser user, bool side, char fieldIndex) {
            if (user.side == side && this.moveNow == side)
            {
                this.map[fieldIndex - '0'] = (byte) (side ? 1 : 2);
                this.moveNow = !this.moveNow;

                Task.Run(async () => { await bot.SendTextMessageAsync(user.userId, GetStringMap()); System.Console.WriteLine(GetStringMap()); });

                return true;
            } else {
                return false;
            }
        }

        public string GetStringMap() {
            string stringMap = "";

            for (int i = 0; i < 9; i++)
            {
                if (this.map[i] == 0) stringMap += " ";
                if (this.map[i] == 1) stringMap += "X";
                if (this.map[i] == 2) stringMap += "O";
                if (i+1 % 3 == 0) stringMap += "\n";
            }

            return stringMap;
        }

        public void FillNull() {
            for (int i = 0; i < 9; i++) this.map.Add(0);
        }
    }

    class TelegramTTTUser {
        public static List<TelegramTTTUser> users = new();
        public static List<long> userIdsInGame = new();

        public ITelegramBotClient bot;
        public TelegramTTTGame game;
        public bool side;

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

            if (!TelegramTTTUser.users.Contains(this)) TelegramTTTUser.users.Add(this);
        }

        public void Search() {
            this.isFree = true;

            var filteredUsers = (
                from user in TelegramTTTUser.users
                where (user.isFree && !(this.Equals(user)))
                select user
            ).ToList();

            if (filteredUsers.Count() > 0)
            {
                var enemy = filteredUsers[0];

                Task.Run(async () => { await this.HandlerFoundGame(enemy.userId, enemy.fullname); });
                Task.Run(async () => { await filteredUsers[0].HandlerFoundGame(this.userId, this.fullname); });

                TelegramTTTUser.userIdsInGame.Add(this.userId);
                TelegramTTTUser.userIdsInGame.Add(enemy.userId);

                this.side = false;
                enemy.side = true;

                CreateGame(this.bot, this, enemy);
            }
        }

        static void CreateGame(ITelegramBotClient bot, TelegramTTTUser user1, TelegramTTTUser user2) {
            TelegramTTTGame newGame = new(bot, user1, user2);
            user1.game = newGame;
            user2.game = newGame;
        }

        public bool Move(char fieldIndex) {
            return this.game.Move(this, this.side, fieldIndex);
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
                    if ("012345678".Contains(message.Text[0])) {
                        if (TelegramTTTUser.userIdsInGame.Contains(userId)) {
                            bool result = GetUserById(userId).Move(message.Text[0]);

                            if (result)
                            {
                                await bot.SendTextMessageAsync(userId, "ok");
                            } else {
                                await bot.SendTextMessageAsync(userId, "you dont have access to move right now");
                            }
                        }
                    }
                }
            }
            // if (callback != null) {
            //     string data = callback.Data;

            //     char fieldIndex = data[0];

            //     if (fieldIndex == '0') {
                    
            //     }
            // }

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

        static TelegramTTTUser GetUserById(long userId) {
            return (
                from user in TelegramTTTUser.users 
                where user.userId == userId 
                select user
            ).ToList()[0];
        }

        static Task Error(ITelegramBotClient bot, Exception exception, CancellationToken token) {
            throw new NotImplementedException();
        }
    }
}
