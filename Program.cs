using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using RaptorDB;

namespace Spindel
{
    public class Program
    {
        private static RaptorDBString db;

        public static void Main(string[] args)
        {
            db = new RaptorDBString(@".\test", true);

            Global.SaveTimerSeconds = 5;

            var c = db.Count();

            for (long i = 0; i < 10000; i++)
            {
                var page = new Page();
                page.Url = i.ToString();
                page.Children = new List<Page> {new Page("childPage")};

                db.Set(page.Url, SerializeToByteArray(page.Children));
            }


            byte[] test;
            db.Get("50", out test);

            var childPage = DeserializeFromByteArray(test);

            db.Shutdown();
        }

        private static byte[] SerializeToByteArray(List<Page> obj)
        {
            var binFormatter = new BinaryFormatter();
            var mStream = new MemoryStream();
            binFormatter.Serialize(mStream, obj);
            return mStream.ToArray();
        }

        private static List<Page> DeserializeFromByteArray(byte[] array)
        {
            var mStream = new MemoryStream();
            var binFormatter = new BinaryFormatter();
            mStream.Write(array, 0, array.Length);
            mStream.Position = 0;
            return binFormatter.Deserialize(mStream) as List<Page>;
        }
    }

    #region [entities]

    [Serializable]
    public class Page
    {
        public Page()
        {
        }

        public Page(string url)
        {
            Url = url;
        }

        public string Url { get; set; }
        public List<Page> Children { get; set; }
    }

    #endregion
}