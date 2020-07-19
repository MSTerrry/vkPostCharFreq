using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using Newtonsoft.Json;
using VkNet.Enums.Filters;
using System.Text.RegularExpressions;

namespace vkPostCharCounter
{
    class Program
    {
        private static ulong _appId = 7540765;
        private static long _groupId = 197250821;
        private static string _accessToken = "d4b54a710b23a15096aafc52e7f2de359075cc37d64ae8067c3a8ac2d8a61c8fc091d99c84c7f86de118f";
        private static VkApi _api;
        static void Main(string[] args)
        {
            _api = new VkApi();
            _api.Authorize(new ApiAuthParams
            {
                ApplicationId = _appId,
                AccessToken = _accessToken,
            });
            long id;
            Console.WriteLine("Введите идентификатор:");
            string str = Console.ReadLine();
            while (str != "")
            {
                var result = GetId(str);
                if (long.TryParse(result.Item1, out id))
                {
                    var statistic = CountFrequency(GetPostsString(id)).OrderBy(x => x.Key);
                    Console.WriteLine(string.Join(Environment.NewLine, statistic));
                    SendPost(result, JsonConvert.SerializeObject(statistic));
                }
                else Console.WriteLine(result.Item1);
                Console.WriteLine("Введите идентификатор:");
                str = Console.ReadLine();
            }
        }
        private static void SendPost(Tuple<string, string> result, string text)
        {
            var message = $"{result.Item2}, статистика для последних 5 постов: {text}";
            try
            {
                _api.Wall.Post(new VkNet.Model.RequestParams.WallPostParams
                {
                    Message = message,
                    OwnerId = -_groupId,
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private static Tuple<string, string> GetId(string input)
        {
            var result = GetUserId(input);
            if (result.Item2 == "error")
                result = GetGroupId(input);
            return result;
        }
        private static Tuple<string, string> GetUserId(string input)
        {
            try
            {
                var user = _api.Users.Get(new List<string> { input }).First();
                return new Tuple<string, string>(user.Id.ToString(), user.Nickname);
            }
            catch (Exception e)
            {
                return new Tuple<string, string>(e.Message, "error");
            }
        }
        private static Tuple<string, string> GetGroupId(string input)
        {
            try
            {
                var group = _api.Groups.GetById(null, input.Replace("public", ""), GroupsFields.All).First();
                return new Tuple<string, string>((-1 * group.Id).ToString(), group.Name);
            }
            catch (Exception e)
            {
                return new Tuple<string, string>(e.Message, "null");
            }
        }
        private static string GetPostsString(long id = -1, int count = 5)
        {
            var posts = _api.Wall.Get(new VkNet.Model.RequestParams.WallGetParams { OwnerId = id, Count = 5 }).WallPosts;
            var builder = new StringBuilder();
            foreach (var e in posts)
            {
                builder.Append(e.Text);
            }
            var regex = new Regex("[^a-zA-zа-яёА-ЯЁ]", RegexOptions.CultureInvariant);
            return regex.Replace(builder.ToString(), "").Replace("`", "").Replace("_", "");
        }
        private static Dictionary<char, double> CountFrequency(string text)
        {
            double count = text.Length;
            return text.GroupBy(x => x).ToDictionary(c => c.Key, c => c.Count() / count);
        }
    }
}